namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Representation of a task in a <see cref="TaskCache"/>.
    /// </summary>
    internal abstract class TaskInfo
    {
        private TaskInfo (int id, Type type, int dataSize)
        {
            ID = id;
            Type = type;

            DataSize = dataSize;

            // Round dataSize to number of IntPtr.Size data indices required to hold task data in a TaskQueue.
            DataIndices = (dataSize + IntPtr.Size - 1) / IntPtr.Size;
        }

        /// <summary>
        /// ID of task.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Type of task.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Size of task data in bytes.
        /// </summary>
        public int DataSize { get; }

        /// <summary>
        /// Size of task in indices of <see cref="TaskQueue.taskData"/>.
        /// </summary>
        public int DataIndices { get; }

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
        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            Type type = typeof (T);

            if (typeof (IDisposable).IsAssignableFrom (type)) // If T implements IDisposable
            {
                Type disposableImplementationOfT = typeof (DisposableImplementation<>).MakeGenericType (type);

                return (TaskInfo)Activator.CreateInstance (disposableImplementationOfT, id);
            } else
            {
                return new Implementation<T> (id);
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

        private class Implementation<T> : TaskInfo where T : struct, ITask
        {
            public Implementation (int id)
                : base (id, typeof (T), Unsafe.SizeOf<T> ()) { }

            public override bool Disposable => false;

            public override void RunAndDispose (ref TaskQueue.TaskDataAccess access)
            {
                T data = access.GetTaskData<T> (this);
                access.Dispose ();

                data.Run ();
            }
        }

        private class DisposableImplementation<T> : TaskInfo where T : struct, ITask, IDisposable
        {
            public DisposableImplementation (int id)
                : base (id, typeof (T), Unsafe.SizeOf<T> ()) { }

            public override bool Disposable => true;

            public override void RunAndDispose (ref TaskQueue.TaskDataAccess access)
            {
                T data = access.GetTaskData<T> (this);
                access.Dispose ();

                try
                {
                    data.Run ();
                } finally
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
}
