using Comet.Account.Database.Models;
using Comet.Network.Sockets;
using System;
using System.Linq;
using System.Net.Sockets;

namespace Comet.Account.States
{
    public sealed class GameServer : TcpServerActor
    {
        public GameServer(Socket socket, Memory<byte> buffer, uint partition) 
            : base(socket, buffer, null, partition, "8a653a5d1e92b4e1db79")
        {
        }

        public DbRealm Realm { get; private set; }

        public void SetRealm(string name)
        {
            Realm = Kernel.Realms.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
