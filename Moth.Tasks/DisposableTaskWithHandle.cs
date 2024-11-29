namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Encapsulates a task which implements <see cref="IDisposable"/>, enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal struct DisposableTaskWithHandle<TTask, TArg, TResult> : ITask<TArg, TResult>, IDisposable
        where TTask : struct, ITask<TArg, TResult>
    {
        [ThreadStatic]
        private static IDisposable disposableTemplateForThread;

        private TTask task;
        private TaskHandle handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskWithHandle{TTask, TArg, TResult}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public DisposableTaskWithHandle (in TTask task, TaskHandle handle)
        {
            this.task = task;
            this.handle = handle;
        }

        /// <inheritdoc/>
        public TResult Run (TArg arg) => task.Run (arg);

        /// <summary>
        /// Dispose of encapsulated task, and notify handle that task was completed.
        /// </summary>
        public unsafe void Dispose ()
        {
            IDisposable disposableTemplate = disposableTemplateForThread;

            if (disposableTemplate == null)
            {
                disposableTemplateForThread = disposableTemplate = (IDisposable)default (TTask);
            }

            ref TTask disposableData = ref Unsafe.Unbox<TTask> (disposableTemplate);

            disposableData = task; // Copy data to DisposableTemplate object

            disposableTemplate.Dispose (); // Call Dispose with data from task

            disposableData = default; // Clear data so as to not leave any object references hanging

            handle.NotifyTaskCompletion ();
        }
    }
}
