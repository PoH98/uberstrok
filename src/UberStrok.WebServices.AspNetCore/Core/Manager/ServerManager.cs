using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using UberStrok.WebServices.AspNetCore.Core.Db;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public static class ServerManager
    {
        private static MongoDatabase<UberBeatDocument> sm_database;
        private static UberBeatDocument _document;
        public static UberBeatDocument Document
        {
            get
            {
                _document = sm_database.Collection.Find(_ => true).FirstOrDefault();
                return _document;
            }
        }

        public static void Init()
        {
            sm_database = new MongoDatabase<UberBeatDocument>("ExceptionData");
            _document = sm_database.Collection.Find(_ => true).FirstOrDefault();
            _document ??= CreateEmpty();
        }

        public static void Append(string exceptiondata)
        {
            _document.ExceptionData.Add(exceptiondata);
            _ = sm_database.Collection.ReplaceOne((UberBeatDocument f) => f.Id == _document.Id, _document, (ReplaceOptions)null, default);
        }

        public static void Remove(string exceptiondata)
        {
            _ = _document.ExceptionData.Remove(exceptiondata);
            _ = sm_database.Collection.ReplaceOne((UberBeatDocument f) => f.Id == _document.Id, _document, (ReplaceOptions)null, default);
        }

        public static string GetExceptionData()
        {
            return string.Join(Environment.NewLine, _document.ExceptionData);
        }

        public static UberBeatDocument CreateEmpty()
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Note Dont forget to add Exception Data to DB\n");
            Console.ResetColor();
            UberBeatDocument doc = new UberBeatDocument
            {
                ExceptionData = new List<string> { "Default string" }
            };
            sm_database.Collection.InsertOne(doc);
            return doc;
        }
    }
}
