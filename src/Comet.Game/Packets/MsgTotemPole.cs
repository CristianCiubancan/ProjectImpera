// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTotemPole.cs
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
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTotemPole : MsgBase<Client>
    {

        public ActionMode Action { get; set; }
        public int Data1 { get; set; }
        public int Data2 { get; set; }
        public int Data3 { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }


        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (ActionMode)reader.ReadUInt32();
            Data1 = reader.ReadInt32();
            Data2 = reader.ReadInt32();
            Data3 = reader.ReadInt32();
            Unknown20 = reader.ReadInt32();
            Unknown24 = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTotemPole);
            writer.Write((uint)Action);
            writer.Write(Data1);
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Unknown20);
            writer.Write(Unknown24);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = Kernel.RoleManager.GetUser(client.Character.Identity);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            if (user.SyndicateIdentity == 0)
                return;

            switch (Action)
            {
                case ActionMode.UnlockArsenal:
                    {
                        if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.DeputyLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.HonoraryDeputyLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.LeaderSpouse)
                            return;

                        Syndicate.TotemPoleType type = (Syndicate.TotemPoleType)Data1;
                        if (type == Syndicate.TotemPoleType.None)
                            return;

                        if (user.Syndicate.LastOpenTotem != null)
                        {
                            int now = int.Parse($"{DateTime.Now:yyyyMMdd}");
                            int lastOpenTotem = int.Parse($"{user.Syndicate.LastOpenTotem.Value:yyyyMMdd}");
                            if (lastOpenTotem >= now)
                                return;
                        }

                        int price = user.Syndicate.UnlockTotemPolePrice();
                        if (user.Syndicate.Money < price)
                            return;

                        if (!await user.Syndicate.OpenTotemPoleAsync(type))
                            return;

                        user.Syndicate.Money -= price;
                        await user.Syndicate.SaveAsync();

                        await user.Syndicate.SendTotemPolesAsync(user);
                        await user.SendSyndicateAsync();
                        break;
                    }

                case ActionMode.InscribeItem:
                    {
                        Item item = user.UserPackage[(uint)Data2];
                        if (item == null)
                            return;

                        await user.Syndicate.InscribeItemAsync(user, item);
                        break;
                    }

                case ActionMode.UnsubscribeItem:
                    {
                        await user.Syndicate.UnsubscribeItemAsync((uint)Data2, user.Identity);
                        break;
                    }

                case ActionMode.Enhance:
                    {
                        if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                            return;

                        if (await user.Syndicate.EnhanceTotemPoleAsync((Syndicate.TotemPoleType)Data1, (byte)Data3))
                        {
                            await user.Syndicate.SendTotemPolesAsync(user);
                            await user.SendSyndicateAsync();
                        }

                        break;
                    }

                case ActionMode.Refresh:
                    {
                        await user.Syndicate.SendTotemPolesAsync(user);
                        break;
                    }
            }
        }

        public enum ActionMode
        {
            UnlockArsenal,
            InscribeItem,
            UnsubscribeItem,
            Enhance,
            Refresh
        }
    }
}