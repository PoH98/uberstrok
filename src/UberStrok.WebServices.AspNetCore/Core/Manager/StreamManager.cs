using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using UberStrok.WebServices.AspNetCore.Core.Db;
using UberStrok.WebServices.AspNetCore.Core.Db.Items.Stream;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public static class StreamManager
    {
        private static MongoDatabase<StreamDocument> sm_database;

        internal static void Init()
        {
            sm_database = new MongoDatabase<StreamDocument>("Stream");
            sm_database.InitSequence();
        }

        internal static async Task<int> GetNextId()
        {
            return (int)await sm_database.IncrementSeed("StreamID");
        }

        internal static Task Create(StreamDocument document)
        {
            return sm_database.Collection.InsertOneAsync(document);
        }

        internal static Task Save(StreamDocument document)
        {
            return IMongoCollectionExtensions.ReplaceOneAsync(sm_database.Collection, (StreamDocument f) => f.Id == document.Id, document, (ReplaceOptions)null, default);
        }

        internal static Task<StreamDocument> Get(int id)
        {
            return sm_database.Collection.Find(Builders<StreamDocument>.Filter.Eq((StreamDocument f) => f.StreamId, id)).FirstOrDefaultAsync();
        }

        internal static Task<StreamDocument[]> Get(int[] ids)
        {
            Task<StreamDocument>[] docs = new Task<StreamDocument>[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                docs[i] = Get(ids[i]);
            }
            return Task.WhenAll<StreamDocument>(docs);
        }

        internal static Task<StreamDocument[]> Get(List<int> ids)
        {
            Task<StreamDocument>[] docs = new Task<StreamDocument>[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                docs[i] = Get(ids[i]);
            }
            return Task.WhenAll<StreamDocument>(docs);
        }
    }
}
