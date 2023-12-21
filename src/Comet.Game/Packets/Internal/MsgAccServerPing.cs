using System;

namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerPing<T> : MsgBase<T>
    {
        public MsgAccServerPing()
        {
            Timestamp = Environment.TickCount64;
        }

        public long Timestamp { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Timestamp = reader.ReadInt64();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAccServerPing);
            writer.Write(Timestamp);
            return writer.ToArray();
        }
    }
}