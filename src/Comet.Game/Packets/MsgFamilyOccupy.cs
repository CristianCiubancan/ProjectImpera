// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgFamilyOccupy.cs
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
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Events;
using Comet.Game.States.Families;
using Comet.Game.States.NPCs;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgFamilyOccupy : MsgBase<Client>
    {
        public enum FamilyPromptType
        {
            Challenge = 0, // Client -> Server 
            CancelChallenge = 1,
            AbandonMap = 2,
            RemoveChallenge = 3,
            ChallengeMap = 4,
            Unknown5 = 5, // Probably Client -> Server
            RequestNpc = 6, // Npc Click Client -> Server -> Client
            AnnounceWarBegin = 7, // Call to war Server -> Client
            AnnounceWarAccept = 8, // Answer Ok to annouce Client -> Server
            ClaimExperience = 10,
            WrongClaimTime = 13, // Claim once a day
            CannotClaim = 12, // New members cannot claim
            ClaimOnceADay = 14, // Claimed
            ClaimedAlready = 15, // Claimed, claim tomorrow
            WrongExpClaimTime = 16, // Claimed
            ReachedMaxLevel = 17,
            ClaimRevenue = 18
        }

        public FamilyPromptType Action; // 4
        public uint Identity; // 8
        public uint RequestNpc; // 12
        public uint SubAction; // 16
        public string OccupyName; // 20
        public string CityName; // 56
        public bool WarRunning; // 92
        public bool CanRemoveChallenge; // 94
        public bool CanApplyChallenge; // 93
        public bool UnknownBool3; // 95
        public uint OccupyDays; // 96
        public uint DailyPrize; // 100
        public uint WeeklyPrize; // 104
        public uint IsChallenged; // 112
        public uint GoldFee; // 120
        public bool CanClaimRevenue;
        public bool CanClaimExperience;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (FamilyPromptType) reader.ReadInt32();
            Identity = reader.ReadUInt32();
            RequestNpc = reader.ReadUInt32();
            SubAction = reader.ReadUInt32();
            OccupyName = reader.ReadString(16);
            reader.BaseStream.Seek(20, SeekOrigin.Current);
            CityName = reader.ReadString(16);
            reader.BaseStream.Seek(24, SeekOrigin.Current);
            OccupyDays = reader.ReadUInt32();
            DailyPrize = reader.ReadUInt32();
            WeeklyPrize = reader.ReadUInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
            GoldFee = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) (Type = PacketType.MsgFamilyOccupy));
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(RequestNpc);
            writer.Write(SubAction);
            writer.Write(OccupyName, 16);
            writer.BaseStream.Seek(20, SeekOrigin.Current);
            writer.Write(CityName, 16);
            writer.BaseStream.Seek(20, SeekOrigin.Current);
            writer.Write(WarRunning);
            writer.Write(CanApplyChallenge);
            writer.Write(CanRemoveChallenge);
            writer.Write(UnknownBool3);
            writer.Write(OccupyDays);
            writer.Write(DailyPrize);
            writer.Write(WeeklyPrize);
            writer.Write(0);
            writer.Write(IsChallenged); // Challenged by other clans
            writer.Write(0);
            writer.Write(GoldFee);
            writer.Write(0);
            writer.Write(CanClaimRevenue); // allow claim
            writer.Write(CanClaimExperience); // claim exp
            writer.Write((ushort) 0);
            writer.Write(0);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            FamilyWar war = Kernel.EventThread.GetEvent<FamilyWar>();
            if (war == null)
                return;

            switch (Action)
            {
                case FamilyPromptType.Challenge:
                {
                    if (user.Family == null)
                        return;

                    if (user.Family.ChallengeMap == Identity)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    DynamicNpc npc = Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Identity == Identity);
                    if (npc == null)
                        return;

                    uint fee = war.GetGoldFee(Identity);
                    if (fee == 0)
                        return;

                    if (user.Family.Money < fee)
                    {
                        await user.SendAsync(Language.StrNotEnoughFamilyMoneyToChallenge);
                        return;
                    }

                    user.Family.Money -= fee;
                    user.Family.ChallengeMap = (uint) npc.Data1;
                    await user.Family.SaveAsync();
                    await user.SendFamilyAsync();

                    GameMap map = Kernel.MapManager.GetMap(user.Family.ChallengeMap);
                    if (map == null) //??
                        return;

                    await user.Family.SendAsync(string.Format(Language.StrPrepareToChallengeFamily, map.Name));

                    Family owner = war.GetFamilyOwner(npc.Identity);
                    if (owner != null)
                        await owner.SendAsync(string.Format(Language.StrPrepareToDefendFamily, map.Name));

                    break;
                }

                case FamilyPromptType.CancelChallenge:
                {
                    if (user.Family == null)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    user.Family.ChallengeMap = 0;
                    await user.Family.SaveAsync();
                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyPromptType.RequestNpc:
                {
                    DailyPrize = war.GetNextReward(user, RequestNpc);
                    WeeklyPrize = war.GetNextWeekReward(user, RequestNpc);

                    DynamicNpc npc = user.Map.QueryRole<DynamicNpc>(RequestNpc);
                    Family owner = war.GetFamilyOwner(RequestNpc);
                    Identity = RequestNpc;
                    if (owner != null)
                    {
                        OccupyDays = owner.OccupyDays;
                        OccupyName = owner.Name;
                    }

                    if (owner?.Identity == user.FamilyIdentity)
                    {
                        WarRunning = war.IsInTime;
                        SubAction = user.Identity == owner.LeaderIdentity ? 1u : 2u;

                        CanClaimRevenue = owner.LeaderIdentity == user.Identity && war.HasRewardToClaim(user);
                        CanClaimExperience = war.HasExpToClaim(user);

                        IsChallenged = war.GetChallengers((uint) npc.Data1).Count > 0 ? 1u : 0u;
                    }
                    else
                    {
                        WarRunning = war.IsInTime && user.Family != null && user.Family.ChallengeMap == npc?.Data1;
                        CanRemoveChallenge = npc?.Data1 == user.Family?.ChallengeMap && !WarRunning;
                        if (CanRemoveChallenge)
                        {
                            SubAction = 3;
                        }
                        else
                        {
                            CanApplyChallenge = user.Family != null && RequestNpc != user.Family.ChallengeMap && !WarRunning;
                            if (CanApplyChallenge)
                                SubAction = 5;
                        }
                    }

                    GoldFee = war.GetGoldFee(RequestNpc);
                    await user.SendAsync(this);
                    break;
                }

                case FamilyPromptType.AnnounceWarAccept:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    if (!war.IsAllowedToJoin(user))
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                    {
                        npc = war.GetChallengeNpc(user.Family);
                        if (npc == null)
                            return;
                    }

                    GameMap map = Kernel.MapManager.GetMap((uint) npc.Data1);
                    if (map == null) 
                        return;

                    if ((DateTime.Now - user.FamilyMember.JoinDate).TotalHours < 24)
                        return;

                    await user.FlyMapAsync(map.Identity, 50, 50);
                    break;
                }

                case FamilyPromptType.ClaimExperience:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                        return;

                    GameMap map = Kernel.MapManager.GetMap((uint)npc.Data1);
                    if (map == null) return;

                    if ((DateTime.Now - user.FamilyMember.JoinDate).TotalDays < 1)
                    {
                        Action = FamilyPromptType.CannotClaim;
                        await user.SendAsync(this);
                        return;
                    }

                    if (!war.HasExpToClaim(user))
                    {
                        Action = FamilyPromptType.WrongExpClaimTime;
                        await user.SendAsync(this);
                        return;
                    }

                    if (user.Level >= Role.MAX_UPLEV)
                    {
                        Action = FamilyPromptType.ReachedMaxLevel;
                        await user.SendAsync(this);
                        return;
                    }

                    double exp = war.GetNextExpReward(user);
                    
                    if (exp == 0)
                        return;

                    DbLevelExperience currLevExp = Kernel.RoleManager.GetLevelExperience(user.Level);
                    if (currLevExp == null)
                        return;

                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrFetchFamilyNpcExpSuccess, user.Name, map.Name, user.Level, exp * 100), MsgTalk.TalkChannel.Center);

                    long awardExp = (long) (currLevExp.Exp * exp);
                    await user.AwardExperienceAsync(awardExp);
                    await war.SetExpRewardAwardedAsync(user);
                    break;
                }

                case FamilyPromptType.ClaimRevenue:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                        return;

                    GameMap map = Kernel.MapManager.GetMap((uint)npc.Data1);
                    if (map == null) return;

                    if (!war.HasRewardToClaim(user))
                    {
                        Action = DateTime.Now.Hour >= 21 ? FamilyPromptType.ClaimedAlready : FamilyPromptType.ClaimOnceADay;
                        await user.SendAsync(this);
                        return;
                    }

                    if (!user.UserPackage.IsPackSpare(5))
                    {
                        await user.SendAsync(string.Format(Language.StrNotEnoughSpaceN, 5), MsgTalk.TalkChannel.TopLeft, Color.Red);
                        return;
                    }

                    uint idItem = war.GetNextReward(user, RequestNpc);
                    if (idItem == 0)
                        return;

                    await war.SetRewardAwardedAsync(user);

                    await user.UserPackage.AwardItemAsync(idItem);
                    await user.Family.SendAsync(string.Format(Language.StrFetchFamilyNpcIncomeSuccess, user.Name, map.Name));
                    break;
                }

                default:
                {
                    if (client.Character.IsPm())
                    {
                        await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                            $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    }

                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}", Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}