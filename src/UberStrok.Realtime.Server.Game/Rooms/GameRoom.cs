using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.SocketServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Uberstrok.Core.Common;
using UberStrok.Core;
using UberStrok.Core.Common;
using UberStrok.Core.Views;

namespace UberStrok.Realtime.Server.Game
{
    public abstract partial class GameRoom : IRoom<GamePeer>, IDisposable
    {
        private bool _disposed;

        private byte _nextPlayer;
        private string _password;

        private ushort _frame;
        private readonly Timer _frameTimer;
        //A time that players had retrain the same for long time
        private readonly GameRoomDataView _view;
        public int EmptyTickTime = 0;
        public int LastTickTime;
        /* 
         * Dictionary mapping player CMIDs to StatisticsManager instances.
         * This is used for when a player leaves and joins the game again; so
         * as to retain his stats.
         */
        private readonly Dictionary<int, StatisticsManager> _stats;
        /* List of cached player stats for end game. */
        private List<StatsSummaryView> _mvps;

        /* List of actor info delta. */
        private readonly List<GameActorInfoDeltaView> _actorDeltas;
        /* List of actor movement. */
        private readonly List<PlayerMovement> _actorMovements;

        private readonly List<GameActor> _actors;
        private readonly List<GameActor> _players;

        protected ILog ReportLog { get; }

        public Loop Loop { get; }
        public ILoopScheduler Scheduler { get; }

        public ICollection<GameActor> Players
        {
            get
            {
                return _players;
            }
        }

        public ICollection<GameActor> Actors
        {
            get
            {
                return _actors;
            }
        }

        public StateMachine<RoomState.Id> State { get; }

        public ShopManager Shop { get; }
        public SpawnManager Spawns { get; }
        public PowerUpManager PowerUps { get; }

        public bool Updated { get; set; }
        public bool IsTeamElimination => _view.GameMode == GameModeType.EliminationMode;
        public int RoundNumber { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }

        public TeamID Winner { get; set; }
        public int BlueTeamScore { get; set; }
        public int RedTeamScore { get; set; }
        /* 
         * Room ID but we call it number since we already defined Id &
         * thats how UberStrike calls it too. 
         */
        public int RoomId
        {
            get => _view.RoomId;
            set => _view.RoomId = value;
        }

        public string Password
        {
            get => _password;
            set
            {
                /* 
                 * If the password is null or empty it means its not
                 * password protected. 
                 */
                _view.IsPasswordProtected = !string.IsNullOrEmpty(value);
                _password = _view.IsPasswordProtected ? value : null;
            }
        }

        public GameRoom(GameRoomDataView data, ILoopScheduler scheduler)
        {
            _view = data ?? throw new ArgumentNullException("data");
            Scheduler = scheduler ?? throw new ArgumentNullException("scheduler");
            ReportLog = LogManager.GetLogger("Report");
            _stats = new Dictionary<int, StatisticsManager>();
            int capacity = data.PlayerLimit / 2;
            _players = new List<GameActor>(capacity);
            _actors = new List<GameActor>(capacity);
            _actorDeltas = new List<GameActorInfoDeltaView>(capacity);
            _actorMovements = new List<PlayerMovement>(capacity);
            Loop = new Loop(OnTick, OnTickError);
            Shop = new ShopManager();
            Spawns = new SpawnManager();
            PowerUps = new PowerUpManager(this);
            State = new StateMachine<RoomState.Id>();
            State.Register(RoomState.Id.None, null);
            State.Register(RoomState.Id.WaitingForPlayers, new WaitingForPlayersRoomState(this));
            State.Register(RoomState.Id.Countdown, new CountdownRoomState(this));
            State.Register(RoomState.Id.Running, new RunningRoomState(this));
            State.Register(RoomState.Id.End, new EndRoomState(this));
            State.Register(RoomState.Id.AfterRound, new AfterRoundState(this));
            _frameTimer = new Timer(Loop, 105.26316f);
            Reset();
            Scheduler.Schedule(Loop);
        }

        public void Join(GamePeer peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException("peer");
            }
            if (peer.Actor != null)
            {
                throw new InvalidOperationException("Peer already in another room");
            }

            Enqueue(delegate
            {
                DoJoin(peer);
            });
        }

        public void SendMessageUDP(string message)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                if (!File.Exists("udphost.txt"))
                {
                    File.WriteAllText("udphost.txt", "127.0.0.1");
                }
                udpClient.Connect(File.ReadAllText("udphost.txt"), 5070);
                byte[] bytes = Encoding.UTF8.GetBytes("game:" + message);
                _ = udpClient.Send(bytes, bytes.Length);
            }
        }

        public void Leave(GamePeer peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException("peer");
            }
            if (peer.Actor == null)
            {
                throw new InvalidOperationException("Peer is not in a room");
            }
            if (peer.Actor.Room != this)
            {
                throw new InvalidOperationException("Peer is not leaving the correct room");
            }
            Enqueue(delegate
            {
                DoLeave(peer);
            });
        }

        public void Spawn(GameActor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException("actor");
            }
            Enqueue(delegate
            {
                DoSpawn(actor);
            });
        }

        private struct Achievement
        {
            public Dictionary<AchievementType, Tuple<StatsSummaryView, ushort>> All;
            public AchievementType Type;
            public int Value;

            public Achievement(AchievementType type, Dictionary<AchievementType, Tuple<StatsSummaryView, ushort>> all)
            {
                All = all;
                Type = type;
                Value = int.MinValue;
            }

            public void Check(StatsSummaryView summary, int value)
            {
                if (Value == value)
                {
                    All.Remove(Type);
                }
                else if (value > Value)
                {
                    Value = value;

                    if (Value > 0)
                        All[Type] = new Tuple<StatsSummaryView, ushort>(summary, (ushort)value);
                }
            }
        }

        public List<StatsSummaryView> GetMvps(bool force = false)
        {
            if (_mvps == null || force)
            {
                _mvps = new List<StatsSummaryView>();

                var achievements = new Dictionary<AchievementType, Tuple<StatsSummaryView, ushort>>();
                var mostValuable = new Achievement(AchievementType.MostValuable, achievements);
                var mostAggressive = new Achievement(AchievementType.MostAggressive, achievements);
                var costEffective = new Achievement(AchievementType.CostEffective, achievements);
                var hardestHitter = new Achievement(AchievementType.HardestHitter, achievements);
                var sharpestShooter = new Achievement(AchievementType.SharpestShooter, achievements);
                var triggerHappy = new Achievement(AchievementType.TriggerHappy, achievements);

                foreach (var player in Players)
                {
                    var summary = new StatsSummaryView
                    {
                        Cmid = player.Cmid,
                        Name = player.PlayerFullName,
                        Kills = player.Info.Kills,
                        Deaths = player.Info.Deaths,
                        Level = player.Info.Level,
                        Team = player.Info.TeamID,
                        Achievements = new Dictionary<byte, ushort>()
                    };

                    _mvps.Add(summary);

                    int kills = player.Statistics.Total.GetKills();
                    int deaths = player.Statistics.Total.Deaths;
                    int kdr;

                    if (kills == deaths)
                        kdr = 10;
                    else
                        kdr = (int)Math.Floor((float)kills / Math.Max(1, deaths)) * 10;

                    int shots = player.Statistics.Total.GetShots();
                    int hits = player.Statistics.Total.GetHits();

                    int accuracy = (int)Math.Floor((float)hits / Math.Max(1, shots) * 1000f);
                    int damageDealt = player.Statistics.Total.GetDamageDealt();
                    int criticalHits = player.Statistics.Total.Headshots + player.Statistics.Total.Nutshots;
                    int consecutiveKills = player.Statistics.MostConsecutiveKills;

                    mostValuable.Check(summary, kdr);
                    mostAggressive.Check(summary, kills);
                    costEffective.Check(summary, accuracy);
                    hardestHitter.Check(summary, damageDealt);
                    sharpestShooter.Check(summary, criticalHits);
                    triggerHappy.Check(summary, consecutiveKills);
                }

                foreach (var kv in achievements)
                {
                    var tuple = kv.Value;
                    var achievement = kv.Key;

                    tuple.Item1.Achievements.Add((byte)achievement, tuple.Item2);
                }

                _mvps = _mvps.OrderByDescending(x => x.Kills).ToList();
            }

            return _mvps;
        }

        public virtual void Reset()
        {
            _frame = 6;
            _frameTimer.Restart();

            _nextPlayer = 0;

            /* Reset all the actors in the room's player list. */
            foreach (var player in Players)
            {
                foreach (var otherActor in Actors)
                {
                    otherActor.Peer.Events.Game.SendPlayerLeftGame(player.Cmid);
                    otherActor.Peer.GetStats(out int rtt, out int rttVar, out int numFailures);
                    Log.Info($"{otherActor.GetDebug()} RTT: {rtt} var<RTT>: {rttVar} NumFailures: {numFailures}");
                }
            }

            _mvps = null;
            _stats.Clear();
            _players.Clear();

            PowerUps.Reset();

            State.Reset();
            State.Set(RoomState.Id.WaitingForPlayers);

            Log.Info($"{GetDebug()} has been reset.");
        }

        private void OnTick()
        {
            bool updateMovements = _frameTimer.Tick();
            if (updateMovements)
                _frame++;

            State.Tick();
            PowerUps.Tick();

            if (Actors.Count == 0)
            {
                EmptyTickTime++;
            }
            else
            {
                EmptyTickTime = 0;
            }

            foreach (GameActor actor in Actors)
            {
                if (actor.Peer.HasError)
                {
                    actor.Peer.Disconnect();
                    continue;
                }
                try
                {
                    actor.Tick();
                }
                catch (Exception ex)
                {
                    base.Log.Error((object)("Failed to tick " + actor.GetDebug() + "."), ex);
                    actor.Peer.Disconnect();
                    continue;
                }
                if (Players.Contains(actor) || actor.State.Current == ActorState.Id.Spectator)
                {
                    GameActorInfoDeltaView viewDelta = actor.Info.GetViewDelta();
                    if (viewDelta.Changes.Count > 0)
                    {
                        viewDelta.Update();
                        _actorDeltas.Add(viewDelta);
                    }
                    if (actor.Damages.Count > 0)
                    {
                        actor.Peer.Events.Game.SendDamageEvent(actor.Damages);
                        actor.Damages.Clear();
                    }
                    if (updateMovements && actor.Info.IsAlive)
                    {
                        _actorMovements.Add(actor.Movement);
                    }
                }
            }
            if (_actorDeltas.Count > 0)
            {
                foreach (GameActor actor in Actors)
                {
                    actor.Peer.Events.Game.SendAllPlayerDeltas(_actorDeltas);
                }
                foreach (GameActorInfoDeltaView actorDelta in _actorDeltas)
                {
                    actorDelta.Reset();
                }
                _actorDeltas.Clear();
            }
            if (!(_actorMovements.Count > 0 && updateMovements))
            {
                return;
            }
            foreach (GameActor actor in Actors)
            {
                actor.Peer.Events.Game.SendAllPlayerPositions(_actorMovements, _frame);
            }
            _actorMovements.Clear();
        }

        private void OnTickError(Exception ex)
        {
            Log.Error("Failed to tick game loop.", ex);
        }

        /* This is executed on the game room loop thread. */
        private void DoJoin(GamePeer peer, bool reJoin = false)
        {
            Debug.Assert(peer != null);
            try
            {
                peer.Handlers.Add(this);

                var view = GetView();
                var actor = new GameActor(peer, this);

                if (view.IsFull && actor.Info.AccessLevel < MemberAccessLevel.QA)
                {
                    peer.Events.SendRoomEnterFailed(default, default, "The game is full.");
                }
                else
                {
                    /* 
                     * This prepares the client for the game room; that is it 
                     * creates the game room instance type and registers the
                     * GameRoom OperationHandler to its photon client.
                     */
                    peer.Events.SendRoomEntered(view, reJoin);

                    peer.Actor = actor;
                    peer.Actor.State.Set(ActorState.Id.Overview);

                    _actors.Add(peer.Actor);
                    var webUrl = actor.PlayerName + "had joined a game! Use this url to play with him! uberstroke://" + AES.EncryptAndEncode(_view.RoomId.ToString());
                    Log.Info("Sending generated Url to web service..." + webUrl);
                    SendMessageUDP(webUrl);
                    Log.Info($"{peer.Actor.GetDebug()} joined.");
                }
            }
            catch (Exception ex)
            {
                peer.Actor = null;
                peer.Handlers.Remove(Id);

                /* The client doesn't care about `server` and `roomId`. */
                peer.Events.SendRoomEnterFailed(default, default, "Failed to join room.");

                Log.Error($"Failed to join {GetDebug()}.", ex);
            }
        }

        private void DoLeave(GamePeer peer)
        {
            if (peer == null)
            {
                Log.Error("Peer was null when tried to leave the room");
                return;
            }
            bool donotResetActor = false;
            if (peer.Actor != null && peer.Actor.Room != this)
            {
                Log.Error("Peer tried to leave a room, but doesnt belong to this room");
                donotResetActor = true;
            }
            var actor = peer.Actor;
            try
            {
                if(actor != null)
                {
                    if (_actors.Remove(actor))
                    {
                        if (_players.Contains(actor))
                        {
                            OnPlayerLeft(new PlayerLeftEventArgs
                            {
                                Player = actor
                            });
                        }
                        base.Log.Info((object)(actor.GetDebug() + " left."));
                    }
                    else
                    {
                        base.Log.Warn((object)(actor.GetDebug() + " tried to leave but was not in the list of Actors."));
                    }
                }
            }
            finally
            {
                /* Clean up. */
                if (!donotResetActor)
                {
                    peer.Actor = null;
                    _ = peer.Handlers.Remove(Id);
                }
                if (peer.OnLeaveRoom != null)
                {
                    peer.OnLeaveRoom();
                    peer.OnLeaveRoom = null;
                }
            }
        }

        private void DoSpawn(GameActor actor)
        {
            Debug.Assert(actor != null);
            Debug.Assert(actor.Room == this);

            var spawn = Spawns.Get(actor.Info.TeamID);
            var movement = actor.Movement;

            Debug.Assert(movement.PlayerId == actor.PlayerId);

            movement.Position = spawn.Position;
            movement.HorizontalRotation = spawn.Rotation;

            /* Let the other actors know it has spawned. */
            foreach (var otherActor in Actors)
            {
                otherActor.Peer.Events.Game.SendPlayerRespawned(
                    actor.Cmid,
                    spawn.Position,
                    spawn.Rotation
                );
            }

            Log.Debug($"{actor.GetDebug()} spawned at {spawn}.");
        }

        /* Determine if state of the room can be switched to RunningRoomState. */
        public abstract bool CanStart();

        /*
         * Determines if the actor can join the specified team.
         * 
         * It would seem there is a client side bug where the client uses
         * GameRoomData.ConnectedPlayers to determined if the room is full or
         * not, however it never updates ConntectedPlayers once it has created
         * the BaseGameRoom instance, so this results in HUDJoinButtons being
         * broken.
         */
        public abstract bool CanJoin(GameActor actor, TeamID team);

        /* Determines if the vicitim can get damaged by the attcker. */
        public abstract bool CanDamage(GameActor victim, GameActor attacker);

        /* Does damage and returns true if victim is killed; otherwise false. */
        protected bool DoDamage(GameActor victim, GameActor attacker, Weapon weapon, short damage, BodyPart part, out Vector3 direction)
        {
            bool selfDamage = victim.Cmid == attacker.Cmid;

            /* Calculate the direction of the hit. */
            var victimPos = victim.Movement.Position;
            var attackerPos = attacker.Movement.Position;
            direction = attackerPos - victimPos;
            
            /* Chill time, game has ended; we don't do damage. */
            if (State.Current == RoomState.Id.End)
                return false;

            /* We can't kill someone who's already dead. */
            if (!victim.Info.IsAlive)
                return false;

            /* Check if we can apply the damage on the players. */
            if (!CanDamage(victim, attacker))
                return false;

            float angle = Vector3.Angle(direction, new Vector3(0, 0, -1));
            if (direction.x < 0)
                angle = 360 - angle;

            byte byteAngle = Conversions.Angle2Byte(angle);

            /* Check if not self-damage. */
            if (!selfDamage)
                victim.Damages.Add(byteAngle, damage, part, 0, 0);
            else
                damage /= 2;

            /* Calculate armor absorption. */
            int armorDamage;
            int healthDamage;
            if (victim.Info.ArmorPoints > 0)
            {
                armorDamage = (byte)(victim.Info.GetAbsorptionRate() * damage);
                healthDamage = (short)(damage - armorDamage);
            }
            else
            {
                armorDamage = 0;
                healthDamage = damage;
            }

            int newArmor = victim.Info.ArmorPoints - armorDamage;
            int newHealth = victim.Info.Health - healthDamage;

            if (newArmor < 0)
                newHealth += newArmor;

            victim.Info.ArmorPoints = (byte)Math.Max(0, newArmor);
            victim.Info.Health = (short)Math.Max(0, newHealth);

            /* Record some statistics. */
            if (!selfDamage)
            {
                victim.Statistics.RecordDamageReceived(damage);
                attacker.Statistics.RecordHit(weapon.GetView().ItemClass);
                attacker.Statistics.RecordDamageDealt(weapon.GetView().ItemClass, damage);
            }

            /* Check if the player is dead. */
            if (victim.Info.Health <= 0)
            {
                
                if (victim.Damages.Count > 0)
                {
                    /* 
                     * Force a push of damage events to the victim peer, so he
                     * gets the feedback of where he was hit from aka red hit
                     * marker HUD.
                     */
                    victim.Peer.Events.Game.SendDamageEvent(victim.Damages);
                    victim.Peer.Flush();

                    victim.Damages.Clear();
                }

                if (selfDamage)
                    attacker.Info.Kills--;
                else
                    attacker.Info.Kills++;

                victim.Info.Deaths++;

                /* Record statistics. */
                victim.Statistics.RecordDeath();

                if (selfDamage)
                {
                    attacker.Statistics.RecordSuicide();
                }
                else
                {
                    if (part == BodyPart.Head)
                        attacker.Statistics.RecordHeadshot();
                    else if (part == BodyPart.Nuts)
                        attacker.Statistics.RecordNutshot();

                    attacker.Statistics.RecordKill(weapon.GetView().ItemClass);
                }

                return true;
            }

            return false;
        }

        public virtual void EndMatch()
        {
            foreach (GameActor player in Players)
            {
                player.EndMatch(Winner == player.Info.TeamID);
            }
        }

        public string GetDebug()
        {
            return $"(room \"{GetView().Name}\":{RoomId} {GetView().ConnectedPlayers}/{GetView().PlayerLimit} state {State.Current})";
        }

        public GameRoomDataView GetView()
        {
            if (_view.ConnectedPlayers != _players.Count)
            {
                _view.ConnectedPlayers = _players.Count;
                Updated = true;
            }

            return _view;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Scheduler.Unschedule(Loop);

                /* Best effort clean up. */
                foreach (var player in Players)
                {
                    foreach (var otherActor in Actors)
                        otherActor.Peer.Events.Game.SendPlayerLeftGame(player.Cmid);
                }

                /* Clean up actors. */
                foreach (var actor in Actors)
                {
                    var peer = actor.Peer;
                    peer.Actor = null;
                    peer.Handlers.Remove(Id);

                    peer.Disconnect();
                    peer.Dispose();
                }

                /* Clear to lose refs to GameActor objects. */
                _actors.Clear();
                _players.Clear();
            }

            _disposed = true;
        }
    }
}
