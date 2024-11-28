using System;

namespace Moth.Tasks
{
    public interface IWorker : IDisposable
    {
        bool IsStarted { get; }

        void Start ();

        void Join ();
    }
}
