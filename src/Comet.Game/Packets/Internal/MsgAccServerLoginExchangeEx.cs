namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerLoginExchangeEx<T> : MsgBase<T>
    {
        public ExchangeResult Result { get; set; }
        public uint AccountIdentity { get; set; }
        public ulong Token { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Result = (ExchangeResult) reader.ReadUInt32();
            AccountIdentity = reader.ReadUInt32();
            Token = reader.ReadUInt64();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAccServerLoginExchangeEx);
            writer.Write((int)Result);
            writer.Write(AccountIdentity);
            writer.Write(Token);
            return writer.ToArray();
        }

        public enum ExchangeResult
        {
            Success,
            AlreadySignedIn,
            ServerFull,
            KeyError
        }
    }
}
