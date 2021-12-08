namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Encapsulates a task enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="T">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct TaskWithHandle<T> : ITask, IDisposable where T : struct, ITask
    {
        private readonly T task;
        private readonly TaskQueue queue;
        private readonly int handleID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWithHandle{T}"/> struct.
        /// </summary>
        /// <param name="queue">Reference to the <see cref="TaskQueue"/> in which the task is enqueued.</param>
        /// <param name="task">Task data.</param>
        /// <param name="handleID">ID of handle.</param>
        public TaskWithHandle (TaskQueue queue, in T task, int handleID)
        {
            this.queue = queue;
            this.task = task;
            this.handleID = handleID;
        }

        /// <inheritdoc/>
        public void Run ()
        {
            task.Run ();
        }

        /// <summary>
        /// Notify handle that task was completed.
        /// </summary>
        public void Dispose ()
        {
            queue.NotifyTaskComplete (handleID);
        }
    }

    /// <summary>
    /// Encapsulates a task which implements <see cref="IDisposable"/>, enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="T">Type of task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal readonly struct DisposableTaskWithHandle<T> : ITask, IDisposable where T : struct, ITask
    {
        private static readonly IDisposable DisposableTemplate = (IDisposable)default (T);

        private readonly T task;
        private readonly TaskQueue queue;
        private readonly int handleID;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskWithHandle{T}"/> struct.
        /// </summary>
        /// <param name="queue">Reference to the <see cref="TaskQueue"/> in which the task is enqueued.</param>
        /// <param name="task">Task data.</param>
        /// <param name="handleID">ID of handle.</param>
        public DisposableTaskWithHandle (TaskQueue queue, in T task, int handleID)
        {
            this.queue = queue;
            this.task = task;
            this.handleID = handleID;
        }

        /// <inheritdoc/>
        public void Run () => task.Run ();

        /// <summary>
        /// Dispose of encapsulated task, and notify handle that task was completed.
        /// </summary>
        public unsafe void Dispose ()
        {
            int headerSize = IntPtr.Size * 2;
            int dataSize = headerSize + Unsafe.SizeOf<T> (); // (IntPtr.Size * 2) for object header, plus actual task data
            byte* disposableCopyData = stackalloc byte[dataSize];

            RefPointerUnion disposableTemplateRef = new RefPointerUnion { Reference = DisposableTemplate };
            RefPointerUnion disposableCopyRef = new RefPointerUnion { Pointer = disposableCopyData + headerSize };

            Unsafe.CopyBlock (disposableCopyData, (byte*)disposableTemplateRef.Pointer - headerSize, (uint)dataSize);

            disposableCopyRef.Reference.Dispose ();

            queue.NotifyTaskComplete (handleID);
        }

        [StructLayout (LayoutKind.Explicit)]
        private unsafe struct RefPointerUnion
        {
            [FieldOffset (0)]
            public void* Pointer;
            [FieldOffset (0)]
            public IDisposable Reference;
        }
    }
}
