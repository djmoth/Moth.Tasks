namespace Moth.Tasks
{
    /// <summary>
    /// Interface for a task metadata provider.
    /// </summary>
    public interface ITaskMetadataProvider
    {
        /// <summary>
        /// Create a task metadata instance for a task type.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="id">ID to give the instance.</param>
        /// <returns>A new task metadata instance.</returns>
        ITaskMetadata<TTask> Create<TTask> (int id) where TTask : struct, ITaskType;
    }
}
