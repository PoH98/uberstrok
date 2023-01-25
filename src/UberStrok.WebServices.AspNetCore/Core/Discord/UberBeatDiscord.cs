using System;
using System.Collections.Generic;
using System.Linq;
using UberStrok.WebServices.AspNetCore.Core.Manager;

namespace UberStrok.WebServices.AspNetCore.Core.Discord
{
    internal class UberBeatDiscord
    {
        public static string prefix;

        public string Reply(string[] args)
        {
            UberBeatManager Manager = new UberBeatManager();
            try
            {
                int cmid = 0;
                int duration = -1;
                if (args.Length > 1)
                {
                    _ = int.TryParse(args[1], out cmid);
                }
                if (args.Length > 2)
                {
                    _ = int.TryParse(args[2], out duration);
                }
                string arg = args[0].Replace(prefix, "");
                string message = string.Join(' ', args);
                string retstring5;
                switch (arg)
                {
                    case "alts":
                        {
                            List<string> alts = Manager.Alts(cmid);
                            retstring5 = string.Join(Environment.NewLine + Environment.NewLine, alts);
                            return retstring5.Length > 1900 ? string.Join("|@@|@|", Trim(retstring5, 1900).ToList()) : "```" + retstring5 + "```";
                        }
                    case "hwid":
                        {
                            string[] hwidlist = Manager.GetHWID(cmid).ToArray();
                            return string.Join(Environment.NewLine, hwidlist);
                        }
                    case "search":
                        {
                            if (args[1].Length < 2)
                            {
                                return "Search name should be 2 characters atleast";
                            }
                            List<string> searchlist = Manager.Search(args[1]);
                            return string.Join(Environment.NewLine + Environment.NewLine, searchlist);
                        }
                    case "mute":
                        return Manager.Mute(cmid, duration);
                    case "unmute":
                        return Manager.Unmute(cmid);
                    case "ban":
                        {
                            retstring5 = Manager.Ban(cmid, duration);
                            string returnstr = PhotonSocket.ExecuteClientSocket(message.Replace(prefix, ""));
                            return returnstr + Environment.NewLine + retstring5;
                        }
                    case "unban":
                        return Manager.Unban(cmid);
                    case "status":
                        return "Disabled.";
                    case "push":
                        return "Disabled.";
                    case "players":
                    case "msg":
                    case "kick":
                        return PhotonSocket.ExecuteClientSocket(message.Replace(prefix, ""));
                    case "processes":
                    case "windows":
                    case "modules":
                        retstring5 = PhotonSocket.ExecuteClientSocket(message.Replace(prefix, ""));
                        if (retstring5.Length > 1900)
                        {
                            return string.Join("|@@|@|", Trim(retstring5, 1900).ToList());
                        }
                        return "```" + retstring5 + "```";
                    case "leaderboardkill":
                    case "leaderboardxp":
                    case "leaderboardkdr":
                        {
                            int count = (cmid == 0) ? 100 : cmid;
                            retstring5 = string.Join(Environment.NewLine, Manager.Leaderboard(count, arg.Replace("leaderboard", "")));
                            return retstring5.Length > 1900 ? string.Join("|@@|@|", Trim(retstring5, 1900).ToList()) : "```" + retstring5 + "```";
                        }
                    default:
                        return null;
                    case "banned":
                        retstring5 = string.Join(Environment.NewLine, Manager.bannedUsers());
                        if (retstring5.Length > 1900)
                        {
                            return string.Join("|@@|@|", Trim(retstring5, 1900).ToList());
                        }
                        return "```" + retstring5 + "```";
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private List<string> Trim(string stringToSplit, int maximumLineLength)
        {
            IEnumerable<string> lines = stringToSplit.Split(new string[1]
            {
                Environment.NewLine
            }, StringSplitOptions.None).Concat<string>(new string[1]
            {
                ""
            });
            return lines.Skip(1).Aggregate(lines.Take(1).ToList(), delegate (List<string> a, string w)
            {
                string text = a.Last();
                while (text.Length > maximumLineLength)
                {
                    a[a.Count() - 1] = text[..maximumLineLength];
                    text = text[maximumLineLength..];
                    a.Add(text);
                }
                string text2 = text + Environment.NewLine + w;
                if (text2.Length > maximumLineLength)
                {
                    a.Add(w);
                }
                else
                {
                    a[a.Count() - 1] = text2;
                }
                return a;
            }).ToList();
        }
    }
}
