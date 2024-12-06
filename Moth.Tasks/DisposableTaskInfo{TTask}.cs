namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;
    internal class DisposableTaskInfo<TTask> : DisposableTaskInfoBase<TTask>, IRunnableTaskInfo
        where TTask : struct, ITask, IDisposable
    {
        public DisposableTaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool HasArgs => false;

        public override bool HasResult => false;

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
