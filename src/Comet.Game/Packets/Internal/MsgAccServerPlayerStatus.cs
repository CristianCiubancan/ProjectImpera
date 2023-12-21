using System.Collections.Generic;

namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerPlayerStatus<T> : MsgBase<T>
    {
        public string ServerName { get; set; }
        public int Count => Status.Count;
        public List<PlayerStatus> Status { get; set; } = new List<PlayerStatus>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            ServerName = reader.ReadString(16);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                uint id = reader.ReadUInt32();
                bool online = reader.ReadBoolean();
                Status.Add(new PlayerStatus
                {
                    Identity = id,
                    Online = online
                });
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAccServerPlayerStatus);
            writer.Write(ServerName, 16);
            writer.Write(Count);
            foreach (var status in Status)
            {
                writer.Write(status.Identity);
                writer.Write(status.Online);
            }
            return writer.ToArray();
        }

        public struct PlayerStatus
        {
            public uint Identity;
            public bool Online;
        }
    }
}
