namespace Moth.Tasks
{
    using System;
    using System.Threading;


    /// <summary>
    /// Handle for checking task status.
    /// </summary>
    public readonly struct TaskHandle : IEquatable<TaskHandle>
    {
        private readonly ITaskHandleManager manager;
        private readonly int handleID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHandle"/> struct.
        /// </summary>
        /// <param name="manager">Reference to <see cref="TaskHandleManager"/> that the task belongs to.</param>
        /// <param name="handleID">ID of handle.</param>
        public TaskHandle (ITaskHandleManager manager, int handleID)
        {
            this.manager = manager;
            this.handleID = handleID;
        }

        /// <summary>
        /// Gets a value indicating whether the handle is valid.
        /// </summary>
        public bool IsValid => manager != null;

        /// <summary>
        /// Checks if the task has been completed.
        /// </summary>
        /// <remarks>
        /// Please note that this property does not indicate whether the task executed successfully or not.
        /// </remarks>
        public bool IsComplete => manager.IsTaskComplete (this);

        /// <summary>
        /// Gets the <see cref="TaskHandleManager"/> that the task belongs to.
        /// </summary>
        internal ITaskHandleManager Manager => manager;

        /// <summary>
        /// Gets the ID of the handle.
        /// </summary>
        internal int ID => handleID;

        /// <inheritdoc/>
        public bool Equals (TaskHandle other) => other.manager == manager && other.handleID == handleID;

        /// <summary>
        /// Waits indefinitely until the task has been completed.
        /// </summary>
        public void WaitForCompletion () => WaitForCompletion (System.Threading.Timeout.Infinite);

        /// <summary>
        /// Waits for a maximum time in milliseconds for the task to complete.
        /// </summary>
        /// <param name="millisceondsTimeout">The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="token">Cancellation token to observe.</param>
        /// <returns><see langword="true"/> if the task was completed before timeout; otherwise, <see langword="false"/>.</returns>
        public bool WaitForCompletion (int millisceondsTimeout, CancellationToken token = default) => manager.WaitForCompletion (this, millisceondsTimeout, token);

        /// <summary>
        /// Notifies the task manager that the task has completed.
        /// </summary>
        public void NotifyTaskCompletion () => manager.NotifyTaskCompletion (this);
    }
}
