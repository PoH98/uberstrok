﻿using System;
using System.Collections.Generic;

namespace UberStrok.Core.Views
{
    [Serializable]
    public class ClanView : BasicClanView
    {
        public ClanView()
        {
            Members = new List<ClanMemberView>();
        }

        public ClanView(int groupId, int membersCount, string description, string name, string motto, string address, DateTime foundingDate, string picture, GroupType type, DateTime lastUpdated, string tag, int membersLimit, GroupColor colorStyle, GroupFontStyle fontStyle, int applicationId, int ownerCmid, string ownerName, List<ClanMemberView> members) : base(groupId, membersCount, description, name, motto, address, foundingDate, picture, type, lastUpdated, tag, membersLimit, colorStyle, fontStyle, applicationId, ownerCmid, ownerName)
        {
            Members = members;
        }

        public List<ClanMemberView> Members { get; set; }

        public override string ToString()
        {
            string text = "[Clan: " + base.ToString();
            text += "[Members:";
            foreach (ClanMemberView clanMemberView in Members)
            {
                text += clanMemberView.ToString();
            }
            text += "]";
            return text;
        }
    }
}
