﻿using log4net;
using UberStrok.Core.Common;

namespace UberStrok.WebServices.Core
{
    public class ModerationWebService : BaseModerationWebService
    {
        private ILog Log;
        public ModerationWebService(WebServiceContext ctx) : base(ctx)
        {
            /* Space */
            Log = LogManager.GetLogger(GetType().Name);
        }

        public override int OnBan(string serviceAuth, int cmid)
        {
            if (Context.Configuration.ServiceAuth != serviceAuth)
                return 1;

            Context.Users.Db.BanCmid(cmid);

            var session = Context.Users.GetSession(cmid);
            if (session == null)
                return 1;

            if (session.Ip != null)
                Context.Users.Db.BanIp(session.Ip);
            if (session.Hwd != null)
                Context.Users.Db.BanHwd(session.Hwd);

            return 0;
        }

        public override int OnUnbanCmid(string authToken, int cmid)
        {
            var member = Context.Users.GetMember(authToken);
            if (member.PublicProfile.AccessLevel < MemberAccessLevel.Moderator)
            {
                Log.Debug("Ban User failed, Access level " + member.PublicProfile.AccessLevel + " not reached");
                return 1;
            }

            Context.Users.Db.UnbanCmid(cmid);
            return 0;
        }

        public override int OnBanCmid(string authToken, int cmid)
        {
            var member = Context.Users.GetMember(authToken);
            if (member.PublicProfile.AccessLevel < MemberAccessLevel.Moderator)
            {
                Log.Debug("Ban User failed, Access level " + member.PublicProfile.AccessLevel + " not reached");
                return 1;
            }

            Context.Users.Db.BanCmid(cmid);
            return 0;
        }

        public override int OnBanHwd(string authToken, string hwd)
        {
            var member = Context.Users.GetMember(authToken);
            if (member.PublicProfile.AccessLevel < MemberAccessLevel.Moderator)
            {
                Log.Debug("Ban User failed, Access level " + member.PublicProfile.AccessLevel + " not reached");
                return 1;
            }

            Context.Users.Db.BanHwd(hwd);
            return 0;
        }

        public override int OnBanIp(string authToken, string ip)
        {
            var member = Context.Users.GetMember(authToken);
            if (member.PublicProfile.AccessLevel < MemberAccessLevel.Moderator)
            {
                Log.Debug("Ban User failed, Access level " + member.PublicProfile.AccessLevel + " not reached");
                return 1;
            }

            Context.Users.Db.BanIp(ip);
            return 0;
        }
    }
}
