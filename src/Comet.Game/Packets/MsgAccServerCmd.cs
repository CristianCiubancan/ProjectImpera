using Comet.Game.Internal;
using Comet.Game.States;
using Comet.Network.Packets.Internal;
using System.Threading.Tasks;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerCmd : MsgAccServerCmd<AccountServer>
    {
        public override async Task ProcessAsync(AccountServer client)
        {
            Character account = Kernel.RoleManager.GetUserByAccount(AccountIdentity);
            if (account == null)
                return;

            switch (Action)
            {
                case ServerAction.Disconnect:
                    {
                        await Kernel.RoleManager.KickOutAsync(account.Identity, "REALM MANAGER REQUEST");
                        break;
                    }
                case ServerAction.Ban:
                    {
                        await Kernel.RoleManager.KickOutAsync(account.Identity, "Account banned!");
                        break;
                    }
                case ServerAction.Maintenance:
                    {
                        // TODO maintenance manager
                        break;
                    }
            }
        }
    }
}
