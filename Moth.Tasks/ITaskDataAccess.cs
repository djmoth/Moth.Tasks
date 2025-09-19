namespace Moth.Tasks
{
    /// <summary>
    /// Interface for accessing task data.
    /// </summary>
    public interface ITaskDataAccess
    {
        /// <summary>
        /// Enter the lock for the task data.
        /// </summary>
        void EnterLock ();

        /// <summary>
        /// Exit the lock for the task data.
        /// </summary>
        void ExitLock ();

        /// <summary>
        /// Gets the task data for the next task.
        /// </summary>
        /// <typeparam name="TTask">Type of the next task.</typeparam>
        /// <param name="taskMetadata">Metadata for the task.</param>
        /// <returns>Instance of <typeparamref name="TTask"/>.</returns>
        TTask GetNextTaskData<TTask> (ITaskMetadata<TTask> taskMetadata)
            where TTask : struct, ITask;
    }
}
