namespace Moth.Tasks
{
    using Moth.IO.Serialization;

    /// <inheritdoc />
    internal class TaskMetadata<TTask> : TaskMetadataBase<TTask>, IRunnableTaskMetadata
        where TTask : struct, ITask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadata{TTask}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public TaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool HasArgs => false;

        /// <inheritdoc />
        public override bool HasResult => false;

        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask task = access.GetNextTaskData (this);
            task.TryRunAndDispose ();
        }
    }
}
