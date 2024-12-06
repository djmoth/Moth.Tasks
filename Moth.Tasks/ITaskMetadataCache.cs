namespace Moth.Tasks
{
    /// <summary>
    /// Interface for a task metadata cache.
    /// </summary>
    public interface ITaskMetadataCache
    {
        /// <summary>
        /// Get the metadata of a task by ID.
        /// </summary>
        /// <param name="id">ID of task.</param>
        /// <returns><see cref="ITaskMetadata"/> instance with ID of <paramref name="id"/>.</returns>
        ITaskMetadata GetTask (int id);

        /// <summary>
        /// Get the metadata of a task by type.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <returns><see cref="ITaskMetadata"/> instance for the type <typeparamref name="TTask"/>.</returns>
        ITaskMetadata<TTask> GetTask<TTask> () where TTask : struct, ITaskType;
    }
}