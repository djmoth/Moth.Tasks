using System.Threading;

namespace Moth.Tasks
{
    /// <summary>
    /// Represents a worker thread.
    /// </summary>
    public interface IWorkerThread
    {
        /// <summary>
        /// Start the worker thread.
        /// </summary>
        /// <param name="method">Method to run.</param>
        void Start (ThreadStart method);

        /// <summary>
        /// Join the worker thread.
        /// </summary>
        void Join ();
    }
}
