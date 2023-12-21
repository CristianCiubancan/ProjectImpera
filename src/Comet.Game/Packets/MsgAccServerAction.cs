using Comet.Game.Internal;
using Comet.Network.Packets.Internal;
using Comet.Shared;
using System;
using System.Threading.Tasks;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerAction : MsgAccServerAction<AccountServer>
    {
        public override async Task ProcessAsync(AccountServer client)
        {
            switch (Action)
            {
                case ServerAction.ConnectionResult:
                    {
                        ConnectionStatus status = (ConnectionStatus)Data;
                        if (status.Equals(ConnectionStatus.Success))
                        {
                            Console.WriteLine("Authenticated successfully with the realm server.");
                        }
                        else if (status.Equals(ConnectionStatus.AddressNotAuthorized))
                        {
                            Console.WriteLine("This IP Address is not authorized to authenticate in the realms server.");
                            return;
                        }
                        else if (status.Equals(ConnectionStatus.AuthorizationError))
                        {
                            Console.WriteLine("Invalid realm authorization information.");
                            return;
                        }
                        else if (status.Equals(ConnectionStatus.InvalidUsernamePassword))
                        {
                            Console.WriteLine("Invalid realm Username or password.");
                            return;
                        }

                        if (Kernel.RoleManager.OnlinePlayers > 0)
                        {
                            // let's send all players data...
                            var players = Kernel.RoleManager.QueryUserSet();
                            MsgAccServerPlayerStatus statuses = new MsgAccServerPlayerStatus();
                            MsgAccServerPlayerExchange msg = new MsgAccServerPlayerExchange();
                            msg.ServerName = msg.ServerName = Kernel.Configuration.ServerName;
                            int idx = 0;
                            foreach (var player in players)
                            {
                                statuses.Status.Add(new MsgAccServerPlayerStatus<AccountServer>.PlayerStatus
                                {
                                    Identity = player.Client.AccountIdentity,
                                    Online = true
                                });

                                msg.Data.Add(MsgAccServerPlayerExchange.CreatePlayerData(player));

                                if (idx > 0 && idx % 30 == 0)
                                {
                                    await client.SendAsync(msg);
                                    msg.Data.Clear();
                                }

                                if (idx > 0 && idx % 500 == 0)
                                {
                                    await client.SendAsync(statuses);
                                    statuses.Status.Clear();
                                }

                                idx++;
                            }

                            if (msg.Data.Count > 0)
                                await client.SendAsync(msg);
                            if (statuses.Status.Count > 0)
                                await client.SendAsync(statuses);
                        }
                        break;
                    }
            }
        }
    }
}
