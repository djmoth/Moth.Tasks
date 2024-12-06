namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    internal class DisposableTaskInfo<TTask, TArg> : DisposableTaskInfoBase<TTask>, IRunnableTaskInfo<TArg>
        where TTask : struct, ITask<TArg>, IDisposable
    {
        public DisposableTaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool HasArgs => true;

        public override bool HasResult => false;

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
    }
}
