namespace Moth.Tasks
{
    using System.Threading;

    /// <summary>
    /// Manages task handles.
    /// </summary>
    public interface ITaskHandleManager
    {
        /// <summary>
        /// Create a new task handle.
        /// </summary>
        /// <returns>A new unique <see cref="TaskHandle"/> associated with this <see cref="TaskHandleManager"/>.</returns>
        TaskHandle CreateTaskHandle ();

        /// <summary>
        /// Check if a task has completed.
        /// </summary>
        /// <param name="handle"><see cref="TaskHandle"/> to check.</param>
        /// <returns><see langword="true"/> if task has completed, otherwise <see langword="false"/>.</returns>
        bool IsTaskComplete (TaskHandle handle);

        /// <summary>
        /// Clear all task handles.
        /// </summary>
        void Clear ();

        /// <summary>
        /// Wait for a task to complete.
        /// </summary>
        /// <param name="handle">Handle to wait for.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns><see langword="true"/> if task was completed, <see langword="false"/> if timeout was reached.</returns>
        bool WaitForCompletion (TaskHandle handle, int millisecondsTimeout);

        /// <summary>
        /// Notify that a task has completed.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        void NotifyTaskCompletion (TaskHandle handle);
    }
}