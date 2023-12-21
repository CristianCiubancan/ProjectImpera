// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgGemEmbed.cs
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
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgGemEmbed : MsgBase<Client>
    {
        public enum EmbedAction : ushort
        {
            Embed = 0,
            TakeOff = 1,
        };

        public MsgGemEmbed()
        {
            Type = PacketType.MsgGemEmbed;
        }

        public uint Identity { get; set; }
        public uint MainIdentity { get; set; }
        public uint MinorIdentity { get; set; }
        public ushort Position { get; set; }
        public EmbedAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            MainIdentity = reader.ReadUInt32();
            MinorIdentity = reader.ReadUInt32();
            Position = reader.ReadUInt16();
            Action = (EmbedAction) reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Identity);
            writer.Write(MainIdentity);
            writer.Write(MinorIdentity);
            writer.Write(Position);
            writer.Write((ushort) Action);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (Identity != user.Identity)
            {
                await Log.GmLogAsync("cheat", $"MsgGemEmbed invalid user identity {Identity} != {user.Identity}");
                return;
            }

            Item main = user.UserPackage[MainIdentity];
            if (main == null)
                return;

            switch (Action)
            {
                case EmbedAction.Embed:
                    Item minor = user.UserPackage[MinorIdentity];
                    if (minor == null || minor.GetItemSubType() != 700)
                    {
                        await user.SendAsync(Language.StrNoGemEmbed);
                        return;
                    }

                    Item.SocketGem gem = (Item.SocketGem) (minor.Type % 1000);
                    if (!Enum.IsDefined(typeof(Item.SocketGem), (byte) gem))
                    {
                        await user.SendAsync(Language.StrNoGemEmbed);
                        return;
                    }

                    if (main.GetItemSubType() == 201)
                    {
                        switch (gem)
                        {
                            case Item.SocketGem.NormalThunderGem:
                            case Item.SocketGem.RefinedThunderGem:
                            case Item.SocketGem.SuperThunderGem:
                                break;
                            default:
                                await user.SendAsync(Language.StrNoGemEmbed);
                                return;
                        }
                    }
                    else if (main.GetItemSubType() == 202)
                    {
                        switch (gem)
                        {
                            case Item.SocketGem.NormalGloryGem:
                            case Item.SocketGem.RefinedGloryGem:
                            case Item.SocketGem.SuperGloryGem:
                                break;
                            default:
                                await user.SendAsync(Language.StrNoGemEmbed);
                                return;
                        }
                    }

                    if (Position == 1 || (Position == 2 && main.SocketOne == Item.SocketGem.EmptySocket))
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket)
                        {
                            await user.SendAsync(Language.StrEmbedTargetNoSocket);
                            return;
                        }

                        if (main.SocketOne != Item.SocketGem.EmptySocket)
                        {
                            await user.SendAsync(Language.StrEmbedSocketAlreadyFilled);
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(minor))
                        {
                            await user.SendAsync(Language.StrEmbedNoRequiredItem);
                            return;
                        }

                        main.SocketOne = gem;
                        await main.SaveAsync();
                        break;
                    }

                    if (Position == 2)
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket || main.SocketOne == Item.SocketGem.EmptySocket)
                            return;

                        if (main.SocketTwo == Item.SocketGem.NoSocket)
                        {
                            await user.SendAsync(Language.StrEmbedNoSecondSocket);
                            return;
                        }

                        if (main.SocketTwo != Item.SocketGem.EmptySocket)
                        {
                            await user.SendAsync(Language.StrEmbedSocketAlreadyFilled);
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(minor))
                        {
                            await user.SendAsync(Language.StrEmbedNoRequiredItem);
                            return;
                        }

                        main.SocketTwo = gem;
                        await main.SaveAsync();
                        break;
                    }

                    break;

                case EmbedAction.TakeOff:
                    if (Position == 1)
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket)
                            return;
                        if (main.SocketOne == Item.SocketGem.EmptySocket)
                            return;

                        main.SocketOne = Item.SocketGem.EmptySocket;

                        if (main.SocketTwo != Item.SocketGem.NoSocket && main.SocketTwo != Item.SocketGem.EmptySocket)
                        {
                            main.SocketOne = main.SocketTwo;
                            main.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await main.SaveAsync();
                        break;
                    }

                    if (Position == 2)
                    {
                        if (main.SocketTwo == Item.SocketGem.NoSocket)
                            return;
                        if (main.SocketTwo == Item.SocketGem.EmptySocket)
                            return;

                        main.SocketTwo = Item.SocketGem.EmptySocket;
                        await main.SaveAsync();
                        break;
                    }

                    break;
            }

            await user.SendAsync(new MsgItemInfo(main, MsgItemInfo.ItemMode.Update));
            await user.SendAsync(this);
        }
    }
}