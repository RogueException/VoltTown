using System.Threading.Tasks;

namespace VoltTown.Components
{
    public abstract class Component
    {
        public virtual Task Run() => Task.CompletedTask;
    }
}
