namespace Moth.Tasks
{
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that takes an argument and returns a result.
    /// </summary>
    /// <typeparam name="TTask">Type of the task.</typeparam>
    /// <typeparam name="TArg">Type of the argument.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    internal class TaskMetadata<TTask, TArg, TResult> : TaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadata{TTask, TArg, TResult}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public TaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool HasArgs => true;

        /// <inheritdoc />
        public override bool HasResult => true;

        /// <summary>
        /// Runs the task with <see langword="default"/> as argument and discards the result.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask task = access.GetNextTaskData (this);
            task.TryRunAndDispose (default (TArg), out TResult _);
        }

        /// <summary>
        /// Runs the task with an argument and discards the result.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask task = access.GetNextTaskData (this);
            task.TryRunAndDispose (arg, out TResult _);
        }

        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access, TArg arg, out TResult result)
        {
            TTask task = access.GetNextTaskData (this);
            task.TryRunAndDispose (arg, out result);
        }
    }
}
