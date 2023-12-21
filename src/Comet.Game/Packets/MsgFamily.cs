// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgFamily.cs
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Families;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgFamily : MsgBase<Client>
    {
        public FamilyAction Action;
        public uint Identity;
        public uint Unknown;
        public List<string> Strings = new List<string>();
        public List<object> Objects = new List<object>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (FamilyAction) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFamily);
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(Unknown);
            if (Objects.Count == 0)
            {
                writer.Write(Strings);
            }
            else
            {
                int idx = 0;
                writer.Write(Objects.Count);
                foreach (var obj in Objects)
                {
                    if (obj is MemberListStruct member)
                    {
                        writer.Write(member.Name, 16);
                        writer.Write((int) member.Level);
                        writer.Write(member.Rank);
                        writer.Write((ushort) (member.Online ? 1 : 0));
                        writer.Write((int) member.Profession);
                        writer.Write(member.Donation);
                    }
                    else if (obj is RelationListStruct relation)
                    {
                        writer.Write(idx + 101);
                        writer.Write(relation.Name, 16);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(relation.LeaderName, 16);
                    }
                    idx++;
                }
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = Kernel.RoleManager.GetUser(client.Character?.Identity ?? 0);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            switch (Action)
            {
                case FamilyAction.Query:
                {
                    if (user.Family == null)
                        return;

                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyAction.QueryMemberList:
                {
                    if (user.Family == null)
                        return;

                    await user.Family.SendMembersAsync(0, user);
                    break;
                }

                case FamilyAction.Recruit:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

                    Character target = Kernel.RoleManager.GetUser(Identity);
                    if (target == null)
                        return;
                    if (target.Family != null)
                        return;

                    user.SetRequest(RequestType.Family, target.Identity);

                    Strings.Clear();

                    Identity = user.FamilyIdentity;
                    Strings.Add(user.FamilyName);
                    Strings.Add(user.Name);
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    break;
                }

                case FamilyAction.AcceptRecruit:
                {
                    if (user.Family != null)
                        return;

                    Family family = Kernel.FamilyManager.GetFamily(Identity);
                    if (family == null)
                        return;

                    if (family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

                    Character leader = family.Leader.User;
                    if (leader == null)
                        return;

                    if (leader.QueryRequest(RequestType.Family) != user.Identity)
                        return;

                    leader.PopRequest(RequestType.Family);
                    await family.AppendMemberAsync(leader, user);
                    break;
                }

                case FamilyAction.Join:
                {
                    if (user.Family != null)
                        return;

                    Character leader = Kernel.RoleManager.GetUser(Identity);
                    if (leader == null)
                        return;
                    if (leader.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (leader.Family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

                    user.SetRequest(RequestType.Family, leader.Identity);

                    Strings.Clear();

                    Identity = user.Identity;
                    Strings.Add(user.Name);
                    await leader.SendAsync(this);
                    await leader.SendRelationAsync(user);
                    break;
                }

                case FamilyAction.AcceptJoinRequest:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Character requester = Kernel.RoleManager.GetUser(Identity);
                    if (requester == null)
                        return;

                    if (requester.Family != null)
                        return;

                    if (requester.QueryRequest(RequestType.Family) != user.Identity)
                        return;

                    requester.PopRequest(RequestType.Family);
                    await user.Family.AppendMemberAsync(user, requester);
                    break;
                }

                case FamilyAction.AddEnemy:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.EnemyCount >= Family.MAX_RELATION)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = Kernel.FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;
                    user.Family.SetEnemy(target);
                    await user.Family.SaveAsync();
                    await user.Family.SendRelationsAsync();
                    break;
                }

                case FamilyAction.DeleteEnemy:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = Kernel.FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (!user.Family.IsEnemy(target.Identity))
                        return;
                    user.Family.UnsetEnemy(target.Identity);
                    await user.Family.SaveAsync();

                    Identity = target.Identity;
                    await user.Family.SendAsync(this);
                    break;
                }

                case FamilyAction.AddAlly:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.AllyCount >= Family.MAX_RELATION)
                        return;
                    if (Identity == 0)
                        return;
                    Character targetUser = Kernel.RoleManager.GetUser(Identity);
                    if (targetUser == null)
                        return;

                    if (targetUser.FamilyIdentity == 0 || targetUser.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Family target = targetUser.Family;
                    if (target == null || !target.Leader.IsOnline)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;

                    Strings.Clear();
                    Identity = user.Family.Identity;
                    Strings = new List<string>
                    {
                        user.FamilyName, 
                        user.Name
                    };

                    await target.Leader.User.SendAsync(this);
                    break;
                }

                case FamilyAction.AcceptAlliance:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.AllyCount >= Family.MAX_RELATION)
                        return;
                    if (Strings.Count == 0)
                        return;

                    Character targetUser = Kernel.RoleManager.GetUser(Strings[0]);
                    if (targetUser == null)
                        return;

                    if (targetUser.FamilyIdentity == 0 || targetUser.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Family target = targetUser.Family;
                    if (target == null)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;

                    user.Family.SetAlly(target);
                    await user.Family.SaveAsync();

                    target.SetAlly(user.Family);
                    await target.SaveAsync();

                    await user.Family.SendRelationsAsync();
                    await target.SendRelationsAsync();
                    break;
                }

                case FamilyAction.DeleteAlly:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = Kernel.FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (!user.Family.IsAlly(target.Identity))
                        return;
                    user.Family.UnsetAlly(target.Identity);
                    await user.Family.SaveAsync();

                    target.UnsetAlly(user.FamilyIdentity);
                    await target.SaveAsync();

                    Identity = target.Identity;
                    await user.Family.SendAsync(this);

                    Identity = user.FamilyIdentity;
                    Strings = new List<string> { user.FamilyName };
                    await target.SendAsync(this);
                    break;
                }

                case FamilyAction.Abdicate:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Character target = Kernel.RoleManager.GetUser(Strings[0]);
                    if (target == null)
                        return;
                    if (target.FamilyIdentity != user.FamilyIdentity)
                        return;
                    if (target.FamilyPosition != Family.FamilyRank.Member)
                        return;
                    await user.Family.AbdicateAsync(user, Strings[0]);
                    break;
                }

                case FamilyAction.KickOut:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader) 
                        return;
                    if (Strings.Count == 0)
                        return;
                    var target = user.Family.GetMember(Strings[0]);
                    if (target == null)
                        return;
                    if (target.FamilyIdentity != user.FamilyIdentity)
                        return;
                    if (target.Rank != Family.FamilyRank.Member)
                        return;
                    await user.Family.KickOutAsync(user, target.Identity);
                    break;
                }

                case FamilyAction.Quit:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition == Family.FamilyRank.ClanLeader)
                        return;
                    if (user.FamilyPosition == Family.FamilyRank.Spouse)
                        return;

                    await user.Family.LeaveAsync(user);
                    break;
                }

                case FamilyAction.Announce:
                {
                    if (user.Family == null)
                        return;

                    Identity = user.FamilyIdentity;
                    Strings.Add(user.Family.Announcement);
                    await user.SendAsync(this);
                    break;
                }

                case FamilyAction.SetAnnouncement:
                {
                    if (Strings.Count == 0)
                        return;
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    user.Family.Announcement = Strings[0].Substring(0, Math.Min(127, Strings[0].Length));
                    await user.Family.SaveAsync();

                    Action = FamilyAction.Announce;
                    await user.Family.SendAsync(this);
                    break;
                }

                case FamilyAction.Dedicate:
                {
                    if (user.Family == null)
                        return;

                    if (!await user.SpendMoneyAsync((int) Identity, true))
                        return;

                    user.Family.Money += Identity;
                    user.FamilyMember.Proffer += Identity;
                    await user.Family.SaveAsync();
                    await user.FamilyMember.SaveAsync();
                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyAction.QueryOccupy:
                {
                    if (user.Family == null)
                        return;

                    await user.SendFamilyAsync();
                    await user.SendFamilyOccupyAsync();
                    break;
                }

                default:
                {
                    if (user.IsPm())
                    {
                        await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                            $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    }

                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        Type, Action, Length, PacketDump.Hex(Encode()));
                        break;
                }
            }
        }

        public struct MemberListStruct
        {
            public string Name;
            public byte Level;
            public ushort Rank;
            public bool Online;
            public ushort Profession;
            public uint Donation;
        }

        public struct RelationListStruct
        {
            public string Name;
            public string LeaderName;
        }

        public enum FamilyAction
        {
            Query = 1,
            QueryMemberList = 4,
            Recruit = 9,
            AcceptRecruit = 10,
            Join = 11,
            AcceptJoinRequest = 12,
            SendEnemy = 13,
            AddEnemy = 14,
            DeleteEnemy = 15,
            SendAlly = 16,
            AddAlly = 17,
            AcceptAlliance = 18,
            DeleteAlly = 20,
            Abdicate = 21,
            KickOut = 22,
            Quit = 23,
            Announce = 24,
            SetAnnouncement = 25,
            Dedicate = 26,
            QueryOccupy = 29
        }
    }
}