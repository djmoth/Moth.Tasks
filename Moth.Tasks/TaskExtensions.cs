namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Static class for task extensions.
    /// </summary>
    public static class TaskExtensions
    {
        public static void Run<TTask> (this ref TTask task)
            where TTask : struct, ITask<Unit, Unit>
            => task.Run (default);

        /// <summary>
        /// Tries to run a task and disposes it if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="task">Task to run and possibly dispose.</param>
        public static void TryRunAndDispose<TTask> (this ref TTask task)
            where TTask : struct, ITask<Unit, Unit>
        {
            if (task is IDisposable)
            {
                try
                {
                    task.Run (default);
                } finally
                {
                    Task<TTask>.TryDispose (ref task);
                }
            } else
            {
                task.Run (default);
            }
        }

        /// <summary>
        /// Tries to run a task with an argument and disposes it if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <typeparam name="TArg">Type of task argument.</typeparam>
        /// <param name="task">Task to run and possibly dispose.</param>
        /// <param name="arg">Task argument.</param>
        public static void TryRunAndDispose<TTask, TArg> (this ref TTask task, TArg arg)
            where TTask : struct, ITask<TArg, Unit>
        {
            if (task is IDisposable)
            {
                try
                {
                    task.Run (arg);
                } finally
                {
                    Task<TTask>.TryDispose (ref task);
                }
            } else
            {
                task.Run (arg);
            }
        }

        /// <summary>
        /// Tries to run a task with an argument and disposes it if it implements <see cref="IDisposable"/> and returns the result.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <typeparam name="TArg">Type of task argument.</typeparam>
        /// <typeparam name="TResult">Type of task result.</typeparam>
        /// <param name="task">Task to run and possibly dispose.</param>
        /// <param name="arg">Task argument.</param>
        /// <param name="result">Task result.</param>
        public static void TryRunAndDispose<TTask, TArg, TResult> (this ref TTask task, TArg arg, out TResult result)
            where TTask : struct, ITask<TArg, TResult>
        {
            if (task is IDisposable)
            {
                try
                {
                    result = task.Run (arg);
                } finally
                {
                    Task<TTask>.TryDispose (ref task);
                }
            } else
            {
                result = task.Run (arg);
            }
        }

        /// <inheritdoc cref="Task{TTask}.TryDispose(ref TTask)"/>
        public static void TryDispose<TTask> (this ref TTask task)
            where TTask : struct, ITask
            => Task<TTask>.TryDispose (ref task);

        /// <summary>
        /// Chains two tasks together.
        /// </summary>
        /// <typeparam name="T1">Type of first task.</typeparam>
        /// <typeparam name="T2">Type of second task.</typeparam>
        /// <param name="task">First task.</param>
        /// <param name="secondTask">Second task.</param>
        /// <returns>A new <see cref="ChainedTask{T1, T2}"/> which runs the tasks in sequence.</returns>
        /// <exception cref="NotSupportedException">Either one of <typeparamref name="T1"/> or <typeparamref name="T2"/> implements <see cref="IDisposable"/>.</exception>
        public static ChainedTask<T1, T2> Then<T1, T2> (this T1 task, T2 secondTask) where T1 : struct, ITask<Unit, Unit> where T2 : struct, ITask<Unit, Unit>
        {
            if (task is IDisposable || secondTask is IDisposable)
                throw new NotSupportedException ("Chaining of tasks implementing IDisposable is not currently supported.");

            return new ChainedTask<T1, T2> (task, secondTask);
        }
    }
}
