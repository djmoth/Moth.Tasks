namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Contains metadata about a task type.
    /// </summary>
    public interface ITaskMetadata
    {
        /// <summary>
        /// Gets the ID of the task type in an <see cref="ITaskMetadataCache"/>.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Gets the runtime <see cref="System.Type"/> of the task.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the size of unmanaged task data in bytes.
        /// </summary>
        int UnmanagedSize { get; }

        /// <summary>
        /// Gets the number of reference fields in the task.
        /// </summary>
        int ReferenceCount { get; }

        /// <summary>
        /// Gets whether the task contains reference types.
        /// </summary>
        bool IsManaged { get; }

        /// <summary>
        /// Gets whether the task implements <see cref="IDisposable"/>.
        /// </summary>
        bool IsDisposable { get; }

        /// <summary>
        /// Gets whether the task takes an argument.
        /// </summary>
        bool HasArgs { get; }

        /// <summary>
        /// Gets whether the task returns a result.
        /// </summary>
        bool HasResult { get; }

        /// <summary>
        /// Disposes the task if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="access"><see cref="TaskDataAccess"/> instance allowing for retrieval of task data.</param>
        void Dispose (TaskDataAccess access);
    }
}
