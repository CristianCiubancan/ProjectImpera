// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgDataArray.cs
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

// #define OLD_COMPOSE

#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgDataArray : MsgBase<Client>
    {
        public enum DataArrayMode : byte
        {
            Composition = 0,
            CompositionSteedOriginal = 2,
            CompositionSteedNew = 3,
            QuickCompose = 4,
            QuickComposeMount = 5,
            UpgradeItemLevel = 6,
            UpgradeItemQuality = 7
        }

        public MsgDataArray()
        {
            Type = PacketType.MsgDataArray;
        }

        public DataArrayMode Action { get; set; }
        public byte Count { get; set; }

        public List<uint> Items = new List<uint>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (DataArrayMode) reader.ReadByte();
            Count = reader.ReadByte();
            reader.ReadUInt16();
            for (int i = 0; i < Count; i++)
            {
                Items.Add(reader.ReadUInt32());
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write((byte) Action);
            writer.Write((byte) Items.Count);
            writer.Write((ushort) 0);
            foreach (var item in Items)
            {
                writer.Write(item);
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
{
            Character user = client.Character;

            if (Items.Count < 2)
                return;

            Item target = user.UserPackage[Items[0]];

            if (target == null)
                return;

            int oldAddition = target.Plus;
            switch (Action)
            {
                case DataArrayMode.Composition:
                {
#if OLD_COMPOSE
                    if (Items.Count < 5 || (Items[1] == 0 && Items[2] == 0))
                        return;
                    
                    Item composeTarget = user.UserPackage[Items[0]];
                    Item composeMinor1 = user.UserPackage[Items[1]];
                    Item composeMinor2 = user.UserPackage[Items[2]];

                    if (composeTarget == null)
                        return;

                    byte oldComposition = composeTarget.Plus;

                    if (composeMinor1 == null || composeMinor2 == null) // need 2 minor items
                        return;

                    if (composeMinor1.GetItemSubType() != 730)
                    {
                        if (composeTarget.GetItemSort() == Item.ItemSort.ItemsortWeaponSingleHand
                            || composeTarget.GetItemSort() == Item.ItemSort.ItemsortWeaponDoubleHand)
                        {
                            if (composeTarget.IsBow() && !composeMinor1.IsBow())
                                return;

                            if (composeTarget.GetItemSort() != composeMinor1.GetItemSort())
                                return;
                        }
                        else
                        {
                            if (composeTarget.GetItemSubType() != composeMinor1.GetItemSubType())
                                return;
                        }
                    }

                    if (composeMinor2.GetItemSubType() != 730)
                    {
                        if (composeTarget.GetItemSort() == Item.ItemSort.ItemsortWeaponSingleHand
                            || composeTarget.GetItemSort() == Item.ItemSort.ItemsortWeaponDoubleHand)
                        {
                            if (composeTarget.IsBow() && !composeMinor2.IsBow())
                                return;

                            if (composeTarget.GetItemSort() != composeMinor2.GetItemSort())
                                return;
                        }
                        else
                        {
                            if (composeTarget.GetItemSubType() != composeMinor2.GetItemSubType())
                                return;
                        }
                    }

                    if (composeMinor1.Plus < composeTarget.Plus
                        || composeMinor2.Plus < composeTarget.Plus)
                        return;

                    Item composeGem1 = null;
                    Item composeGem2 = null;
                    bool useGems = false;

                    if (composeTarget.Plus >= 5)
                    {
                        if (Items[3] == 0 || Items[4] == 0)
                            return;

                        composeGem1 = user.UserPackage[Items[3]];
                        composeGem2 = user.UserPackage[Items[4]];

                        if (composeGem1 == null || composeGem2 == null) // need 2 gems if going to >+6
                            return;

                        if (composeGem1.GetItemSubType() != 700 || composeGem2.GetItemSubType() != 700) // must be gems
                            return;

                        useGems = true;
                    }

                    await user.UserPackage.SpendItemAsync(composeMinor1);
                    await user.UserPackage.SpendItemAsync(composeMinor2);
                    if (useGems)
                    {
                        await user.UserPackage.SpendItemAsync(composeGem1);
                        await user.UserPackage.SpendItemAsync(composeGem2);
                    }

                    composeTarget.ChangeAddition();

                    await user.SendAsync(new MsgItemInfo(composeTarget, MsgItemInfo.ItemMode.Update));
                    await composeTarget.SaveAsync();

                    if (oldComposition < composeTarget.Plus && composeTarget.Plus >= 6)
                    {
                        if (user.Gender == 1)
                            await user.SendAsync(string.Format(Language.StrComposeOverpowerMale, user.Name, composeTarget.Name, composeTarget.Plus));
                        else
                            await user.SendAsync(string.Format(Language.StrComposeOverpowerFemale, user.Name, composeTarget.Name, composeTarget.Plus));
                    }
#else
                    if (target.Plus >= 12)
                    {
                        await user.SendAsync(Language.StrComposeItemMaxComposition);
                        return;
                    }

                    for (int i = 1; i < Items.Count; i++)
                    {
                        Item source = user.UserPackage[Items[i]];
                        if (source == null)
                            continue;

                        if (source.Type < Item.TYPE_STONE1 || source.Type > Item.TYPE_STONE8)
                        {
                            if (source.IsWeaponOneHand())
                            {
                                if (!target.IsWeaponOneHand() && !target.IsWeaponProBased())
                                    continue;
                            }
                            else if (source.IsWeaponTwoHand())
                            {
                                if (source.IsBow() && !target.IsBow())
                                    continue;
                                if (!target.IsWeaponTwoHand())
                                    continue;
                            }

                            if (target.GetItemSort() != source.GetItemSort())
                                continue;

                            if (source.Plus == 0 || source.Plus > 8)
                                continue;
                        }

                        target.CompositionProgress += PlusAddLevelExp(source.Plus, false);
                        while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                        {
                            if (target.Plus < 12)
                            {
                                target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                target.ChangeAddition();
                            }
                            else
                            {
                                target.CompositionProgress = 0;
                                break;
                            }
                        }

                        await user.UserPackage.SpendItemAsync(source);
                    }
#endif
                    break;
                }

                case DataArrayMode.CompositionSteedOriginal:
                case DataArrayMode.CompositionSteedNew:
                {
                    if (!target.IsMount())
                        return;

                    for (int i = 1; i < Items.Count; i++)
                    {
                        Item source = user.UserPackage[Items[i]];
                        if (source == null)
                            continue;

                        target.CompositionProgress += PlusAddLevelExp(source.Plus, true);
                        while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                        {
                            if (target.Plus < 12)
                            {
                                target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                target.ChangeAddition();
                            }
                        }

                        if (Action == DataArrayMode.CompositionSteedNew)
                        {
                            int color1 = (int) target.SocketProgress;
                            int color2 = (int) source.SocketProgress;
                            int B1 = color1 & 0xFF;
                            int B2 = color2 & 0xFF;
                            int G1 = (color1 >> 8) & 0xFF;
                            int G2 = (color2 >> 8) & 0xFF;
                            int R1 = (color1 >> 16) & 0xFF;
                            int R2 = (color2 >> 16) & 0xFF;
                            int newB = (int) Math.Floor(0.9 * B1) + (int) Math.Floor(0.1 * B2);
                            int newG = (int) Math.Floor(0.9 * G1) + (int) Math.Floor(0.1 * G2);
                            int newR = (int) Math.Floor(0.9 * R1) + (int) Math.Floor(0.1 * R2);
                            target.ReduceDamage = (byte) newR;
                            target.Enchantment = (byte) newB;
                            target.AntiMonster = (byte) newG;
                            target.SocketProgress = (uint)(newG | (newB << 8) | (newR << 16));
                        }

                        await user.UserPackage.SpendItemAsync(source);
                    }

                    break;
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Error, $"Invalid MsgDataArray Action: {Action}." +
                                                       $"{user.Identity},{user.Name},{user.Level},{user.MapIdentity}[{user.Map.Name}],{user.MapX},{user.MapY}");
                    return;
            }

            if (oldAddition < target.Plus && target.Plus >= 6)
            {
                if (user.Gender == 1)
                {
                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrComposeOverpowerMale, user.Name,
                        target.Itemtype.Name, target.Plus), MsgTalk.TalkChannel.TopLeft);
                }
                else
                {
                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrComposeOverpowerFemale, user.Name,
                        target.Itemtype.Name, target.Plus), MsgTalk.TalkChannel.TopLeft);
                }
            }

            await target.SaveAsync();
            await user.SendAsync(new MsgItemInfo(target, MsgItemInfo.ItemMode.Update));
        }

        private static ushort PlusAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0:
                    if (steed) return 1;
                    return 0;
                case 1: return 10;
                case 2: return 40;
                case 3: return 120;
                case 4: return 360;
                case 5: return 1080;
                case 6: return 3240;
                case 7: return 9720;
                case 8: return 29160;
                default: return 0;
            }
        }

        private static ushort GetAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0: return 20;
                case 1: return 20;
                case 2:
                    if (steed) return 90;
                    return 80;
                case 3: return 240;
                case 4: return 720;
                case 5: return 2160;
                case 6: return 6480;
                case 7: return 19440;
                case 8: return 58320;
                case 9: return 2700;
                case 10: return 5500;
                case 11: return 9000;
                default: return 0;
            }
        }
    }
}