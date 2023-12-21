using System.Collections.Generic;

namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerAction<T> : MsgBase<T>
    {
        public enum ConnectionStatus
        {
            Success,
            AddressNotAuthorized,
            AuthorizationError,
            InvalidUsernamePassword
        }

        public ServerAction Action { get; set; }
        public int Data { get; set; }
        public int Param { get; set; }
        public List<string> Strings { get; set; } = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (ServerAction)reader.ReadUInt16();
            Data = reader.ReadInt32();
            Param = reader.ReadInt32();
            Strings = reader.ReadStrings() ?? new List<string>();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAccServerAction);
            writer.Write((ushort) Action);
            writer.Write(Data);
            writer.Write(Param);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public enum ServerAction
        {
            ConnectionResult,
        }
    }
}
