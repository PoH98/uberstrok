using LiteDB;

namespace UberStrok.WebServices.AspNetCore.Database.LiteDb
{
    public class LiteDbService : IDbService
    {
        private readonly LiteDatabase _db;

        private readonly LiteDbSessionCollection _sessions;
        private readonly LiteDbClanCollection _clans;
        private readonly LiteDbMemberCollection _members;

        public IDbSessionCollection Sessions => _sessions;
        public IDbClanCollection Clans => _clans;
        public IDbMemberCollection Members => _members;

        public LiteDbService()
        {
            _db = new LiteDatabase("Filename=uberstrok.db;connection=shared;");
            _clans = new LiteDbClanCollection(_db);
            _members = new LiteDbMemberCollection(_db);
            _sessions = new LiteDbSessionCollection(_db);
        }
    }
}
