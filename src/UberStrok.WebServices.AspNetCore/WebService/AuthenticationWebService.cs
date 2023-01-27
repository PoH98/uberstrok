using log4net;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UberStrok.Core.Common;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Core.Session;
using UberStrok.WebServices.AspNetCore.WebService.Base;

namespace UberStrok.WebServices.AspNetCore.WebService
{
    public class AuthenticationWebService : BaseAuthenticationWebService
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(AuthenticationWebService));

        public override async Task<AccountCompletionResultView> OnCompleteAccount(int cmid, string name, ChannelType channelType, string locale, string machineId)
        {
            if (Startup.WebServiceConfiguration.Locked)
            {
                return new AccountCompletionResultView(5, null, null);
            }
            if (!ResourceManager.IsNameValid(name))
            {
                return new AccountCompletionResultView(4, null, null);
            }
            if (ResourceManager.IsNameOffensive(name))
            {
                return new AccountCompletionResultView(6, null, null);
            }
            if (await UserManager.IsNameUsed(name))
            {
                return new AccountCompletionResultView(2, null, null);
            }
            if (GameSessionManager.TryGet(cmid, out GameSession session))
            {
                Dictionary<int, int> itemsAttributed = new Dictionary<int, int>();
                foreach (int t in session.Member.MemberItems)
                {
                    itemsAttributed.Add(t, 0);
                }
                session.Member.PublicProfile.Name = name;
                session.Member.PublicProfile.EmailAddressStatus = EmailAddressStatus.Verified;
                try
                {
                    _ = session.Document.Names.Add(name);
                    await UserManager.Save(session.Document);
                }
                catch (MongoWriteException)
                {
                    return new AccountCompletionResultView(2);
                }
                return new AccountCompletionResultView(1, itemsAttributed, null);
            }
            Log.Error("An unidentified cmid was passed.");
            return null;
        }

        public override async Task<MemberAuthenticationResultView> OnLoginSteam(string clientVersion, string steamId, string authToken, string machineId, string hwid, bool isMac)
        {
            _ = 1;
            try
            {
                if (clientVersion != Startup.WebServiceConfiguration.ServerGameVersion)
                {
                    return new MemberAuthenticationResultView
                    {
                        MemberAuthenticationResult = MemberAuthenticationResult.NewUpdate,
                        ServerGameVersion = Startup.WebServiceConfiguration.ServerGameVersion
                    };
                }
                UberBeatManager Manager = new UberBeatManager();
                UserDocument member = await UserManager.GetUser(steamId);
                UberBeat hwidInfo = UberBeatManager.ParseHWIDToObject(hwid);
                if (member == null)
                {
                    member = await UserManager.CreateUser(steamId, hwidInfo);
                    if (member == null)
                    {
                        return new MemberAuthenticationResultView
                        {
                            MemberAuthenticationResult = MemberAuthenticationResult.UnknownError
                        };
                    }
                    if (Manager.AltCmids(member.Profile.Cmid).Count > 1)
                    {
                        await UserManager.DeleteUser(member.Profile.Cmid);
                        return new MemberAuthenticationResultView
                        {
                            MemberAuthenticationResult = MemberAuthenticationResult.IsIpBanned
                        };
                    }
                }
                else
                {
                    Manager.Update(member, hwid);
                    int banduration = Manager.BanDuration(member, hwid);

                    Manager.UserLog(steamId, banduration, hwid);
                    if (banduration != 0)
                    {
                        return new MemberAuthenticationResultView
                        {
                            MemberAuthenticationResult = MemberAuthenticationResult.IsBanned,
                            BanDuration = banduration
                        };
                    }
                }
                if (SecurityManager.IsCmidBanned(member.UserId))
                {
                    return new MemberAuthenticationResultView
                    {
                        MemberAuthenticationResult = MemberAuthenticationResult.IsBanned
                    };
                }
                GameSession session = GameSessionManager.CreateSession(member, null, machineId);
                int muteDuration = Manager.MuteDuration(member, hwid);
                return new MemberAuthenticationResultView
                {
                    MemberAuthenticationResult = MemberAuthenticationResult.Ok,
                    AuthToken = session.SessionId,
                    IsAccountComplete = member.Profile.EmailAddressStatus == EmailAddressStatus.Verified,
                    ServerTime = DateTime.Now,
                    MemberView = session.Member,
                    PlayerStatisticsView = member.Statistics,
                    MuteDuration = muteDuration
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return new MemberAuthenticationResultView
            {
                MemberAuthenticationResult = MemberAuthenticationResult.UnknownError
            };
        }
    }
}
