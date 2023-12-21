namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerGameInformation<T> : MsgBase<T>
    {
        public int Status { get; set; }        
        public int PlayerCount { get; set; }
        public int PlayerCountRecord { get; set; }
        public int PlayerLimit { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Status = reader.ReadInt32();
            PlayerCount = reader.ReadInt32();
            PlayerCountRecord = reader.ReadInt32();
            PlayerLimit = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new ();
            writer.Write((ushort)PacketType.MsgAccServerGameInformation);
            writer.Write(Status);
            writer.Write(PlayerCount);
            writer.Write(PlayerCountRecord);
            writer.Write(PlayerLimit);
            return writer.ToArray();
        }
    }
}
