using System.Collections.Generic;

namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerCmd<T> : MsgBase<T>
    {
        public ServerAction Action { get; set; }
        public uint AccountIdentity { get; set; }
        public uint Param { get; set; }
        public List<string> Strings { get; set; } = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (ServerAction)reader.ReadInt32();
            AccountIdentity = reader.ReadUInt32();
            Param = reader.ReadUInt32();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgAccServerCmd);
            writer.Write((int)Action);
            writer.Write(AccountIdentity);
            writer.Write(Param);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public enum ServerAction
        {
            Disconnect,
            Ban,
            Maintenance
        }
    }
}