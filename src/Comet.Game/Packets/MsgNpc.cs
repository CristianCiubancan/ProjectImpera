// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgNpc.cs
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
using Comet.Game.States.NPCs;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgNpc : MsgBase<Client>
    {
        public MsgNpc()
        {
            Type = PacketType.MsgNpc;
        }

        public uint Identity { get; set; }
        public uint Data { get; set; }
        public NpcActionType RequestType { get; set; }
        public ushort Event { get; set; }

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
            Data = reader.ReadUInt32();
            RequestType = (NpcActionType) reader.ReadUInt16();
            Event = reader.ReadUInt16();
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
            writer.Write(Data);
            writer.Write((ushort) RequestType);
            writer.Write(Event);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (RequestType)
            {
                case NpcActionType.Activate:
                    user.ClearTaskId();
                    Role role = Kernel.RoleManager.GetRole(Identity);
                    if (role is BaseNpc npc
                        && (role.MapIdentity == user.MapIdentity
                            && role.GetDistance(user) <= 18
                            || role.MapIdentity == 5000))
                    {
                        user.InteractingNpc = npc.Identity;
                        await npc.ActivateNpc(user);
                    }
                    break;
                case NpcActionType.CancelInteraction:
                    user.CancelInteraction();
                    break;
            }
        }

        public enum NpcActionType : ushort
        {
            Activate = 0,
            AddNpc = 1,
            LeaveMap = 2,
            DeleteNpc = 3,
            ChangePosition = 4,
            LayNpc = 5,

            CancelInteraction = 255
        }
    }
}