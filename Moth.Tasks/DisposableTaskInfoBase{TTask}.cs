namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    internal abstract class DisposableTaskInfoBase<TTask> : TaskInfoBase<TTask>, IDisposableTaskInfo
        where TTask : struct, ITaskType, IDisposable
    {
        protected DisposableTaskInfoBase (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => true;

        public void Dispose (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Dispose ();
    }
}
