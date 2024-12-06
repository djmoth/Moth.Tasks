namespace Moth.Tasks
{
    using Moth.IO.Serialization;

    internal class TaskInfo<TTask, TArg> : TaskInfoBase<TTask>, IRunnableTaskInfo<TArg>
        where TTask : struct, ITask<TArg>
    {
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => true;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access) => Run (default);

        public void Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);
    }
}
