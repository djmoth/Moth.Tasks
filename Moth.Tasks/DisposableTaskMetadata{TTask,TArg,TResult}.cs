namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that can be disposed and takes an argument and returns a result.
    /// </summary>
    /// <inheritdoc />
    internal class DisposableTaskMetadata<TTask, TArg, TResult> : DisposableTaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskMetadata{TTask, TArg, TResult}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public DisposableTaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool HasArgs => true;

        /// <inheritdoc />
        public override bool HasResult => true;

        /// <summary>
        /// Runs the task with <see langword="default"/> as argument and disposes it and discards the result.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (default);
            } finally
            {
                data.Dispose ();
            }
        }

        /// <summary>
        /// Runs the task with an argument and disposes it and discards the result.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }

        /// <summary>
        /// Runs the task with an argument and disposes it and returns the result.
        /// </summary>
        /// <inheritdoc />
        /// <returns>The result of the task.</returns>
        TResult IRunnableTaskMetadata<TArg, TResult>.Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                return data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }
    }
}
