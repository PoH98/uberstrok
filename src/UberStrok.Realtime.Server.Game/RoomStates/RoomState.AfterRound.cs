using System;
using UberStrok.Core;
using UberStrok.Core.Common;

namespace UberStrok.Realtime.Server.Game
{
    public sealed class AfterRoundState : RoomState
    {
        private readonly Countdown _restartCountdown;

        private TeamID _winner;

        public AfterRoundState(GameRoom room)
            : base(room)
        {
            _restartCountdown = new Countdown(base.Room.Loop, 3, 0);
            _restartCountdown.Completed += OnRestartCountdownCompleted;
        }

        public override void OnEnter()
        {
            _winner = GetWinner();
            foreach (GameActor player in base.Room.Players)
            {
                player.Peer.Events.Game.SendTeamWins(_winner);
                player.State.Set(ActorState.Id.AfterRound);
            }
            _restartCountdown.Restart();
        }

        public override void OnTick()
        {
            _restartCountdown.Tick();
        }

        private void OnRestartCountdownCompleted()
        {
            if (base.Room.IsTeamElimination)
            {
                if (base.Room.GetView().KillLimit - Math.Max(base.Room.BlueTeamScore, base.Room.RedTeamScore) == 0)
                {
                    base.Room.State.Set(Id.End);
                    return;
                }
                base.Room.RoundNumber++;
                base.Room.State.Set(Id.Countdown);
            }
            else
            {
                base.Room.RoundNumber++;
                base.Room.State.Set(Id.End);
            }
        }

        private TeamID GetWinner()
        {
            if (base.Room.IsTeamElimination)
            {
                if (base.Room.GetView().KillLimit - Math.Max(base.Room.BlueTeamScore, base.Room.RedTeamScore) == 0)
                {
                    if (base.Room.BlueTeamScore > base.Room.RedTeamScore)
                    {
                        base.Room.Winner = (TeamID)1;
                    }
                    else if (base.Room.RedTeamScore > base.Room.BlueTeamScore)
                    {
                        base.Room.Winner = (TeamID)2;
                    }
                    else
                    {
                        base.Room.Winner = (TeamID)0;
                    }
                    return base.Room.Winner;
                }
                return ((TeamEliminationGameRoom)base.Room).RoundWinner;
            }
            return base.Room.Winner;
        }
    }

}
