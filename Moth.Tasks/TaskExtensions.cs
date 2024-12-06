namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Static class for task extensions.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Enqueues a task in a task queue.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="task">Task to enqueue.</param>
        /// <param name="queue">Queue to enqueue in.</param>
        public static void Enqueue<TTask> (this TTask task, ITaskQueue queue) where TTask : struct, ITask
        {
            queue.Enqueue (task);
        }

        /// <summary>
        /// Chains two tasks together.
        /// </summary>
        /// <typeparam name="T1">Type of first task.</typeparam>
        /// <typeparam name="T2">Type of second task.</typeparam>
        /// <param name="task">First task.</param>
        /// <param name="secondTask">Second task.</param>
        /// <returns>A new <see cref="ChainedTask{T1, T2}"/> which runs the tasks in sequence.</returns>
        /// <exception cref="NotSupportedException">Either one of <typeparamref name="T1"/> or <typeparamref name="T2"/> implements <see cref="IDisposable"/>.</exception>
        public static ChainedTask<T1, T2> Then<T1, T2> (this T1 task, T2 secondTask) where T1 : struct, ITask where T2 : struct, ITask
        {
            if (task is IDisposable || secondTask is IDisposable)
                throw new NotSupportedException ("Chaining of tasks implementing IDisposable is not currently supported.");

            return new ChainedTask<T1, T2> (task, secondTask);
        }
    }
}
