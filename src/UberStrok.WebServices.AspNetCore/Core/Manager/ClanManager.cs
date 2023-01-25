using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Core.Db;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;
using UberStrok.WebServices.AspNetCore.Helper;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public static class ClanManager
    {
        private static MongoDatabase<ClanDocument> sm_database;

        internal static void Init()
        {
            sm_database = new MongoDatabase<ClanDocument>("Clans");
            sm_database.InitSequence();
            _ = sm_database.Collection.Indexes.CreateOne(new CreateIndexModel<ClanDocument>(Builders<ClanDocument>.IndexKeys.Ascending((ClanDocument f) => f.Clan.Name), new CreateIndexOptions
            {
                Name = "Name",
                Unique = true
            }));
            _ = sm_database.Collection.Indexes.CreateOne(new CreateIndexModel<ClanDocument>(Builders<ClanDocument>.IndexKeys.Ascending((ClanDocument f) => f.Clan.Tag), new CreateIndexOptions
            {
                Name = "Tag",
                Unique = true
            }));
        }

        internal static async Task<ClanDocument> Create(GroupCreationView groupCreation, PublicProfileView profile)
        {
            _ = 1;
            try
            {
                int id = (int)await sm_database.IncrementSeed("ClanID");
                ClanDocument clan = new ClanDocument
                {
                    ClanId = id,
                    Clan = ClanHelper.GetClanView(groupCreation, id, profile)
                };
                await sm_database.Collection.InsertOneAsync(clan);
                return clan;
            }
            catch (MongoWriteException)
            {
                return null;
            }
        }

        internal static Task<bool> IsClanNameUsed(string name)
        {
            return Task.FromResult<bool>(sm_database.Collection.AsQueryable().Where((ClanDocument f) => f.Clan.Name.ToLower() == name.ToLower()).Count() != 0);
        }

        internal static Task<bool> IsClanTagUsed(string tag)
        {
            return Task.FromResult<bool>(sm_database.Collection.AsQueryable().Where((ClanDocument f) => f.Clan.Tag.ToLower() == tag.ToLower()).Count() != 0);
        }

        internal static Task Save(ClanDocument document)
        {
            return IMongoCollectionExtensions.ReplaceOneAsync(sm_database.Collection, (ClanDocument f) => f.Id == document.Id, document, (ReplaceOptions)null, default);
        }

        internal static Task Remove(ClanDocument document)
        {
            return IMongoCollectionExtensions.DeleteOneAsync(sm_database.Collection, (ClanDocument f) => f.Id == document.Id, default);
        }

        internal static Task<ClanDocument> Get(int id)
        {
            return sm_database.Collection.Find(Builders<ClanDocument>.Filter.Eq((ClanDocument f) => f.ClanId, id)).FirstOrDefaultAsync();
        }
    }
}
