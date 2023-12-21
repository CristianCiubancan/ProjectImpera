using Comet.Game.Packets;
using System.Threading.Tasks;

namespace Comet.Game.States
{
    public sealed class TimedMessageBox : MessageBox
    {
        public TimedMessageBox(Character user, int seconds)
            : base(user)
        {
            TimeOut = seconds;
        }

        public int MessageId { get; set; }
        public int AcceptMsgId { get; set; }
        public int Priority { get; set; }

        public uint TargetMapIdentity { get; set; } = 1002;
        public ushort[] TargetMapX { get; set; } = { 430 };
        public ushort[] TargetMapY { get; set; } = { 378 };

        public override async Task OnAcceptAsync()
        {
            if (HasExpired)
                return;

            if (m_owner.Map.IsChgMapDisable() || m_owner.Map.IsTeleportDisable() || m_owner.Map.IsPrisionMap())
                return;

            int idx = await Kernel.NextAsync(TargetMapX.Length) % TargetMapX.Length;
            ushort x = TargetMapX[idx],
                   y = TargetMapY[idx];

            await m_owner.FlyMapAsync(TargetMapIdentity, x, y);
        }

        public override Task OnTimerAsync()
        {
            return base.OnTimerAsync();
        }

        public override Task SendAsync()
        {
            m_expiration.Startup(TimeOut);
            return m_owner.SendAsync(new MsgInviteTrans
            {
                Mode = MsgInviteTrans.Action.Pop,
                Message = MessageId,
                Priority = Priority,
                Seconds = TimeOut
            });
        }
    }
}
