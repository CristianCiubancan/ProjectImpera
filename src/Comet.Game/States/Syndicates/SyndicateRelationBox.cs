// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - SyndicateRelationBox.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using System.Threading.Tasks;

#endregion

namespace Comet.Game.States.Syndicates
{
    public sealed class SyndicateRelationBox : MessageBox
    {
        private Character m_senderUser;
        private Character m_targetUser;

        private Syndicate m_sender;
        private Syndicate m_target;

        public SyndicateRelationBox(Character owner)
            : base(owner)
        {
        }

        public async Task<bool> CreateAsync(Character sender, Character target, RelationType type)
        {
            if (sender?.Syndicate == null || target?.Syndicate == null)
                return false;

            if (sender.SyndicateIdentity == target.SyndicateIdentity)
                return false;

            if (sender.Syndicate.Deleted || target.Syndicate.Deleted)
                return false;

            if (!sender.Syndicate.Leader.IsOnline || !target.Syndicate.Leader.IsOnline)
                return false;

            m_sender = sender.Syndicate;
            m_target = target.Syndicate;

            m_senderUser = sender;
            m_targetUser = target;
            
            Message = string.Format(Language.StrSyndicateAllianceRequest, sender.Name, sender.SyndicateName);
            await SendAsync();
            return true;
        }

        public override async Task OnAcceptAsync()
        {
            await m_sender.CreateAllianceAsync(m_senderUser, m_target);
        }

        public override async Task OnCancelAsync()
        {
            await m_sender.SendAsync(string.Format(Language.StrSyndicateAllianceDeny, m_target.Name));
        }

        public enum RelationType
        {
            Ally
        }
    }
}