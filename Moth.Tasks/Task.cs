namespace Moth.Tasks
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Static class providing helper methods for tasks.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    public static class Task<TTask>
        where TTask : struct, ITaskType
    {
        private static readonly DisposeMethod DisposeMethodInstance;

        static Task ()
        {
            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask)))
            {
                DisposeMethodInstance = (DisposeMethod)typeof (DisposableTask<>).MakeGenericType (typeof (TTask)).GetMethod ("Dispose", BindingFlags.Static | BindingFlags.Public).CreateDelegate (typeof (DisposeMethod));
            }
        }

        private delegate void DisposeMethod (ref TTask task);

        /// <summary>
        /// Tries to dispose a task if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="task">Task to dispose.</param>
        /// <returns><see langword="true"/> if the task was disposed.</returns>
        public static bool TryDispose (ref TTask task)
        {
            if (DisposeMethodInstance == null)
                return false;

            DisposeMethodInstance (ref task);
            return true;
        }
    }
}
