namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerLoginExchange<T> : MsgBase<T>
    {
        public string IPAddress { get; set; }
        public uint AccountID { get; set; }
        public ushort AuthorityID { get; set; }
        public string AuthorityName { get; set; }
        public byte VipLevel { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            IPAddress = reader.ReadString(15);
            AccountID = reader.ReadUInt32();
            AuthorityID = reader.ReadUInt16();
            AuthorityName = reader.ReadString(32);
            VipLevel = reader.ReadByte();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAccServerLoginExchange);

            writer.Write(IPAddress, 15);
            writer.Write(AccountID);
            writer.Write(AuthorityID);
            writer.Write(AuthorityName, 32);
            writer.Write(VipLevel);
            return writer.ToArray();
        }
    }
}
