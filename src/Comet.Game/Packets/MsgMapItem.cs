// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgMapItem.cs
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
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgMapItem : MsgBase<Client>
    {
        public MsgMapItem()
        {
            Type = PacketType.MsgMapItem;
        }

        public uint Identity;
        public uint Itemtype;
        public ushort MapX;
        public ushort MapY;
        public Item.ItemColor Color;
        public DropType Mode;

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
            Identity = reader.ReadUInt32();
            Itemtype = reader.ReadUInt32();
            MapX = reader.ReadUInt16();
            MapY = reader.ReadUInt16();
            Color = (Item.ItemColor) reader.ReadUInt16();
            Mode = (DropType) reader.ReadUInt16();
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
            writer.Write(Identity);
            writer.Write(Itemtype);
            writer.Write(MapX);
            writer.Write(MapY);
            writer.Write((ushort) Color);
            writer.Write((ushort) Mode);
            return writer.ToArray();
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (!user.IsAlive)
            {
                await user.SendAsync(Language.StrDead);
                return;
            }

            switch (Mode)
            {
                case DropType.PickupItem:
                    if (await user.SynPositionAsync(MapX, MapY, 0))
                    {
                        await user.PickMapItemAsync(Identity);
                        await user.BroadcastRoomMsgAsync(this, true);
                    }
                    break;
                default:
                    await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                        $"Missing packet {Type}, Action {Mode}, Length {Length}"));
                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        Type, Mode, Length, PacketDump.Hex(Encode()));
                    break;
            }
        }
    }

    public enum DropType : ushort
    {
        Unknown = 0,
        LayItem = 1,
        DisappearItem = 2,
        PickupItem = 3,
        DetainItem = 4,
        LayTrap = 10,
        SynchroTrap = 11,
        DropTrap = 12
    }
}