// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTeam.cs
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
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTeam : MsgBase<Client>
    {
        public MsgTeam()
        {
            Type = PacketType.MsgTeam;
        }

        public TeamAction Action { get; set; }
        public uint Identity { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (TeamAction)reader.ReadUInt32();
            Identity = reader.ReadUInt32();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write((uint)Action);
            writer.Write(Identity);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = Kernel.RoleManager.GetUser(Identity);
            if (target == null && Identity != 0)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Team no target");
                return;
            }

#if !DEBUG
            if (user.IsGm() && target != null && !target.IsPm())
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"GM Character trying to team with no GM");
                return;
            }
#endif

            if (user.Map.IsTeamDisable())
                return;

            if (user.Team != null && !user.Team.IsLeader(user.Identity))
                await user.DetachStatusAsync(StatusSet.TEAM_LEADER);

            switch (Action)
            {
                case TeamAction.Create:
                    if (user.Team != null)
                        return;
                    
                    Team team = new Team(user);
                    if (!team.Create())
                        return;

                    await user.SendAsync(this);
                    await user.AttachStatusAsync(user, StatusSet.TEAM_LEADER, 0, int.MaxValue, 0, 0);
                    break;

                case TeamAction.Dismiss:
                    if (user.Team == null)
                        return;

                    if (await user.Team.DismissAsync(user))
                    {
                        await user.SendAsync(this);
                        await user.DetachStatusAsync(StatusSet.TEAM_LEADER);
                    }
                    break;

                case TeamAction.RequestJoin:
                    if (user.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamAlreadyNoJoin);
                        return;
                    }

                    if (target.Identity == user.Identity || user.GetDistance(target) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTeamLeaderNotInRange);
                        return;
                    }

                    if (target.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeam);
                        return;
                    }

                    if (!target.Team.JoinEnable)
                    {
                        await user.SendAsync(Language.StrTeamClosed);
                        return;
                    }

                    if (target.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (!target.IsAlive)
                    {
                        await user.SendAsync(Language.StrTeamLeaderDead);
                        return;
                    }

                    if (!target.Team.IsLeader(target.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoLeader);
                        return;
                    }

                    user.SetRequest(RequestType.TeamApply, target.Identity);
                    Identity = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrTeamApplySent);
                    break;

                case TeamAction.AcceptJoin:
                    if (target == null)
                        return;

                    if (user.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeamToInvite);
                        return;
                    }

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    if (user.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (user.GetDistance(target) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTeamTargetNotInRange);
                        return;
                    }

                    if (target.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamTargetAlreadyTeam);
                        return;
                    }

                    uint application = target.QueryRequest(RequestType.TeamApply);
                    if (application == user.Identity)
                    {
                        target.PopRequest(RequestType.TeamApply);
                        await user.SendAsync(this);
                        await user.Team.EnterTeamAsync(target);
                    }
                    else
                    {
                        await user.SendAsync(Language.StrTeamTargetHasNotApplied);
                    }
                    break;

                case TeamAction.RequestInvite:
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTeamInvitedNotFound);
                        return;
                    }

                    if (user.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeam);
                        return;
                    }

                    if (!user.Team.JoinEnable)
                    {
                        await user.SendAsync(Language.StrTeamClosed);
                        return;
                    }

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    if (user.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (target.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamTargetAlreadyTeam);
                        return;
                    }

                    if (!target.IsAlive)
                    {
                        await user.SendAsync(Language.StrTargetIsNotAlive);
                        return;
                    }

                    user.SetRequest(RequestType.TeamInvite, target.Identity);

                    Identity = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrInviteSent);
                    break;

                case TeamAction.AcceptInvite:
                    if (user.Team != null)
                    {
                        // ?? send message
                        return;
                    }

                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTeamTargetNotInRange);
                        return;
                    }

                    if (target.Team == null)
                    {
                        await user.SendAsync(Language.StrTargetHasNoTeam);
                        return;
                    }

                    if (target.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (!target.Team.IsLeader(target.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoLeader);
                        return;
                    }

                    uint inviteApplication = target.QueryRequest(RequestType.TeamInvite);
                    if (inviteApplication == user.Identity)
                    {
                        target.PopRequest(RequestType.TeamInvite);
                        await target.SendAsync(this);
                        await target.Team.EnterTeamAsync(user);
                    }
                    else
                    {
                        await user.SendAsync(Language.StrTeamNotInvited);
                    }

                    break;

                case TeamAction.LeaveTeam:
                    if (user.Team == null)
                        return;

                    if (user.Team.IsLeader(user.Identity))
                    {
                        await user.Team.DismissAsync(user);
                        return;
                    }

                    await user.Team.DismissMemberAsync(user);
                    await user.SendAsync(this);
                    break;

                case TeamAction.Kick:
                    if (user.Team == null || user.Team.IsLeader(user.Identity))
                        return;
                    if (target?.Team == null || target.Team.IsLeader(target.Identity))
                        return;

                    await user.Team.KickMemberAsync(user, Identity);
                    break;

                case TeamAction.Forbid:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.JoinEnable = false;
                    break;

                case TeamAction.RemoveForbid:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.JoinEnable = true;
                    break;

                case TeamAction.CloseMoney:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.MoneyEnable = false;
                    break;

                case TeamAction.OpenMoney:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.MoneyEnable = true;
                    break;

                case TeamAction.CloseItem:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.ItemEnable = false;
                    break;

                case TeamAction.OpenItem:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.ItemEnable = true;
                    break;
            }
        }

        public enum TeamAction
        {
            Create = 0x00,
            RequestJoin = 0x01,
            LeaveTeam = 0x02,
            AcceptInvite = 0x03,
            RequestInvite = 0x04,
            AcceptJoin = 0x05,
            Dismiss = 0x06,
            Kick = 0x07,
            Forbid = 0x08,
            RemoveForbid = 0x09,
            CloseMoney = 0x0A,
            OpenMoney = 0x0B,
            CloseItem = 0x0C,
            OpenItem = 0x0D
        }
    }
}