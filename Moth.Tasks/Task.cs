namespace Moth.Tasks
{
    using System;

    internal struct Task<TTask, TArg, TResult> : ITask
        where TTask : struct, ITask<TArg, TResult>
    {
        private TTask task;

        public Task (TTask task)
        {
            this.task = task;
        }

        public void Run () => task.Run (default);
    }

    internal struct Task<TTask, TArg> : ITask<TArg, Unit>
        where TTask : struct, ITask<TArg>
    {
        private TTask task;

        public Task (TTask task)
        {
            this.task = task;
        }

        public Unit Run (TArg arg)
        {
            task.Run (arg);
            return default;
        }
    }

    internal struct Task<TTask> : ITask<Unit, Unit>
        where TTask : struct, ITask
    {
        private readonly TTask task;

        public Task (TTask task)
        {
            this.task = task;
        }

        public Unit Run (Unit arg)
        {
            task.Run ();
            return default;
        }
    }
}
