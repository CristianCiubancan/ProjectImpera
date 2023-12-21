// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Enemy.cs
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

using System;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Shared;

namespace Comet.Game.States.Relationship
{
    public sealed class Enemy
    {
        private DbEnemy m_DbEnemy;
        private Character m_owner;

        public Enemy(Character owner)
        {
            m_owner = owner;
        }

        public uint Identity => m_DbEnemy.TargetIdentity;
        public string Name => m_DbEnemy.TargetName;
        public bool Online => User != null;
        public Character User => Kernel.RoleManager.GetUser(Identity);

        public async Task<bool> CreateAsync(Character user)
        {
            m_DbEnemy = new DbEnemy
            {
                UserIdentity = m_owner.Identity,
                TargetIdentity = user.Identity,
                TargetName = user.Name,
                Time = DateTime.Now
            };
            await SendAsync();
            return await SaveAsync();
        }

        public Task CreateAsync(DbEnemy enemy)
        {
            m_DbEnemy = enemy;
            return Task.CompletedTask;
        }

        public async Task SendAsync()
        {
            await m_owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend.MsgFriendAction.AddEnemy,
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
                IsEnemy = true
            });
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using ServerDbContext ctx = new ServerDbContext();
                if (m_DbEnemy.Identity == 0)
                    await ctx.Enemies.AddAsync(m_DbEnemy);
                else
                    ctx.Enemies.Update(m_DbEnemy);
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
                if (m_DbEnemy.Identity == 0)
                    return false;
                ctx.Enemies.Remove(m_DbEnemy);
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