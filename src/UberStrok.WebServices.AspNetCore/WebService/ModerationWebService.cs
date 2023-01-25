using UberStrok.Core.Common;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Core.Session;
using UberStrok.WebServices.AspNetCore.WebService.Base;

namespace UberStrok.WebServices.AspNetCore.WebService
{
    public class ModerationWebService : BaseModerationWebService
    {
        public override int OnBan(string serviceAuth, int cmid)
        {
            SecurityManager.BanCmid(cmid);
            if (GameSessionManager.TryGet(cmid, out GameSession session))
            {
                if (session.IPAddress != null)
                {
                    SecurityManager.BanIp(session.IPAddress);
                }
                if (session.MachineId != null)
                {
                    SecurityManager.BanHwd(session.MachineId);
                }
            }
            return 0;
        }

        public override int OnBanIp(string authToken, string ip)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Member.PublicProfile.AccessLevel < MemberAccessLevel.SeniorModerator)
                {
                    return 1;
                }
                SecurityManager.BanIp(ip);
            }
            return 0;
        }

        public override int OnBanCmid(string authToken, int cmid)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Member.PublicProfile.AccessLevel < MemberAccessLevel.SeniorQA)
                {
                    return 1;
                }
                SecurityManager.BanCmid(cmid);
            }
            return 0;
        }

        public override int OnBanHwd(string authToken, string hwd)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Member.PublicProfile.AccessLevel < MemberAccessLevel.SeniorModerator)
                {
                    return 1;
                }
                SecurityManager.BanHwd(hwd);
            }
            return 0;
        }

        public override int OnUnbanCmid(string authToken, int cmid)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Member.PublicProfile.AccessLevel < MemberAccessLevel.SeniorQA)
                {
                    return 1;
                }
                SecurityManager.UnbanCmid(cmid);
            }
            return 0;
        }
    }
}
