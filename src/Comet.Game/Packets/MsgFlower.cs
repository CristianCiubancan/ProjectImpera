// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgFlower.cs
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
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgFlower : MsgBase<Client>
    {
        public enum RequestMode
        {
            SendFlower,
            QueryIcon,
            QueryData
        }

        public enum FlowerType
        {
            RedRose,
            WhiteRose,
            Orchid,
            Tulip
        }

        public enum FlowerEffect : uint
        {
            None = 0,

            RedRose,
            WhiteRose,
            Orchid,
            Tulip,

            Kiss = 1,
            Love = 2,
            Tins = 3,
            Jade = 4,
        }

        public RequestMode Mode { get; set; }
        public uint Identity { get; set; }
        public uint ItemIdentity { get; set; }
        public uint FlowerIdentity { get; set; }
        public string SenderName { get; set; } = "";
        public uint Amount { get; set; }
        public FlowerType Flower { get; set; }
        public string ReceiverName { get; set; } = "";
        public uint SendAmount { get; set; }
        public FlowerType SendFlowerType { get; set; }
        public FlowerEffect SendFlowerEffect { get; set; }

        public uint RedRoses { get; set; }
        public uint RedRosesToday { get; set; }
        public uint WhiteRoses { get; set; }
        public uint WhiteRosesToday { get; set; }
        public uint Orchids { get; set; }
        public uint OrchidsToday { get; set; }
        public uint Tulips { get; set; }
        public uint TulipsToday { get; set; }

        public List<string> Strings { get; set; } = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16(); // 0
            Type = (PacketType)reader.ReadUInt16(); // 2
            Mode = (RequestMode)reader.ReadUInt32(); // 4
            Identity = reader.ReadUInt32(); // 8
            ItemIdentity = reader.ReadUInt32(); // 12
            FlowerIdentity = reader.ReadUInt32(); // 16
            Amount = reader.ReadUInt32(); // 20
            Flower = (FlowerType)reader.ReadUInt32(); // 24
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgFlower);
            writer.Write((uint)Mode);
            writer.Write(Identity);
            writer.Write(ItemIdentity);
            if (Mode == RequestMode.QueryIcon)
            {
                writer.Write(RedRoses);
                writer.Write(RedRosesToday);
                writer.Write(WhiteRoses);
                writer.Write(WhiteRosesToday);
                writer.Write(Orchids);
                writer.Write(OrchidsToday);
                writer.Write(Tulips);
                writer.Write(TulipsToday);
            }
            else
            {
                writer.Write(SenderName, 16);
                writer.Write(ReceiverName, 16);
            }
            writer.Write(SendAmount);
            writer.Write((uint)SendFlowerType);
            writer.Write((uint)SendFlowerEffect);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case RequestMode.SendFlower:
                    {
                        uint idTarget = Identity;
                        uint idItem = ItemIdentity;

                        Character target = Kernel.RoleManager.GetUser(idTarget);

                        if (!user.IsAlive)
                        {
                            await user.SendAsync(Language.StrFlowerSenderNotAlive);
                            return;
                        }

                        if (target == null)
                        {
                            await user.SendAsync(Language.StrTargetNotOnline);
                            return;
                        }

                        if (user.Gender != 1)
                        {
                            await user.SendAsync(Language.StrFlowerSenderNotMale);
                            return;
                        }

                        if (target.Gender != 2)
                        {
                            await user.SendAsync(Language.StrFlowerReceiverNotFemale);
                            return;
                        }

                        if (user.Level < 50)
                        {
                            await user.SendAsync(Language.StrFlowerLevelTooLow);
                            return;
                        }

                        ushort amount = 0;
                        string flowerName = Language.StrFlowerNameRed;
                        FlowerType type = FlowerType.RedRose;
                        FlowerEffect effect = FlowerEffect.RedRose;
                        if (ItemIdentity == 0) // daily flower
                        {
                            if (user.SendFlowerTime != null
                                && int.Parse(user.SendFlowerTime.Value.ToString("yyyyMMdd")) >=
                                int.Parse(DateTime.Now.ToString("yyyyMMdd")))
                            {
                                await user.SendAsync(Language.StrFlowerHaveSentToday);
                                return;
                            }

                            switch (user.BaseVipLevel)
                            {
                                case 0:
                                    amount = 1;
                                    break;
                                case 1:
                                    amount = 2;
                                    break;
                                case 2:
                                    amount = 5;
                                    break;
                                case 3:
                                    amount = 7;
                                    break;
                                case 4:
                                    amount = 9;
                                    break;
                                case 5:
                                    amount = 12;
                                    break;
                                default:
                                    amount = 30;
                                    break;
                            }

                            user.SendFlowerTime = DateTime.Now;
                            await user.SaveAsync();
                        }
                        else
                        {
                            Item flower = user.UserPackage[ItemIdentity];
                            if (flower == null)
                                return;

                            switch (flower.GetItemSubType())
                            {
                                case 751:
                                    type = FlowerType.RedRose;
                                    effect = FlowerEffect.RedRose;
                                    flowerName = Language.StrFlowerNameRed;
                                    break;
                                case 752:
                                    type = FlowerType.WhiteRose;
                                    effect = FlowerEffect.WhiteRose;
                                    flowerName = Language.StrFlowerNameWhite;
                                    break;
                                case 753:
                                    type = FlowerType.Orchid;
                                    effect = FlowerEffect.Orchid;
                                    flowerName = Language.StrFlowerNameLily;
                                    break;
                                case 754:
                                    type = FlowerType.Tulip;
                                    effect = FlowerEffect.Tulip;
                                    flowerName = Language.StrFlowerNameTulip;
                                    break;
                            }

                            amount = flower.Durability;
                            await user.UserPackage.SpendItemAsync(flower);
                        }

                         var flowersToday = await Kernel.FlowerManager.QueryFlowersAsync(target);
                        switch (type)
                        {
                            case FlowerType.RedRose:
                                target.FlowerRed += amount;
                                flowersToday.RedRose += amount;
                                break;
                            case FlowerType.WhiteRose:
                                target.FlowerWhite += amount;
                                flowersToday.WhiteRose += amount;
                                break;
                            case FlowerType.Orchid:
                                target.FlowerOrchid += amount;
                                flowersToday.Orchids += amount;
                                break;
                            case FlowerType.Tulip:
                                target.FlowerTulip += amount;
                                flowersToday.Tulips += amount;
                                break;
                        }

                        await user.SendAsync(Language.StrFlowerSendSuccess);
                        if (ItemIdentity != 0 && amount >= 99)
                        {
                            await Kernel.RoleManager.BroadcastMsgAsync(
                                string.Format(Language.StrFlowerGmPromptAll, user.Name, amount, flowerName, target.Name),
                                MsgTalk.TalkChannel.Center);
                        }

                        await target.SendAsync(string.Format(Language.StrFlowerReceiverPrompt, user.Name));
                        await user.BroadcastRoomMsgAsync(new MsgFlower
                        {
                            Identity = Identity,
                            ItemIdentity = ItemIdentity,
                            SenderName = user.Name,
                            ReceiverName = target.Name,
                            SendAmount = amount,
                            SendFlowerType = type,
                            SendFlowerEffect = effect
                        }, true);

                        await user.SendAsync(new MsgFlower
                        {
                            Mode = RequestMode.QueryIcon
                        });

                        await BaseRepository.SaveAsync(flowersToday.GetDatabaseObject());
                        break;
                    }
                default:
                    {
                        await Log.WriteLogAsync(LogLevel.Error, $"Unhandled MsgFlower:{Mode}");
                        await Log.WriteLogAsync(LogLevel.Debug, PacketDump.Hex(Encode()));
                        return;
                    }
            }
        }
    }
}