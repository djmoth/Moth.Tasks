namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Interface for a worker.
    /// </summary>
    public interface IWorker : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the worker is started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Start the worker.
        /// </summary>
        void Start ();

        /// <summary>
        /// Wait for the worker to finish.
        /// </summary>
        void Join ();
    }
}
