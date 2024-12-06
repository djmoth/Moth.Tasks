namespace Moth.Tasks
{
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that takes an argument and returns no result.
    /// </summary>
    /// <typeparam name="TTask">Type of the task.</typeparam>
    /// <typeparam name="TArg">Type of the argument.</typeparam>
    internal class TaskMetadata<TTask, TArg> : TaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg>
        where TTask : struct, ITask<TArg>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadata{TTask, TArg}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public TaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool IsDisposable => false;

        /// <inheritdoc />
        public override bool HasArgs => true;

        /// <inheritdoc />
        public override bool HasResult => false;

        /// <summary>
        /// Runs the task with <see langword="default"/> as argument.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Run (default);

        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);
    }
}
