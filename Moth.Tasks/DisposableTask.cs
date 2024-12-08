namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Static class providing helper methods for tasks that implement <see cref="IDisposable"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    public static class DisposableTask<TTask>
        where TTask : struct, ITaskType, IDisposable
    {
        /// <summary>
        /// Disposes a task.
        /// </summary>
        /// <param name="task">Task to dispose.</param>
        /// <remarks>
        /// Used by <see cref="Task{TTask}.TryDispose(ref TTask)"/>.
        /// </remarks>
        public static void Dispose (ref TTask task) => task.Dispose ();
    }
}
