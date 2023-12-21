using Comet.Shared.Comet.Shared;
using System.Threading.Tasks;

namespace Comet.Game.World.Threading
{
    public sealed class GeneratorProcessing : TimerBase
    {
        public GeneratorProcessing()
            : base(1000, "Generator Thread")
        {
        }

        protected override async Task<bool> OnElapseAsync()
        {
            await Kernel.GeneratorManager.OnTimerAsync();
            return true;
        }
    }
}