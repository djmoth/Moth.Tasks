namespace Moth.Tasks
{
    /// <summary>
    /// Describes a profiler which can measure the run time of a task.
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Signals the beginning of a new task.
        /// </summary>
        /// <param name="task">Name of task type.</param>
        void BeginTask (string task);

        /// <summary>
        /// Signals the completion of a task.
        /// </summary>
        void EndTask ();
    }
}
