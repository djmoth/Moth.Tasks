namespace Moth.Tasks
{
    using System;

    public static partial class Task
    {
        public static void Enqueue<T> (this T task, TaskQueue queue) where T : struct, ITask
        {
            queue.Enqueue (task);
        }

        public static ChainedTask<T1, T2> Then<T1, T2> (this T1 task, T2 secondTask) where T1 : struct, ITask where T2 : struct, ITask
        {
            if (task is IDisposable || secondTask is IDisposable)
                throw new NotSupportedException ("Chaining of tasks implementing IDisposable is not currently supported.");

            return new ChainedTask<T1, T2> (task, secondTask);
        }
    }
}
