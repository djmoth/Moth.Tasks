namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Validation;

    /// <summary>
    /// Representation of a task in a <see cref="TaskCache"/>.
    /// </summary>
    internal abstract class TaskInfo
    {
        protected TaskInfo (int id, Type type)
        {
            ID = id;
            Type = type;
        }

        /// <summary>
        /// Gets the ID of the task.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets the runtime <see cref="System.Type"/> of the task.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the size of unmanaged task data in bytes.
        /// </summary>
        /// <remarks>
        /// The unmanaged size is the size of the task data excluding fields of reference types.
        /// </remarks>
        public abstract int UnmanagedSize { get; }

        /// <summary>
        /// Gets the number of reference fields in the task.
        /// </summary>
        public int ReferenceCount { get; protected set; }

        /// <summary>
        /// Gets whether the task contains reference types.
        /// </summary>
        public bool IsManaged { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the task type implements <see cref="IDisposable"/>.
        /// </summary>
        public abstract bool Disposable { get; }

        /// <summary>
        /// Creates a new TaskInfo from a type and ID.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new TaskInfo representing the task <typeparamref name="T"/>.</returns>
        public static unsafe TaskInfo<T> Create<T> (int id) where T : struct, ITask
        {
            Type type = typeof (T);

            if (typeof (IDisposable).IsAssignableFrom (type)) // If T implements IDisposable
            {
                Type disposableImplementationOfT = typeof (DisposableTaskInfo<>).MakeGenericType (type);

                return (TaskInfo<T>)Activator.CreateInstance (disposableImplementationOfT, id);
            } else
            {
                return new TaskInfo<T> (id);
            }
        }

        /// <summary>
        /// Call the <see cref="ITask.Run"/> method of the task, with <see cref="TaskQueue.TaskDataAccess"/> for getting task data. Also calls <see cref="IDisposable.Dispose"/>, if implemented.
        /// </summary>
        /// <param name="access">Access to data from <see cref="TaskQueue"/>.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public abstract void RunAndDispose (ref TaskQueue.TaskDataAccess access);

        /// <summary>
        /// Call the <see cref="IDisposable.Dispose"/> method of the task, with <see cref="TaskQueue.TaskDataAccess"/> for getting task data.
        /// </summary>
        /// <param name="access">Access to data from <see cref="TaskQueue"/>.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public virtual void Dispose (in TaskQueue.TaskDataAccess access) { }
    }

    internal class TaskInfo<T> : TaskInfo where T : struct, ITask
    {
        private Format<T> taskFormat;

        public TaskInfo (int id)
            : base (id, typeof (T))
        {
            taskFormat = (Format<T>)Formats.Get<T> ();

            if (taskFormat is VariableFormat<T> varFormat)
            {
                // Count all references in format
                Span<byte> tmpTaskData = stackalloc byte[varFormat.MinSize];
                varFormat.Serialize (default, tmpTaskData, (in object obj, Span<byte> destination) => { ReferenceCount++; return 0; } );

                IsManaged = true;
            }
        }

        public override int UnmanagedSize => taskFormat.MinSize;

        public override bool Disposable => false;

        public void Serialize (in T task, Span<byte> destination, TaskReferenceStore references)
        {
            Debug.Assert (destination.Length >= UnmanagedSize, "destination.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Serialize (task, destination, references.Write);
        }

        public void Deserialize (out T task, ReadOnlySpan<byte> source, TaskReferenceStore references)
        {
            Debug.Assert (source.Length >= UnmanagedSize, "source.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Deserialize (out task, source, references.Read);
        }

        public override void RunAndDispose (ref TaskQueue.TaskDataAccess access)
        {
            T data = access.GetTaskData (this);
            access.Dispose ();

            data.Run ();
        }
    }

    internal class DisposableTaskInfo<T> : TaskInfo<T> where T : struct, ITask, IDisposable
    {
        public DisposableTaskInfo (int id)
            : base (id) { }

        public override bool Disposable => true;

        public override void RunAndDispose (ref TaskQueue.TaskDataAccess access)
        {
            T data = access.GetTaskData<T> (this);
            access.Dispose ();

            try
            {
                data.Run ();
            }
            finally
            {
                data.Dispose ();
            }
        }

        public override void Dispose (in TaskQueue.TaskDataAccess access)
        {
            T data = access.GetTaskData<T> (this);

            data.Dispose ();
        }
    }
}
