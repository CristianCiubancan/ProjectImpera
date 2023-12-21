// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Friend.cs
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
using Comet.Game.States.Syndicates;
using Comet.Shared;

#endregion

namespace Comet.Game.States.Relationship
{
    public sealed class Friend
    {
        private DbFriend m_dbFriend;
        private Character m_owner;

        public Friend(Character owner)
        {
            m_owner = owner;
        }

        public uint Identity => m_dbFriend.TargetIdentity;
        public string Name => m_dbFriend.TargetName;
        public bool Online => User != null;
        public Character User => Kernel.RoleManager.GetUser(Identity);
        
        public async Task<bool> CreateAsync(Character user)
        {
            m_dbFriend = new DbFriend
            {
                UserIdentity = m_owner.Identity,
                TargetIdentity = user.Identity,
                TargetName = user.Name,
                Time = DateTime.Now
            };
            return true;
        }

        public Task CreateAsync(DbFriend friend)
        {
            m_dbFriend = friend;
            return Task.CompletedTask;
        }

        public async Task SendAsync()
        {
            await m_owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend.MsgFriendAction.AddFriend,
                Online = Online
            });
        }
        
        public async Task SendInfoAsync()
        {
            Character user = User;
            await m_owner.SendAsync(new MsgFriendInfo
            {
                Identity = Identity,
                PkPoints = user?.PkPoints ?? 0,
                Level = user?.Level ?? 0,
                Mate = user?.MateName ?? Language.StrNone,
                Profession = user?.Profession ?? 0,
                Lookface = user?.Mesh ?? 0,
                SyndicateIdentity = user?.SyndicateIdentity ?? 0,
                SyndicateRank = (ushort) (user?.SyndicateRank ?? SyndicateMember.SyndicateRank.None)
            });
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using ServerDbContext ctx = new ServerDbContext();
                if (m_dbFriend.Identity == 0)
                    await ctx.Friends.AddAsync(m_dbFriend);
                else
                    ctx.Friends.Update(m_dbFriend);
                await ctx.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteAsync()
        {
            try
            {
                await using ServerDbContext ctx = new ServerDbContext();
                if (m_dbFriend.Identity == 0)
                    return false;
                ctx.Friends.Remove(m_dbFriend);
                await ctx.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }
    }
}