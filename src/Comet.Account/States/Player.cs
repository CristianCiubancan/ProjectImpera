using Comet.Account.Database.Models;

namespace Comet.Account.States
{
    public sealed class Player
    {
        public DbRealm Realm;
        public uint AccountIdentity;
        public DbAccount Account;
    }
}