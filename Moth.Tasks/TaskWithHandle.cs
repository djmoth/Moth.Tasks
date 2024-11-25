namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Encapsulates a task enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct TaskHandleData<TTask>
        where TTask : struct, ITaskType
    {
        public readonly TTask Task;
        public readonly TaskHandle Handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHandleData{TTask}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public TaskHandleData (in TTask task, TaskHandle handle)
        {
            Task = task;
            Handle = handle;
        }
    }

    /// <summary>
    /// Encapsulates a task enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct TaskWithHandle<TTask> : ITask, IDisposable
        where TTask : struct, ITask
    {
        private readonly TaskHandleData<TTask> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWithHandle{T}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public TaskWithHandle (in TTask task, TaskHandle handle) => data = new TaskHandleData<TTask> (task, handle);

        /// <inheritdoc/>
        public void Run ()
        {
            data.Task.Run ();
        }

        /// <summary>
        /// Notify handle that task was completed.
        /// </summary>
        public void Dispose ()
        {
            data.Handle.NotifyTaskCompletion ();
        }
    }

    /// <summary>
    /// Encapsulates a task enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    /// <typeparam name="TArg">Type of task argument.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct TaskWithHandle<TTask, TArg> : ITask<TArg>, IDisposable
        where TTask : struct, ITask<TArg>
    {
        private readonly TaskHandleData<TTask> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWithHandle{T}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public TaskWithHandle (in TTask task, TaskHandle handle) => data = new TaskHandleData<TTask> (task, handle);

        /// <inheritdoc/>
        public void Run (TArg arg)
        {
            data.Task.Run (arg);
        }

        /// <summary>
        /// Notify handle that task was completed.
        /// </summary>
        public void Dispose ()
        {
            data.Handle.NotifyTaskCompletion ();
        }
    }

    /// <summary>
    /// Encapsulates a task which implements <see cref="IDisposable"/>, enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct DisposableTaskWithHandle<TTask> : ITask, IDisposable
        where TTask : struct, ITask
    {
        [ThreadStatic]
        private static IDisposable disposableTemplateForThread;

        private readonly TaskHandleData<TTask> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskWithHandle{T}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public DisposableTaskWithHandle (in TTask task, TaskHandle handle) => data = new TaskHandleData<TTask> (task, handle);

        /// <inheritdoc/>
        public void Run () => data.Task.Run ();

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

            disposableData = data.Task; // Copy data to DisposableTemplate object

            disposableTemplate.Dispose (); // Call Dispose with data from task

            disposableData = default; // Clear data so as to not leave any object references hanging

            data.Handle.NotifyTaskCompletion ();
        }
    }

    /// <summary>
    /// Encapsulates a task which implements <see cref="IDisposable"/>, enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct DisposableTaskWithHandle<TTask, TArg> : ITask<TArg>, IDisposable
        where TTask : struct, ITask<TArg>
    {
        [ThreadStatic]
        private static IDisposable disposableTemplateForThread;

        private readonly TaskHandleData<TTask> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskWithHandle{TTask, TArg}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public DisposableTaskWithHandle (in TTask task, TaskHandle handle) => data = new TaskHandleData<TTask> (task, handle);

        /// <inheritdoc/>
        public void Run (TArg arg) => data.Task.Run (arg);

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

            disposableData = data.Task; // Copy data to DisposableTemplate object

            disposableTemplate.Dispose (); // Call Dispose with data from task

            disposableData = default; // Clear data so as to not leave any object references hanging

            data.Handle.NotifyTaskCompletion ();
        }
    }
}
