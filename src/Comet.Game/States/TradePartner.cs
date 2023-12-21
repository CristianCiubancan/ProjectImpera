// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Trade Partner.cs
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

using System;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;

#endregion

namespace Comet.Game.States
{
    public sealed class TradePartner
    {
        private DbBusiness m_dbBusiness;

        public TradePartner(Character owner, DbBusiness business = null)
        {
            Owner = owner;
            if (business != null)
                m_dbBusiness = business;
        }

        public Character Owner { get; }

        public Character Target =>
            Kernel.RoleManager.GetUser(m_dbBusiness.UserId == Owner.Identity
                ? m_dbBusiness.BusinessId
                : m_dbBusiness.UserId);

        public uint Identity => m_dbBusiness.UserId == Owner.Identity ? m_dbBusiness.BusinessId : m_dbBusiness.UserId;
        public string Name =>  m_dbBusiness.UserId == Owner.Identity ? m_dbBusiness.Business?.Name : m_dbBusiness.User?.Name;

        public bool IsValid()
        {
            return m_dbBusiness.Date < DateTime.Now;
        }

        public Task SendAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Name = Name,
                Action = MsgTradeBuddy.TradeBuddyAction.AddPartner,
                IsOnline = Target != null,
                HoursLeft = (int) (!IsValid() ? (m_dbBusiness.Date - DateTime.Now).TotalMinutes : 0),
                Identity = Identity
            });
        }

        public Task SendInfoAsync()
        {
            Character target = Target;
            if (target == null)
                return Task.CompletedTask;

            return Owner.SendAsync(new MsgTradeBuddyInfo
            {
                Identity = Identity,
                Name = target.MateName,
                Level = target.Level,
                Lookface = target.Mesh,
                PkPoints = target.PkPoints,
                Profession = target.Profession,
                Syndicate = target.SyndicateIdentity,
                SyndicatePosition = target.SyndicateRank
            });
        }

        public Task SendRemoveAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Action = MsgTradeBuddy.TradeBuddyAction.BreakPartnership,
                Identity = Identity,
                IsOnline = true,
                Name = ""
            });
        }

        public Task<bool> DeleteAsync()
        {
            return BaseRepository.DeleteAsync(m_dbBusiness);
        }
    }
}