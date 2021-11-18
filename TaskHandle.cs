namespace Moth.Tasks
{
    /// <summary>
    /// Handle for checking task status.
    /// </summary>
    public readonly struct TaskHandle
    {
        private readonly TaskQueue queue;
        private readonly int handleID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHandle"/> struct.
        /// </summary>
        /// <param name="queue">Reference to queue that the task belongs to.</param>
        /// <param name="handleID">ID of handle.</param>
        internal TaskHandle (TaskQueue queue, int handleID)
        {
            this.queue = queue;
            this.handleID = handleID;
        }

        /// <summary>
        /// Checks if the task has been completed.
        /// </summary>
        /// <remarks>
        /// Please note that this property does not indicate whether the task executed successfully or not.
        /// </remarks>
        public bool IsComplete => queue.IsTaskComplete (handleID);

        /// <summary>
        /// Waits indefinitely until the task has been completed.
        /// </summary>
        public void WaitForCompletion () => WaitForCompletion (-1);

        /// <summary>
        /// Waits for a maximum time in milliseconds for the task to complete.
        /// </summary>
        /// <param name="millisceondsTimeout">The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns><see langword="true"/> if the task was completed before timeout; otherwise, <see langword="false"/>.</returns>
        public bool WaitForCompletion (int millisceondsTimeout) => queue.WaitForCompletion (handleID, millisceondsTimeout);
    }
}
