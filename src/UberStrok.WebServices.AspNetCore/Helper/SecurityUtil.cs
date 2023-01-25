using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;
using UberStrok.WebServices.AspNetCore.Core.Manager;

namespace UberStrok.WebServices.AspNetCore.Helper
{
    // Token: 0x02000026 RID: 38
    public static class SecurityUtil
    {
        // Token: 0x06000161 RID: 353 RVA: 0x00008B60 File Offset: 0x00006D60
        public static async Task<string> GetHwdBan(UberBeat hwid)
        {
            hwid.MOTHERBOARD.ExceptWith(UberBeatManager.ExceptionData);
            hwid.BIOS.ExceptWith(UberBeatManager.ExceptionData);
            hwid.MAC.ExceptWith(UberBeatManager.ExceptionData);
            hwid.HDD.ExceptWith(UberBeatManager.ExceptionData);
            string result;
            if (hwid.MOTHERBOARD.Count + hwid.BIOS.Count + hwid.MAC.Count + hwid.HDD.Count == 0)
            {
                result = null;
            }
            else
            {
                List<FilterDefinition<UserDocument>> list = new List<FilterDefinition<UserDocument>>();
                if (hwid.MOTHERBOARD.Count > 0)
                {
                    list.Add(Builders<UserDocument>.Filter.AnyIn((u) => u.MOTHERBOARD, hwid.MOTHERBOARD));
                }
                if (hwid.BIOS.Count > 0)
                {
                    list.Add(Builders<UserDocument>.Filter.AnyIn((u) => u.BIOS, hwid.BIOS));
                }
                if (hwid.MAC.Count > 0)
                {
                    list.Add(Builders<UserDocument>.Filter.AnyIn((u) => u.MAC, hwid.MAC));
                }
                if (hwid.HDD.Count > 0)
                {
                    list.Add(Builders<UserDocument>.Filter.AnyIn((u) => u.HDD, hwid.HDD));
                }
                string text = await UserManager.Database.Collection.Find(Builders<UserDocument>.Filter.Exists((u) => u.UBBan, true) & Builders<UserDocument>.Filter.Or(list), null).Project((u) => u.UBBan).FirstOrDefaultAsync(default);
                result = text;
            }
            return result;
        }
    }
}
