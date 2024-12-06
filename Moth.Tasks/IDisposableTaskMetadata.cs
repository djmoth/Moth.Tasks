namespace Moth.Tasks
{
    /// <summary>
    /// Represents a task that can be disposed.
    /// </summary>
    public interface IDisposableTaskMetadata
    {
        /// <summary>
        /// Disposes of the task.
        /// </summary>
        /// <param name="access"><see cref="TaskQueue.TaskDataAccess"/> instance allowing for retrieval of task data.</param>
        void Dispose (TaskQueue.TaskDataAccess access);
    }
}
