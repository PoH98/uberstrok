using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Database;
using UberStrok.WebServices.AspNetCore.Models;

namespace UberStrok.WebServices.AspNetCore
{
    public class ClanWebService : BaseClanWebService
    {
        private readonly IDbService _database;
        private readonly ISessionService _sessions;
        private readonly ILogger<ClanWebService> _logger;
        private readonly Regex ClanRegex = new Regex("[a-zA-Z0-9 .!_\\-<>{}~@#$%^&*()=+|:?]", RegexOptions.Compiled);

        public ClanWebService(ILogger<ClanWebService> logger, IDbService database, ISessionService sessions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }
       
        public override async Task<ClanRequestAcceptView> OnAcceptClanInvitation(int clanInvitationId, string authToken)
        {
            try
            {
                var data = await _database.ClanInvitations.FindAsync(clanInvitationId);
                if (data != null)
                {
                    var clan = await _database.Clans.FindAsync(data.View.GroupId);
                    var member = await _sessions.GetMemberAsync(authToken);
                    clan.Members.Add(new ClanMember(member)
                    {
                        JoinDate = DateTime.Now
                    });
                    member.Clan = clan;
                    member.ClanId = clan.Id;
                    _ = _database.Clans.UpdateAsync(clan);
                    _ = _database.ClanInvitations.DeleteAsync(data);
                    _ = _database.Members.UpdateAsync(member);
                    return new ClanRequestAcceptView
                    {
                        ActionResult = 0,
                        ClanView = new ClanView()
                        {
                            MembersCount = clan.Members.Count,
                            OwnerCmid = clan.LeaderId,
                            Name = clan.Name,
                            Motto = clan.Motto,
                            GroupId = clan.Id,
                            Tag = clan.Tag,
                            Members = clan.Members.Select(x =>
                            {
                                var member = _database.Members.FindAsync(x.CmId).Result;
                                return new ClanMemberView
                                {
                                    Cmid = x.CmId,
                                    Name = member.Name,
                                    Lastlogin = member.LastLogin,
                                    JoiningDate = x.JoinDate
                                };
                            }).ToList()

                        }
                    };
                }
            }
            catch
            {

            }
            return new ClanRequestAcceptView
            {
                ActionResult = 69
            };
        }

        public override async Task<int> OnCancelInvite(int clanInvitationId, string authToken)
        {
            var data = await _database.ClanInvitations.FindAsync(clanInvitationId);
            if(data != null)
            {
                _ = _database.ClanInvitations.DeleteAsync(data);
            }
            return 0;
        }

        public override async Task<ClanCreationReturnView> OnCreateClan(GroupCreationView creationView)
        {
            var session = await _sessions.GetMemberAsync(creationView.AuthToken);
            if(session == null)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 69
                };
            }
            var clans = (await _database.Clans.Where(x => x.Name == creationView.Name || x.Tag == creationView.Tag )).ToList();
            if (!ValidName(creationView.Name) || creationView.Name.Length < 3 || creationView.Name.Length > 25)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 1
                };
            }
            if (session.ClanId != 0)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 2
                };
            }
            if(clans.Any(x => x.Name == creationView.Name))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 3
                };
            }
            if(!ValidName(creationView.Tag) || creationView.Tag.Length < 2 || creationView.Tag.Length > 5)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 4
                };
            }
            if (clans.Any(x => x.Tag == creationView.Tag))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 10
                };
            }
            if (!ValidName(creationView.Motto) || creationView.Motto.Length < 3 || creationView.Motto.Length > 25)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 8
                };
            }
            var clan = new Clan
            {
                LeaderId = session.Id,
                Name = creationView.Name,
                Tag = creationView.Tag,
                Motto = creationView.Motto,
                Members = new List<ClanMember>()
            };
            clan.Members.Add(new ClanMember(session)
            {
               JoinDate = DateTime.Now
            });
            session.ClanId = clan.Id;
            session.Clan = clan;
            _ = _database.Members.UpdateAsync(session);
            _ = _database.Clans.InsertAsync(clan);
            return new ClanCreationReturnView
            {
                ClanView = new ClanView
                {
                    MembersCount = clan.Members.Count,
                    OwnerCmid = clan.LeaderId,
                    Name = clan.Name,
                    Motto = clan.Motto,
                    GroupId = clan.Id,
                    Tag = clan.Tag,
                    Members = clan.Members.Select(x =>
                    {
                        var member = _database.Members.FindAsync(x.CmId).Result;
                        return new ClanMemberView
                        {
                            Cmid = x.CmId,
                            Name = member.Name,
                            Lastlogin = member.LastLogin,
                            JoiningDate = x.JoinDate
                        };
                    }).ToList(),
                }
            };
        }

        private bool ValidName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return ClanRegex.IsMatch(name);
            }
            return false;
        }

        public override async Task<ClanRequestDeclineView> OnDeclineClanInvitation(int clanInvitationId, string authToken)
        {
            await OnCancelInvite(clanInvitationId, authToken);
            return new ClanRequestDeclineView();
        }

        public override Task<int> OnDisbandClan(int groupId, string authToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<List<GroupInvitationView>> OnGetAllGroupInvitations(string authToken)
        {
            throw new System.NotImplementedException();
        }

        public override int OnGetMyClanId(string authToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<ClanView> OnGetOwnClan(string authToken, int groupId)
        {
            throw new System.NotImplementedException();
        }

        public override Task<List<GroupInvitationView>> OnGetPendingGroupInvitations(int groupId, string authToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<int> OnInviteMemberToJoinAGroup(int clanId, string authToken, int invitee, string message)
        {
            throw new System.NotImplementedException();
        }

        public override Task<int> OnKick(int groupId, string authToken, int cmidToKick)
        {
            throw new System.NotImplementedException();
        }

        public override Task<int> OnLeaveClan(int groupId, string authToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<int> OnTransferOwnership(int groupId, string authToken, int newLeaderCmid)
        {
            throw new System.NotImplementedException();
        }

        public override Task<int> OnUpdateMemberPosition(MemberPositionUpdateView memberPositionUpdate)
        {
            throw new System.NotImplementedException();
        }
    }
}
