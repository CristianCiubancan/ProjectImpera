// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Screen.cs
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

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Comet.Core.Mathematics;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.World.Maps
{
    public sealed class Screen
    {
        public const int VIEW_SIZE = 18;
        public const int BROADCAST_SIZE = 21;

        private readonly Character m_user;

        public ConcurrentDictionary<uint, Role> Roles = new ConcurrentDictionary<uint, Role>();

        public Screen(Character user)
        {
            m_user = user;
        }

        public bool Add(Role role)
        {
            if (role is Monster mob)
                mob.PlayerOnSight++;
            return Roles.TryAdd(role.Identity, role);
        }

        public async Task RemoveAsync(uint idRole, bool force = false)
        {
            Roles.TryRemove(idRole, out var role);

            if (role is  Monster mob)
                mob.PlayerOnSight--;

            if (force)
            {
                MsgAction msg = new MsgAction
                {
                    Identity = idRole,
                    Action = MsgAction.ActionType.RemoveEntity
                };
                await m_user.SendAsync(msg);
            }
        }

        public async Task SynchroScreenAsync()
        {
            foreach (var role in Roles.Values)
            {
                await role.SendSpawnToAsync(m_user);

                if (role is Character user)
                    await m_user.SendSpawnToAsync(user);
            }
        }

        public async Task UpdateAsync(IPacket msg = null)
        {
            var targets = m_user.Map.Query9BlocksByPos(m_user.MapX, m_user.MapY);
            targets.AddRange(Roles.Values);
            foreach (Role target in targets.Select(x => x).Distinct())
            {
                if (target.Identity == m_user.Identity) continue;

                Character targetUser = target as Character;
                if (ScreenCalculations.GetDistance(m_user.MapX, m_user.MapY, target.MapX, target.MapY) < VIEW_SIZE)
                {
                    /*
                     * I add the target to my screen and it doesn't matter if he already sees me, I'll try to add myself into his screen.
                     * If succcess, I exchange the spawns.
                     */
                    if (Add(target))
                    {
                        targetUser?.Screen.Add(m_user);

                        await target.SendSpawnToAsync(m_user);
                        if (targetUser != null)
                            await m_user.SendSpawnToAsync(targetUser);
                    }
                }
                else
                {
                    await RemoveAsync(target.Identity);
                    if (targetUser != null)
                        await targetUser.Screen.RemoveAsync(m_user.Identity);
                }

                if (msg != null && targetUser != null)
                    await targetUser.SendAsync(msg);
            }
        }

        public async Task BroadcastRoomMsgAsync(IPacket msg, bool self = true)
        {
            if (self)
                await m_user.SendAsync(msg);

            foreach (var target in Roles.Values.Where(x => x is Character).Cast<Character>())
                await target.SendAsync(msg);
        }

        /// <summary>
        ///     For roles (not users) entering the screen.
        /// </summary>
        public async Task<bool> SpawnAsync(Role role)
        {
            if (Roles.TryAdd(role.Identity, role))
            {
                if (role is Monster mob)
                    mob.PlayerOnSight++;

                await role.SendSpawnToAsync(m_user);
                return true;
            }

            return false;
        }

        public async Task ClearAsync(bool sync = false)
        {
            if (sync)
            {
                foreach (var role in Roles.Values)
                {
                    if (role is Monster mob)
                        mob.PlayerOnSight--;

                    MsgAction msg = new MsgAction
                    {
                        Identity = role.Identity,
                        Action = MsgAction.ActionType.RemoveEntity
                    };
                    await m_user.SendAsync(msg);
                }
            }

            Roles.Clear();
        }
    }
}