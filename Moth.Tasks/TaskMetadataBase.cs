namespace Moth.Tasks
{
    using System;
    using System.Diagnostics;
    using Moth.IO.Serialization;

    /// <inheritdoc cref="ITaskMetadata{TTask}"/>
    /// <typeparam name="TTask">Type of task.</typeparam>
    public abstract class TaskMetadataBase<TTask> : ITaskMetadata<TTask>
        where TTask : struct, ITaskType
    {
        private readonly IFormat<TTask> taskFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadataBase{TTask}"/> class.
        /// </summary>
        /// <param name="id">ID of task.</param>
        /// <param name="taskFormat"><see cref="IFormat{TTask}"/> to use for serialization.</param>
        protected TaskMetadataBase (int id, IFormat<TTask> taskFormat)
        {
            ID = id;
            Type = typeof (TTask);

            this.taskFormat = taskFormat;

            if (taskFormat is IVariableFormat<TTask> varFormat)
            {
                // Count all references in format
                Span<byte> tmpTaskData = stackalloc byte[varFormat.MinSize];
                varFormat.Serialize (default, tmpTaskData, (in object obj, Span<byte> destination) =>
                {
                    ReferenceCount++;
                    return 0;
                });

                IsManaged = true;
            } else
            {
                IsManaged = false;
            }
        }

        /// <inheritdoc />
        public int ID { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        /// <remarks>
        /// The unmanaged size is the size of the task data excluding fields of reference types.
        /// </remarks>
        public int UnmanagedSize => taskFormat.MinSize;

        /// <inheritdoc />
        public int ReferenceCount { get; set; }

        /// <inheritdoc />
        public bool IsManaged { get; set; }

        /// <inheritdoc />
        public abstract bool IsDisposable { get; }

        /// <inheritdoc />
        public abstract bool HasArgs { get; }

        /// <inheritdoc />
        public abstract bool HasResult { get; }

        /// <inheritdoc />
        public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
        {
            Debug.Assert (destination.Length >= UnmanagedSize, "destination.Length was less than TaskMetadata.UnmanagedSize");

            taskFormat.Serialize (task, destination, refWriter);
        }

        /// <inheritdoc />
        public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
        {
            Debug.Assert (source.Length >= UnmanagedSize, "source.Length was less than TaskMetadata.UnmanagedSize");

            taskFormat.Deserialize (out task, source, refReader);
        }
    }
}
