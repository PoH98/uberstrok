using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Core.Db.Items;
using UberStrok.WebServices.AspNetCore.Core.Db.Items.Stream;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Core.Session;
using UberStrok.WebServices.AspNetCore.Helper;
using UberStrok.WebServices.AspNetCore.WebService.Base;

namespace UberStrok.WebServices.AspNetCore.WebService
{
    public class ClanWebService : BaseClanWebService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClanWebService));

        private static readonly Regex ClanRegex = new Regex("[a-zA-Z0-9 .!_\\-<>{}~@#$%^&*()=+|:?]", RegexOptions.Compiled);
        public override async Task<ClanView> OnGetOwnClan(string authToken, int groupId)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == groupId)
                {
                    ClanDocument clan = await ClanManager.Get(groupId);
                    if (clan != null)
                    {
                        return clan.Clan;
                    }
                    Log.Error("ClanManager.Get returned null");
                }
                else
                {
                    Log.Error("Mismatch between groupId and session.Document.ClanId.");
                }
            }
            else
            {
                Log.Error("An unidentified AuthToken was passed.");
            }
            return null;
        }

        public override async Task<ClanCreationReturnView> OnCreateClan(GroupCreationView groupCreation)
        {
            if (!GameSessionManager.TryGet(groupCreation.AuthToken, out GameSession session))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 69
                };
            }
            if (!ValidString(groupCreation.Name) || groupCreation.Name.Length < 3 || groupCreation.Name.Length > 25)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 1
                };
            }
            if (ClanHelper.IsTagLocked(session.Document.UserId, groupCreation.Tag))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 690
                };
            }
            if (session.Document.ClanId != 0)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 2
                };
            }
            if (await ClanManager.IsClanNameUsed(groupCreation.Name))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 3
                };
            }
            if (!ValidString(groupCreation.Tag) || groupCreation.Tag.Length < 2 || groupCreation.Tag.Length > 5)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 4
                };
            }
            if (await ClanManager.IsClanTagUsed(groupCreation.Tag))
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 10
                };
            }
            if (!ValidString(groupCreation.Motto) || groupCreation.Motto.Length < 3 || groupCreation.Motto.Length > 25)
            {
                return new ClanCreationReturnView
                {
                    ResultCode = 8
                };
            }
            ClanDocument document = await ClanManager.Create(groupCreation, session.Document.Profile);
            if (document.Clan.AddMemberToClan(ClanMemberHelper.GetClanMemberView(session.Document, GroupPosition.Leader)))
            {
                session.Document.ClanId = document.ClanId;
                session.Document.Profile.GroupTag = document.Clan.Tag;
                await UserManager.Save(session.Document);
                await ClanManager.Save(document);
                return new ClanCreationReturnView
                {
                    ClanView = document.Clan
                };
            }
            return new ClanCreationReturnView
            {
                ResultCode = 12345
            };
        }

        public override async Task<List<GroupInvitationView>> OnGetAllGroupInvitations(string authToken)
        {
            return GameSessionManager.TryGet(authToken, out GameSession session)
                ? (await StreamManager.Get(session.Document.Streams)).Where((f) => f.StreamType == StreamType.GroupInvitation && ((GroupInvitationStream)f).GroupInvitation.InviteeCmid == session.Document.UserId).Select((s) => ((GroupInvitationStream)s).GroupInvitation).ToList()
                : null;
        }

        public override async Task<List<GroupInvitationView>> OnGetPendingGroupInvitations(int groupId, string authToken)
        {
            return GameSessionManager.TryGet(authToken, out GameSession session)
                ? (await StreamManager.Get(session.Document.Streams)).Where((f) => f.StreamType == StreamType.GroupInvitation && ((GroupInvitationStream)f).GroupInvitation.InviteeCmid != session.Document.UserId).Select((s) => ((GroupInvitationStream)s).GroupInvitation).ToList()
                : null;
        }

        public override async Task<int> OnUpdateMemberPosition(MemberPositionUpdateView memberPositionUpdate)
        {
            if (!GameSessionManager.TryGet(memberPositionUpdate.AuthToken, out GameSession session))
            {
                Log.Error("An unidentified AuthToken was passed.");
                return -1;
            }
            if (session.Document.ClanId != memberPositionUpdate.GroupId)
            {
                Log.Error("Mismatch between groupId and session.Document.ClanId.");
                return -1;
            }
            ClanDocument document = await ClanManager.Get(memberPositionUpdate.GroupId);
            if (document != null)
            {
                ClanMemberView executorMemberView = document.Clan.GetMember(session.Document.UserId);
                ClanMemberView targetMemberView = document.Clan.GetMember(memberPositionUpdate.MemberCmid);
                if (executorMemberView != null && targetMemberView != null)
                {
                    if (executorMemberView.HasHigherPermissionThan(targetMemberView.Position))
                    {
                        targetMemberView.Position = memberPositionUpdate.Position;
                        await ClanManager.Save(document);
                        return 0;
                    }
                    Log.Error("executorMemberView.HasHigherPermissionThan returned false");
                }
                else
                {
                    Log.Error("executorMemberView or targetMemberView is null");
                }
            }
            else
            {
                Log.Error("ClanManager.Get returned false");
            }
            return -1;
        }

        public override async Task<int> OnTransferOwnership(int groupId, string authToken, int newLeaderCmid)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == groupId)
                {
                    ClanDocument document = await ClanManager.Get(groupId);
                    if (document != null)
                    {
                        ClanMemberView executorMemberView = document.Clan.GetMember(session.Document.UserId);
                        ClanMemberView newLeaderMemberView = document.Clan.GetMember(newLeaderCmid);
                        if (executorMemberView != null && newLeaderMemberView != null)
                        {
                            newLeaderMemberView.Position = GroupPosition.Leader;
                            executorMemberView.Position = GroupPosition.Member;
                            document.Clan.OwnerName = newLeaderMemberView.Name;
                            document.Clan.OwnerCmid = newLeaderMemberView.Cmid;
                            _ = ClanManager.Save(document);
                            return 0;
                        }
                        Log.Error("executorMemberView or newLeaderMemberView is null");
                    }
                    else
                    {
                        Log.Error("ClanManager.Get returned false");
                    }
                }
                else
                {
                    Log.Error("Mismatch between groupId and session.Document.ClanId.");
                }
            }
            else
            {
                Log.Error("An unidentified AuthToken was passed.");
            }
            return 69;
        }

        public override async Task<int> OnKick(int groupId, string authToken, int cmidToKick)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == groupId)
                {
                    ClanDocument document = await ClanManager.Get(groupId);
                    if (document != null)
                    {
                        ClanMemberView executorMemberView = document.Clan.GetMember(session.Document.UserId);
                        ClanMemberView userMemberView = document.Clan.GetMember(cmidToKick);
                        if (executorMemberView != null && userMemberView != null)
                        {
                            if (executorMemberView.HasHigherPermissionThan(userMemberView))
                            {
                                if (document.Clan.RemoveMemberFromClan(userMemberView))
                                {
                                    UserDocument userDocument = GameSessionManager.TryGet(userMemberView.Cmid, out GameSession userSession) ? userSession.Document : UserManager.GetUser(userMemberView.Cmid).Result;
                                    userDocument.ClanId = 0;
                                    userDocument.Profile.GroupTag = string.Empty;
                                    _ = UserManager.Save(userDocument);
                                    _ = ClanManager.Save(document);
                                    return 0;
                                }
                            }
                            else
                            {
                                Log.Error("executorMemberView.HasHigherPermissionThan returned false");
                            }
                        }
                        else
                        {
                            Log.Error("executorMemberView or userMemberView is null");
                        }
                    }
                    else
                    {
                        Log.Error("ClanManager.Get returned false");
                    }
                }
                else
                {
                    Log.Error("Mismatch between groupId and session.Document.ClanId.");
                }
            }
            else
            {
                Log.Error("An unidentified AuthToken was passed.");
            }
            return 69;
        }

        public override async Task<int> OnLeaveClan(int groupId, string authToken)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == groupId)
                {
                    ClanDocument document = await ClanManager.Get(groupId);
                    if (document != null)
                    {
                        ClanMemberView userMemberView = document.Clan.GetMember(session.Document.UserId);
                        if (document.Clan.RemoveMemberFromClan(userMemberView))
                        {
                            if (GameSessionManager.TryGet(userMemberView.Cmid, out GameSession userSession))
                            {
                                userSession.Document.ClanId = 0;
                                userSession.Document.Profile.GroupTag = string.Empty;
                                await UserManager.Save(userSession.Document);
                                await ClanManager.Save(document);
                                return 0;
                            }
                            Log.Error("Received leave clan while user doesn't have any active session");
                            return -1;
                        }
                        Log.Error("document.Clan.RemoveMemberFromClan returned false");
                    }
                    else
                    {
                        Log.Error("ClanManager.Get returned false");
                    }
                }
                else
                {
                    Log.Error("Mismatch between groupId and session.Document.ClanId.");
                }
            }
            else
            {
                Log.Error("An unidentified AuthToken was passed.");
            }
            return 69;
        }

        public override async Task<int> OnDisbandClan(int groupId, string authToken)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == groupId)
                {
                    ClanDocument document = await ClanManager.Get(groupId);
                    if (document != null)
                    {
                        for (int i = 0; i < document.Clan.Members.Count; i++)
                        {
                            ClanMemberView userMemberView = document.Clan.Members[i];
                            UserDocument userDocument = GameSessionManager.TryGet(userMemberView.Cmid, out GameSession userSession) ? userSession.Document : UserManager.GetUser(userMemberView.Cmid).Result;
                            userDocument.ClanId = 0;
                            userDocument.Profile.GroupTag = string.Empty;
                            _ = UserManager.Save(userDocument);
                        }
                        await ClanManager.Remove(document);
                        return 0;
                    }
                    Log.Error("ClanManager.Get returned false");
                }
                else
                {
                    Log.Error("Mismatch between groupId and session.Document.ClanId.");
                }
            }
            else
            {
                Log.Error("An unidentified AuthToken was passed.");
            }
            return 69;
        }

        public override async Task<int> OnInviteMemberToJoinAGroup(int clanId, string authToken, int invitee, string message)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                ClanDocument clanDocument = await ClanManager.Get(clanId);
                if (clanDocument != null)
                {
                    UserDocument userDocument = session.Document;
                    if (!userDocument.ClanRequests.Contains(clanId))
                    {
                        int streamId = await StreamManager.GetNextId();
                        UserDocument inviteeDocument = GameSessionManager.TryGet(invitee, out GameSession userSession) ? userSession.Document : UserManager.GetUser(invitee).Result;
                        GroupInvitationStream groupInvitation = new GroupInvitationStream
                        {
                            StreamId = streamId,
                            GroupInvitation = new GroupInvitationView
                            {
                                GroupInvitationId = streamId,
                                GroupId = clanId,
                                GroupName = clanDocument.Clan.Name,
                                GroupTag = clanDocument.Clan.Tag,
                                InviterCmid = userDocument.UserId,
                                InviterName = userDocument.Profile.Name,
                                InviteeCmid = inviteeDocument.UserId,
                                InviteeName = inviteeDocument.Profile.Name,
                                Message = message
                            }
                        };
                        await StreamManager.Create(groupInvitation);
                        userDocument.Streams.Add(streamId);
                        inviteeDocument.Streams.Add(streamId);
                        await UserManager.Save(userDocument);
                        await UserManager.Save(inviteeDocument);
                        return 0;
                    }
                }
            }
            return -1;
        }

        public override async Task<ClanRequestAcceptView> OnAcceptClanInvitation(int clanInvitationId, string authToken)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (session.Document.ClanId == 0)
                {
                    StreamDocument document = await StreamManager.Get(clanInvitationId);
                    if (document != null && document.StreamType == StreamType.GroupInvitation)
                    {
                        GroupInvitationView groupInvitation = ((GroupInvitationStream)document).GroupInvitation;
                        ClanDocument clanDocument = await ClanManager.Get(groupInvitation.GroupId);
                        if (clanDocument != null)
                        {
                            if (clanDocument.Clan.AddMemberToClan(ClanMemberHelper.GetClanMemberView(session.Document)))
                            {
                                UserDocument user = !GameSessionManager.TryGet(groupInvitation.InviterCmid, out GameSession friendSession) ? UserManager.GetUser(groupInvitation.InviterCmid).Result : friendSession.Document;
                                session.Document.ClanId = clanDocument.ClanId;
                                session.Document.Profile.GroupTag = clanDocument.Clan.Tag;
                                _ = session.Document.Streams.Remove(groupInvitation.GroupInvitationId);
                                _ = user.Streams.Remove(groupInvitation.GroupInvitationId);
                                _ = await Task.WhenAny(UserManager.Save(user), UserManager.Save(session.Document), ClanManager.Save(clanDocument));
                                return new ClanRequestAcceptView
                                {
                                    ActionResult = 0,
                                    ClanView = clanDocument.Clan
                                };
                            }
                            Log.Error("Unable to accept clan invite. Unable to add user to clan");
                        }
                        else
                        {
                            Log.Error("Unable to accept clan invite. Unable to get clan stream");
                        }
                    }
                    else
                    {
                        Log.Error("Unable to accept clan invite. Unable to verify stream");
                    }
                }
                else
                {
                    Log.Error("Unable to accept clan invite. Clan id is not zero");
                }
            }
            else
            {
                Log.Error("Unable to accept clan invite. Game session doesn't exist");
            }
            return new ClanRequestAcceptView
            {
                ActionResult = 69
            };
        }

        public override async Task<ClanRequestDeclineView> OnDeclineClanInvitation(int clanInvitationId, string authToken)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                StreamDocument document = await StreamManager.Get(clanInvitationId);
                if (document != null && document.StreamType == StreamType.GroupInvitation)
                {
                    GroupInvitationView groupInvitation = ((GroupInvitationStream)document).GroupInvitation;
                    UserDocument inviterDocument = GameSessionManager.TryGet(groupInvitation.InviterCmid, out GameSession userSession) ? userSession.Document : UserManager.GetUser(groupInvitation.InviterCmid).Result;
                    _ = inviterDocument.ClanRequests.RemoveAll((T) => T == groupInvitation.GroupId);
                    _ = inviterDocument.Streams.RemoveAll((T) => T == groupInvitation.GroupInvitationId);
                    _ = session.Document.Streams.RemoveAll((T) => T == groupInvitation.GroupInvitationId);
                    _ = UserManager.Save(inviterDocument);
                    _ = UserManager.Save(session.Document);
                }
            }
            return new ClanRequestDeclineView();
        }

        public override async Task<int> OnCancelInvite(int clanInvitationId, string authToken)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                StreamDocument document = await StreamManager.Get(clanInvitationId);
                if (document != null && document.StreamType == StreamType.GroupInvitation)
                {
                    GroupInvitationView groupInvitation = ((GroupInvitationStream)document).GroupInvitation;
                    UserDocument inviteeDocument = GameSessionManager.TryGet(groupInvitation.InviteeCmid, out GameSession userSession) ? userSession.Document : UserManager.GetUser(groupInvitation.InviteeCmid).Result;
                    _ = inviteeDocument.ClanRequests.RemoveAll((T) => T == groupInvitation.GroupId);
                    _ = inviteeDocument.Streams.RemoveAll((T) => T == groupInvitation.GroupInvitationId);
                    _ = session.Document.Streams.RemoveAll((T) => T == groupInvitation.GroupInvitationId);
                    await UserManager.Save(inviteeDocument);
                    await UserManager.Save(session.Document);
                }
            }
            return 0;
        }

        public override int OnGetMyClanId(string authToken)
        {
            return !GameSessionManager.TryGet(authToken, out GameSession session) ? 0 : session.Document.ClanId;
        }

        private bool ValidString(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && ClanRegex.IsMatch(name);
        }
    }
}
