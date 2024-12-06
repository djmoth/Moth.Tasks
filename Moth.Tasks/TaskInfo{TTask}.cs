namespace Moth.Tasks
{
    using Moth.IO.Serialization;

    /// <inheritdoc />
    internal class TaskInfo<TTask> : TaskInfoBase<TTask>, IRunnableTaskInfo
        where TTask : struct, ITask
    {
        /// <inheritdoc cref="TaskInfoBase{TTask}(int, IFormat{TTask})"/>
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool IsDisposable => false;

        public override bool HasArgs => false;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Run ();
    }
}
