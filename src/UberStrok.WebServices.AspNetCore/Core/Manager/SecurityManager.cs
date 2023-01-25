using MongoDB.Driver;
using System;
using System.Collections.Generic;
using UberStrok.WebServices.AspNetCore.Core.Db;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public static class SecurityManager
    {
        private class SecurityDocument : MongoDocument
        {
            public string Type
            {
                get => "Security";
                set
                {
                }
            }

            public HashSet<int> CmidBans
            {
                get;
                set;
            }

            public HashSet<string> IpBans
            {
                get;
                set;
            }

            public HashSet<string> HwdBans
            {
                get;
                set;
            }
        }

        private static HashSet<int> sm_cmidBans;

        private static HashSet<string> sm_ipBans;

        private static HashSet<string> sm_hwdBans;

        private static MongoDatabase<SecurityDocument> sm_database;

        private static readonly FilterDefinition<SecurityDocument> sm_filter = Builders<SecurityDocument>.Filter.Eq((SecurityDocument f) => f.Type, "Security");

        public static void Init()
        {
            sm_database = new MongoDatabase<SecurityDocument>("Servers");
            SecurityDocument document = sm_database.Collection.Find(sm_filter).FirstOrDefault();
            if (document != null)
            {
                sm_cmidBans = document.CmidBans;
                sm_ipBans = document.IpBans;
                sm_hwdBans = document.HwdBans;
            }
            else
            {
                sm_cmidBans = new HashSet<int>();
                sm_ipBans = new HashSet<string>();
                sm_hwdBans = new HashSet<string>();
            }
        }

        public static bool IsCmidBanned(int cmid)
        {
            return sm_cmidBans.Contains(cmid);
        }

        public static void BanCmid(int cmid)
        {
            if (!sm_cmidBans.Contains(cmid))
            {
                _ = sm_cmidBans.Add(cmid);
                _ = sm_database.Collection.UpdateOneAsync(sm_filter, Builders<SecurityDocument>.Update.AddToSet((SecurityDocument document) => document.CmidBans, cmid), new UpdateOptions
                {
                    IsUpsert = true
                });
            }
        }

        public static void UnbanCmid(int cmid)
        {
            if (sm_cmidBans.Contains(cmid))
            {
                _ = sm_cmidBans.Remove(cmid);
                _ = sm_database.Collection.UpdateOneAsync(sm_filter, Builders<SecurityDocument>.Update.Pull((SecurityDocument document) => document.CmidBans, cmid), new UpdateOptions
                {
                    IsUpsert = true
                });
            }
        }

        public static bool IsHwdBanned(string hwd)
        {
            return hwd == null ? throw new ArgumentNullException("hwd") : sm_hwdBans.Contains(hwd);
        }

        public static void BanHwd(string hwd)
        {
            if (!sm_hwdBans.Contains(hwd))
            {
                _ = sm_hwdBans.Add(hwd);
                _ = sm_database.Collection.UpdateOneAsync(sm_filter, Builders<SecurityDocument>.Update.AddToSet((SecurityDocument document) => document.HwdBans, hwd), new UpdateOptions
                {
                    IsUpsert = true
                });
            }
        }

        public static bool IsIpBanned(string ip)
        {
            return ip == null ? throw new ArgumentNullException("ip") : sm_ipBans.Contains(ip);
        }

        public static void BanIp(string ip)
        {
            if (!sm_ipBans.Contains(ip))
            {
                _ = sm_ipBans.Add(ip);
                _ = sm_database.Collection.UpdateOneAsync(sm_filter, Builders<SecurityDocument>.Update.AddToSet((SecurityDocument document) => document.IpBans, ip), new UpdateOptions
                {
                    IsUpsert = true
                });
            }
        }
    }
}
