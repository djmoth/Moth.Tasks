namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    internal class TaskInfo<TTask, TArg, TResult> : TaskInfoBase<TTask>, IRunnableTaskInfo<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>
    {
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => true;

        public override bool HasResult => true;

        public void Run (TaskQueue.TaskDataAccess access) => Run (default);

        public void Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);

        TResult IRunnableTaskInfo<TArg, TResult>.Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);
    }
}
