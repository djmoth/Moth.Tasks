namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout (LayoutKind.Auto)]
    internal readonly struct TaskWithHandle<T> : ITask, IDisposable where T : struct, ITask
    {
        private readonly T task;
        private readonly TaskQueue queue;
        private readonly int handleID;

        public void Run ()
        {
            task.Run ();
        }

        public void Dispose ()
        {
            queue.NotifyTaskComplete (handleID);
        }

        public struct Disposable<T> where T : struct, ITask, IDisposable
        {
            private TaskWithHandle<T> data;

            public void Run () => data.Run ();

            public void Dispose ()
            {
                data.task.Dispose ();

                data.Dispose ();
            }
        }
    }
}
