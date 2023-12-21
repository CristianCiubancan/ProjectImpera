// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgAllot.cs
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
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgAllot : MsgBase<Client>
    {
        public MsgAllot()
        {
            Type = PacketType.MsgAllot;
        }

        public ushort Force { get; set; }
        public ushort Speed { get; set; }
        public ushort Health { get; set; }
        public ushort Soul { get; set; }

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
            Force = reader.ReadByte();
            Speed = reader.ReadByte();
            Health = reader.ReadByte();
            Soul = reader.ReadByte();
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
            writer.Write((byte)Force);
            writer.Write((byte)Speed);
            writer.Write((byte)Health);
            writer.Write((byte)Soul);
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

            if (Force > 0 && Force <= user.AttributePoints)
            {
                user.Strength += 1;
                user.AttributePoints -= 1;
            }
            if (Speed > 0 && Speed <= user.AttributePoints)
            {
                user.Agility += 1;
                user.AttributePoints -= 1;
            }
            if (Health > 0 && Health <= user.AttributePoints)
            {
                user.Vitality += 1;
                user.AttributePoints -= 1;
            }
            if (Soul > 0 && Soul <= user.AttributePoints)
            {
                user.Spirit += 1;
                user.AttributePoints -= 1;
            }

            await user.SendAsync(new MsgUserAttrib(client.Identity, ClientUpdateType.Strength, client.Character.Strength));
            await user.SendAsync(new MsgUserAttrib(client.Identity, ClientUpdateType.Agility, client.Character.Agility));
            await user.SendAsync(new MsgUserAttrib(client.Identity, ClientUpdateType.Vitality, client.Character.Vitality));
            await user.SendAsync(new MsgUserAttrib(client.Identity, ClientUpdateType.Spirit, client.Character.Spirit));
            await user.SendAsync(new MsgUserAttrib(client.Identity, ClientUpdateType.Atributes, client.Character.AttributePoints));

            await user.SaveAsync();
        }
    }
}