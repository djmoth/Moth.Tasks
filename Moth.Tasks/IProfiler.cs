namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Describes a profiler which can measure the run time of a task.
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Signals the beginning of a new task.
        /// </summary>
        /// <param name="task">Type of task.</param>
        void BeginTask (Type task);

        /// <summary>
        /// Signals the completion of a task.
        /// </summary>
        void EndTask ();
    }
}
