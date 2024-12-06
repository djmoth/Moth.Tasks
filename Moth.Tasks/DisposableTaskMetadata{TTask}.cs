namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that can be disposed and takes no argument and returns no result.
    /// </summary>
    /// <inheritdoc />
    internal class DisposableTaskMetadata<TTask> : DisposableTaskMetadataBase<TTask>, IRunnableTaskMetadata
        where TTask : struct, ITask, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskMetadata{TTask}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public DisposableTaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool HasArgs => false;

        /// <inheritdoc />
        public override bool HasResult => false;

        /// <summary>
        /// Runs the task and disposes it.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run ();
            }
            finally
            {
                data.Dispose ();
            }
        }
    }
}
