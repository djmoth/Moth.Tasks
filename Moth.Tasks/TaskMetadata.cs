namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Static class for generating <see cref="ITaskMetadata"/> instances.
    /// </summary>
    public static class TaskMetadata
    {
        /// <summary>
        /// <see cref="TaskMetadataProvider"/> for creating <see cref="ITaskMetadata"/> instances using <see cref="Format.Provider"/> for serialization.
        /// </summary>
        public static readonly TaskMetadataProvider Provider = new TaskMetadataProvider (Format.Provider);

        /// <summary>
        /// Creates a new <see cref="ITaskMetadata{TTask}"/> instance from a type and ID.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new <see cref="ITaskMetadata{TTask}"/> instance representing the task <typeparamref name="TTask"/>.</returns>
        internal static unsafe ITaskMetadata<TTask> Create<TTask> (int id)
             where TTask : struct, ITaskType
            => Provider.Create<TTask> (id);
    }
}
