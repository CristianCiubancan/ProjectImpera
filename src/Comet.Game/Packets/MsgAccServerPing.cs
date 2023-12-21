using Comet.Game.Internal;
using Comet.Network.Packets.Internal;
using System.Threading.Tasks;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerPing : MsgAccServerPing<AccountServer>
    {
        public override Task ProcessAsync(AccountServer client)
        {
            return client.SendAsync(new MsgAccServerGameInformation
            {
                PlayerCount = Kernel.RoleManager.OnlinePlayers,
                PlayerCountRecord = Kernel.RoleManager.MaxOnlinePlayers,
                PlayerLimit = Kernel.RoleManager.MaxOnlinePlayers,
                Status = 1
            });
        }
    }
}