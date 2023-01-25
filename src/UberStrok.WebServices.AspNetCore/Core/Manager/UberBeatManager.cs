using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;
using UberStrok.WebServices.AspNetCore.Core.Discord;

namespace UberStrok.WebServices.AspNetCore.Core.Manager
{
    public class UberBeatManager
    {
        public static List<string> ExceptionData => ServerManager.Document.ExceptionData;

        private static readonly ILog Log = LogManager.GetLogger(typeof(UberBeatManager));
        public void Update(UserDocument document, string hwid)
        {
            try
            {
                if (document != null)
                {
                    UberBeat obj = ParseHWIDToObject(hwid);
                    if (obj != null)
                    {
                        document.HDD.UnionWith(obj.HDD);
                        document.BIOS.UnionWith(obj.BIOS);
                        document.MOTHERBOARD.UnionWith(obj.MOTHERBOARD);
                        document.MAC.UnionWith(obj.MAC);
                        document.UNITY.UnionWith(obj.UNITY);
                        _ = UserManager.Save(document);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public string Mute(int cmid, int duration = 0)
        {
            try
            {
                UserDocument member = UserManager.GetUser(cmid).Result;
                if (duration > 0)
                {
                    member.UBMute = DateTime.UtcNow.AddMinutes(duration).ToString();
                    _ = UserManager.Save(member);
                    return $"{member.Profile.Name} has been muted for {duration} minutes.";
                }
                member.UBMute = "-1";
                _ = UserManager.Save(member);
                return $"{member.Profile.Name} has been muted permanently.";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public void UserLog(string steamid, int duration, string hwid)
        {
            UserDocument member = UserManager.GetUser(steamid).Result;
            if (member == null)
            {
                switch (duration)
                {
                    case -1:
                        UDPListener.SendDiscord("``Permanently Banned user with HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has been kicked.", login: true);
                        break;
                    case 0:
                        UDPListener.SendDiscord("``User with HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has logged in.", login: true);
                        break;
                    default:
                        UDPListener.SendDiscord("``Temporarily Banned user ( for " + duration.ToString() + " more minutes ) with HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has logged in.", login: true);
                        break;
                }
            }
            else
            {
                switch (duration)
                {
                    case -1:
                        UDPListener.SendDiscord("``Permanently banned user with Name: " + member.Profile.Name + " and HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has been kicked.", login: true);
                        break;
                    case 0:
                        UDPListener.SendDiscord("``User with Name: " + member.Profile.Name + " and HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has logged in.", login: true);
                        break;
                    default:
                        UDPListener.SendDiscord("``Temporarily Banned user ( for " + duration.ToString() + " more minutes ) with Name " + member.Profile.Name + " and HWID:``" + Environment.NewLine + "```" + hwid.Replace("|", Environment.NewLine) + "```has logged in.", login: true);
                        break;
                }
            }
        }

        public string Unmute(int cmid)
        {
            try
            {
                UserDocument member = UserManager.GetUser(cmid).Result;
                if (member == null)
                {
                    return $"User with cmid {cmid} does not exist.";
                }
                if (member.UBMute == null)
                {
                    return $"User with cmid {cmid} and name {member.Profile.Name} is not muted.";
                }
                member.UBBan = null;
                _ = UserManager.Save(member);
                return $"User with cmid {cmid} and name {member.Profile.Name} has been unmuted.";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public int MuteDuration(UserDocument member, string hwid)
        {
            List<int> duration = new List<int>();
            try
            {
                if (member != null)
                {
                    if (!string.IsNullOrEmpty(member.UBMute))
                    {
                        if (member.UBMute == "-1")
                        {
                            return -1;
                        }
                        DateTime Date = DateTime.Parse(member.UBMute);
                        if (Date > DateTime.UtcNow)
                        {
                            duration.Add((int)(Date - DateTime.UtcNow).TotalMinutes + 1);
                        }
                        else if (Date <= DateTime.UtcNow)
                        {
                            member.UBMute = null;
                            _ = UserManager.Save(member);
                        }
                    }
                    foreach (int cmid2 in AltCmids(member.Profile.Cmid))
                    {
                        duration.Add(GetDuration(cmid2, muteduration: true));
                    }
                }
                foreach (int cmid in AltCmids(hwid))
                {
                    duration.Add(GetDuration(cmid, muteduration: true));
                }
                return duration.Contains(-1) ? -1 : duration.Max();
            }
            catch
            {
                if (duration.Count > 0)
                {
                    return duration.Max();
                }
            }
            return 0;
        }

        public string Ban(int cmid, int duration = 0)
        {
            try
            {
                UserDocument member = UserManager.GetUser(cmid).Result;
                if (duration > 0)
                {
                    member.UBBan = DateTime.UtcNow.AddMinutes(duration).ToString();
                    _ = UserManager.Save(member);
                    return $"{member.Profile.Name} has been banned for {duration} minutes.";
                }
                member.UBBan = "-1";
                _ = UserManager.Save(member);
                return $"{member.Profile.Name} has been banned permanently.";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string Unban(int cmid)
        {
            try
            {
                UserDocument member = UserManager.GetUser(cmid).Result;
                if (member == null)
                {
                    return $"User with cmid {cmid} does not exist.";
                }
                if (member.UBBan == null)
                {
                    return $"User with cmid {cmid} and name {member.Profile.Name} is not banned.";
                }
                member.UBBan = null;
                _ = UserManager.Save(member);
                return $"User with cmid {cmid} and name {member.Profile.Name} has been unbanned.";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public int BanDuration(UserDocument member, string hwid)
        {
            List<int> duration = new List<int>();
            try
            {
                if (member != null)
                {
                    if (!string.IsNullOrEmpty(member.UBBan))
                    {
                        if (member.UBBan == "-1")
                        {
                            return -1;
                        }
                        DateTime Date = DateTime.Parse(member.UBBan);
                        if (Date > DateTime.UtcNow)
                        {
                            duration.Add((int)(Date - DateTime.UtcNow).TotalMinutes + 1);
                        }
                        else if (Date <= DateTime.UtcNow)
                        {
                            member.UBBan = null;
                            _ = UserManager.Save(member);
                        }
                    }
                }
                else
                {
                    foreach (int cmid in AltCmids(hwid))
                    {
                        duration.Add(GetDuration(cmid));
                    }
                    if (duration.Contains(-1))
                    {
                        return -1;
                    }
                }
                return duration.Max();
            }
            catch
            {
                if (duration.Count > 0)
                {
                    return duration.Max();
                }
            }
            return 0;
        }

        private static int GetDuration(int cmid, bool muteduration = false)
        {
            UserDocument doc = UserManager.GetUser(cmid).GetAwaiter().GetResult();
            string datestr = (!muteduration) ? doc.UBBan : doc.UBMute;
            if (string.IsNullOrEmpty(datestr))
            {
                return 0;
            }
            if (datestr == "-1")
            {
                return -1;
            }
            try
            {
                DateTime date = DateTime.Parse(datestr);
                if (date > DateTime.UtcNow)
                {
                    return (int)(date - DateTime.UtcNow).TotalMinutes + 1;
                }
                if (muteduration)
                {
                    doc.UBMute = null;
                }
                else
                {
                    doc.UBBan = null;
                }
                _ = UserManager.Save(doc);
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private void FilterExceptionData(ref UberBeat userdata)
        {

            HashSet<string> ExceptionDataSet = new HashSet<string>(ExceptionData);
            userdata.HDD.ExceptWith(ExceptionDataSet);
            userdata.BIOS.ExceptWith(ExceptionDataSet);
            userdata.MOTHERBOARD.ExceptWith(ExceptionDataSet);
            userdata.UNITY.ExceptWith(ExceptionDataSet);
            userdata.MAC.ExceptWith(ExceptionDataSet);
        }

        public UberBeat ParseHWIDToObject(string data)
        {
            UberBeat newuserdata = new UberBeat();
            try
            {
                string[] hwid = data.Split("|").Distinct().ToArray();
                string[] array = hwid;
                foreach (string line in array)
                {
                    if (line.Contains("MAC:"))
                    {
                        string newline5 = line.Replace("MAC:", "");
                        _ = newuserdata.MAC.Add(newline5);
                    }
                    if (line.Contains("HDD:"))
                    {
                        string newline4 = line.Replace("HDD:", "");
                        _ = newuserdata.HDD.Add(newline4);
                    }
                    if (line.Contains("BIOS:"))
                    {
                        string newline3 = line.Replace("BIOS:", "");
                        _ = newuserdata.BIOS.Add(newline3);
                    }
                    if (line.Contains("MOTHERBOARD:"))
                    {
                        string newline2 = line.Replace("MOTHERBOARD:", "");
                        _ = newuserdata.MOTHERBOARD.Add(newline2);
                    }
                    /*if (line.Contains("UNITY:"))
					{
						string newline = line.Replace("UNITY:", "");
						newuserdata.UNITY.Add(newline);
					}*/
                }
                return newuserdata;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return newuserdata;
            }
        }

        public List<string> GetHWID(int cmid)
        {
            UserDocument target = UserManager.GetUser(cmid).Result;
            List<string> returnlist = new List<string>();
            if (target == null)
            {
                returnlist.Add("Invalid CMID");
                return returnlist;
            }
            foreach (string name in target.Names)
            {
                returnlist.Add("NAME:" + name);
            }
            foreach (string hdd in target.HDD)
            {
                returnlist.Add("HDD:" + hdd);
            }
            foreach (string mac in target.MAC)
            {
                returnlist.Add("MAC:" + mac);
            }
            foreach (string motherboard in target.MOTHERBOARD)
            {
                returnlist.Add("MOTHERBOARD:" + motherboard);
            }
            foreach (string bios in target.BIOS)
            {
                returnlist.Add("BIOS:" + bios);
            }
            foreach (string unity in target.UNITY)
            {
                returnlist.Add("UNITY:" + unity);
            }
            return returnlist;
        }

        public List<int> AltCmids(string hwid)
        {
            return FindMatchingCmid(ParseHWIDToObject(hwid));
        }

        private List<int> FindMatchingCmid(UberBeat ub)
        {
            List<int> alts = new List<int>();
            FilterExceptionData(ref ub);
            HashSet<int>[] result = Task.WhenAll(UserManager.MatchHWID("bios", ub.BIOS),
                UserManager.MatchHWID("mac", ub.MAC),
                UserManager.MatchHWID("mac", ub.MAC),
                UserManager.MatchHWID("hdd", ub.HDD),
                UserManager.MatchHWID("motherboard", ub.MOTHERBOARD)).GetAwaiter().GetResult();
            HashSet<int> bios = result[0];
            HashSet<int> mac = result[1];
            HashSet<int> hdd = result[2];
            HashSet<int> motherboard = result[3];
            List<int> overallAlts = new List<int>();
            overallAlts.AddRange(bios); overallAlts.AddRange(mac); overallAlts.AddRange(hdd); overallAlts.AddRange(motherboard);
            IEnumerable<IGrouping<int, int>> keypair = overallAlts.GroupBy(i => i);
            foreach (IGrouping<int, int> pair in keypair)
            {
                if (pair.Count() > 1 && !alts.Contains(pair.Key))
                {
                    alts.Add(pair.Key);
                }
            }
            return alts;
        }

        public List<int> AltCmids(int cmid)
        {
            UserDocument doc = UserManager.GetUser(cmid).GetAwaiter().GetResult();
            UberBeat ub = new UberBeat
            {
                HDD = doc.HDD,
                BIOS = doc.BIOS,
                MAC = doc.MAC,
                MOTHERBOARD = doc.MOTHERBOARD
            };
            return FindMatchingCmid(ub);
        }

        public List<string> Search(string name)
        {
            try
            {
                List<string> result = new List<string>();
                List<UserDocument> list = UserManager.FindUser(name).Result;
                if (list.Count < 1)
                {
                    result.Add("Cant find name with such characters");
                    return result;
                }
                foreach (UserDocument doc in list)
                {
                    string name2 = (doc.Names.Count > 0) ? string.Join(Environment.NewLine, doc.Names) : doc.Profile.Name;
                    result.Add($"CMID: {doc.Profile.Cmid} Name: {name2}");
                }
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public List<string> Alts(int cmid)
        {
            List<string> result = new List<string>();
            foreach (int cmid2 in AltCmids(cmid))
            {
                UserDocument doc = UserManager.GetUser(cmid2).Result;
                if (doc != null)
                {
                    result.Add($"CMID: {doc.Profile.Cmid} Name: {string.Join(Environment.NewLine, doc.Names)}");
                }
            }
            return result;
        }

        public List<string> Leaderboard(int limit, string key)
        {
            try
            {
                List<UserDocument> docs = UserManager.Leaderboard(limit, key).Result;
                List<string> result = new List<string>();
                int i = 1;
                foreach (UserDocument doc in docs)
                {
                    string clantag = null;
                    string kda = getKda(doc.Kills, doc.Deaths).ToString();
                    if (!string.IsNullOrEmpty(doc.Profile.GroupTag))
                    {
                        clantag = "[" + doc.Profile.GroupTag + "] ";
                    }
                    result.Add($"[{i}]    > {clantag}{doc.Profile.Name}\n           Kills {doc.Kills} - Level {doc.Statistics.Level} - {doc.Statistics.Xp} XP - KDR {kda}\n");
                    i++;
                }
                return result;
            }
            catch (Exception)
            {
                return new string[1]
                {
                    "Error"
                }.ToList();
            }
        }

        public List<string> bannedUsers()
        {
            try
            {
                List<UserDocument> docs = UserManager.bannedUsers().Result;
                List<string> result = new List<string>();
                foreach (UserDocument doc in docs)
                {
                    string clantag = null;
                    if (!string.IsNullOrEmpty(doc.Profile.GroupTag))
                    {
                        clantag = "{" + doc.Profile.GroupTag + "} ";
                    }
                    result.Add($"[{doc.Profile.Cmid}]  {clantag}{doc.Profile.Name}\n");
                }
                return result;
            }
            catch (Exception)
            {
                return new string[1]
                {
                    "Error"
                }.ToList();
            }
        }

        private double getKda(double kills, double deaths)
        {
            if (deaths == 0.0)
            {
                deaths = 1.0;
            }
            double kda = kills / deaths;
            return Math.Truncate(100.0 * kda) / 100.0;
        }
    }
}
