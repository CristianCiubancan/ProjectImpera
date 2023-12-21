using System.Threading.Tasks;
using Comet.Network.Packets;

namespace Comet.Game.States.BaseEntities
{
    public abstract class Role
    {
        public uint UID { get; }

        uint Map { get; }
        bool Alive { get; }
        protected ushort currentX,
                         currentY;
        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public virtual ushort X
        {
            get => currentX;
            set => currentX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public virtual ushort Y
        {
            get => currentY;
            set => currentY = value;
        }
        public virtual Task SendSpawnToAsync(Character player)
        {
            return Task.CompletedTask;
        }
        public virtual Task SendAsync(IPacket packet)
        {
            return Task.CompletedTask;
        }
        public virtual Task SendAsync(byte[] msg)
        {
            return Task.CompletedTask;
        }
        public virtual Task EnterMapAsync()
        {
            return Task.CompletedTask;
        }
    }
}
