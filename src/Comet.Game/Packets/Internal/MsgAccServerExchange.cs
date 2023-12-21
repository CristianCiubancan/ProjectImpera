namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerExchange<T> : MsgBase<T>
    {
        public string ServerName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            ServerName = reader.ReadString(16);
            Username = reader.ReadString(16);
            Password = reader.ReadString(16);
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgAccServerExchange);
            writer.Write(ServerName, 16);
            writer.Write(Username, 16);
            writer.Write(Password, 16);
            return writer.ToArray();
        }
    }
}
