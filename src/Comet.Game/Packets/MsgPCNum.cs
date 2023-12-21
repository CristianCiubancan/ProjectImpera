using Comet.Game.Internal;
using Comet.Game.States;
using Comet.Network.Packets;
using System.Threading.Tasks;

namespace Comet.Game.Packets
{
    public sealed class MsgPCNum : MsgBase<AccountServer>
    {
        public uint AccountIdentity;
        public string MacAddress;

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Type = (PacketType)reader.ReadUInt16();
            AccountIdentity = reader.ReadUInt32();
            MacAddress = reader.ReadString(12);
        }

        public override Task ProcessAsync(AccountServer client)
        {
            Character user = Kernel.RoleManager.GetUserByAccount(AccountIdentity);
            if (user == null)
                return Task.CompletedTask;

            user.Client.MacAddress = MacAddress;
            return Task.CompletedTask;
        }
    }
}
