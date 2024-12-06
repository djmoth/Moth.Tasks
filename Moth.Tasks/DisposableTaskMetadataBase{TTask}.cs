namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that can be disposed.
    /// </summary>
    /// <inheritdoc />
    internal abstract class DisposableTaskMetadataBase<TTask> : TaskMetadataBase<TTask>, IDisposableTaskMetadata
        where TTask : struct, ITaskType, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskMetadataBase{TTask}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        protected DisposableTaskMetadataBase (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool IsDisposable => true;

        /// <inheritdoc />
        public void Dispose (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Dispose ();
    }
}
