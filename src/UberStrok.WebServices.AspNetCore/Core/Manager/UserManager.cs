using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UberStrok.Core.Common;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Core.Db;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public static class UserManager
    {
        public static MongoDatabase<UserDocument> Database { get; private set; }
        public static void Init()
        {
            Database = new MongoDatabase<UserDocument>("Users");
            Database.InitSequence();
            try
            {
                _ = Database.Collection.Indexes.CreateOne(new CreateIndexModel<UserDocument>(Builders<UserDocument>.IndexKeys.Ascending((UserDocument f) => f.Profile.Name), new CreateIndexOptions<UserDocument>
                {
                    Name = "Name",
                    Unique = new bool?(true),
                    Collation = new Collation("en", default, default, CollationStrength.Secondary, default, default, default, default, default)
                }), null, default);
                _ = Database.Collection.Indexes.CreateOne(new CreateIndexModel<UserDocument>(Builders<UserDocument>.IndexKeys.Ascending((UserDocument f) => f.SteamId), new CreateIndexOptions
                {
                    Name = "SteamId",
                    Unique = new bool?(true)
                }), null, default);
                _ = Database.Collection.Indexes.CreateOne(new CreateIndexModel<UserDocument>(Builders<UserDocument>.IndexKeys.Ascending((UserDocument f) => f.UBBan), new CreateIndexOptions<UserDocument>
                {
                    Name = "Banned",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Exists((UserDocument u) => u.UBBan, true)
                }), null, default);
            }
            catch(Exception ex)
            {
                //ignore if indexes can't be created
                Console.WriteLine(ex.Message);
            }

        }

        internal static Task<UserDocument> CreateUser(string steamId, UberBeat uberbeat)
        {
            try
            {
                int id = GetCount().Result;
                UserDocument document = new UserDocument
                {
                    UserId = id,
                    SteamId = steamId,
                    Profile = new PublicProfileView(id, steamId, MemberAccessLevel.Default, isChatDisabled: false, DateTime.UtcNow, EmailAddressStatus.Unverified, "-1"),
                    Wallet = new MemberWalletView(id, Startup.WebServiceConfiguration.Wallet.StartingCredits, Startup.WebServiceConfiguration.Wallet.StartingPoints, DateTime.MaxValue, DateTime.MaxValue),
                    Inventory = new List<ItemInventoryView>
                    {
                        new ItemInventoryView(1, null, -1, id),
                        new ItemInventoryView(12, null, -1, id)
                    },
                    Statistics = new PlayerStatisticsView(id, 0, 0, 0L, 0L, 0, 0, 0, 1, new PlayerPersonalRecordStatisticsView(), new PlayerWeaponStatisticsView()),
                    Loadout = new LoadoutView
                    {
                        Cmid = id,
                        MeleeWeapon = 1,
                        Weapon1 = 12
                    },
                    Friends = new List<int>(),
                    FriendRequests = new List<int>(),
                    ClanRequests = new List<int>(),
                    Streams = new List<int>(),
                    Names = new HashSet<string>(),
                    HDD = new HashSet<string>(uberbeat.HDD),
                    BIOS = new HashSet<string>(uberbeat.BIOS),
                    MOTHERBOARD = new HashSet<string>(uberbeat.MOTHERBOARD),
                    MAC = new HashSet<string>(uberbeat.MAC),
                    UNITY = new HashSet<string>(uberbeat.UNITY)
                };
                Database.Collection.InsertOne(document);
                return Task.FromResult<UserDocument>(document);
            }
            catch (MongoException e2)
            {
                Console.WriteLine(e2.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return null;
        }

        internal static Task DeleteUser(int cmid)
        {
            return Task.Run(()=>Database.Collection.DeleteOne(x => x.UserId == cmid));
        }

        internal static Task<bool> IsNameUsed(string name)
        {
            return Task.FromResult(Database.Collection.AsQueryable().Where((UserDocument f) => f.Profile.Name.ToLower() == name.ToLower()).Count() != 0);
        }

        internal static Task<UserDocument> GetUser(int id)
        {
            return Database.Collection.Find(Builders<UserDocument>.Filter.Eq((UserDocument f) => f.UserId, id)).FirstOrDefaultAsync();
        }

        internal static Task<int> GetCount()
        {
            try
            {
                FindOptions<UserDocument, UserDocument> findOptions = new FindOptions<UserDocument, UserDocument>
                {
                    Limit = 1,
                    Sort = Builders<UserDocument>.Sort.Descending((UserDocument f) => f.Profile.Cmid)
                };
                FindOptions<UserDocument, UserDocument> filter = findOptions;
                UserDocument doc = Database.Collection.FindAsync(FilterDefinition<UserDocument>.Empty, filter).Result.FirstOrDefault<UserDocument>();
                return doc == null ? Task.FromResult<int>(1) : Task.FromResult<int>(doc.Profile.Cmid + 1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return Task.FromResult<int>(0);
            }
        }

        internal static Task<List<UserDocument>> Leaderboard(int limit, string key)
        {
            return key == "kill"
                ? Database.Collection.Find((UserDocument x) => true, null).SortByDescending((UserDocument f) => f.Kills).Limit(limit)
                    .ToListAsync()
                : key == "xp"
                ? Database.Collection.Find((UserDocument x) => true, null).SortByDescending((UserDocument f) => f.Statistics.Xp).Limit(limit)
                    .ToListAsync()
                : Database.Collection.Find((UserDocument x) => x.Statistics.Level > 9, null).SortByDescending((UserDocument f) => f.Statistics.Xp).Limit(limit)
                .ToListAsync();
        }

        internal static Task<List<UserDocument>> bannedUsers()
        {
            return Database.Collection.Find((UserDocument x) => x.UBBan != null, null).ToListAsync();
        }

        internal static Task<UserDocument> GetUser(string steamId)
        {
            return Database.Collection.Find(Builders<UserDocument>.Filter.Eq((UserDocument f) => f.SteamId, steamId)).FirstOrDefaultAsync();
        }

        internal static Task<List<UserDocument>> FindUser(string name)
        {
            FilterDefinitionBuilder<UserDocument> filterBuilder = Builders<UserDocument>.Filter;
            return Database.Collection.Find((UserDocument x) => x.Names.Any((string t) => t.ToLower().Contains(name.ToLower())), null).ToListAsync();
        }

        internal static Task Save(UserDocument document)
        {
            return Database.Collection.ReplaceOneAsync((UserDocument f) => f.Id == document.Id, document, (ReplaceOptions)null, default);
        }

        internal static Task ChangeName(int id, string newName)
        {
            return Database.Collection.UpdateOneAsync(Builders<UserDocument>.Filter.Eq((UserDocument f) => f.UserId, id), Builders<UserDocument>.Update.Set((UserDocument f) => f.Profile.Name, newName));
        }

        internal static Task<HashSet<int>> MatchHWID(string key, HashSet<string> obj)
        {
            List<UserDocument> docs = new List<UserDocument>();
            FilterDefinitionBuilder<UserDocument> filterBuilder = Builders<UserDocument>.Filter;
            switch (key)
            {
                case "hdd":
                    docs = Database.Collection.Find(filterBuilder.AnyIn((UserDocument x) => x.HDD, obj)).ToList();
                    break;
                case "bios":
                    docs = Database.Collection.Find(filterBuilder.AnyIn((UserDocument x) => x.BIOS, obj)).ToList();
                    break;
                case "motherboard":
                    docs = Database.Collection.Find(filterBuilder.AnyIn((UserDocument x) => x.MOTHERBOARD, obj)).ToList();
                    break;
                case "mac":
                    docs = Database.Collection.Find(filterBuilder.AnyIn((UserDocument x) => x.MAC, obj)).ToList();
                    break;
            }
            HashSet<int> cmids = new HashSet<int>();
            foreach (UserDocument doc in docs)
            {
                _ = cmids.Add(doc.Profile.Cmid);
            }
            return Task.FromResult(cmids);
        }
    }
}
