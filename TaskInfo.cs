namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Representation of a task in a <see cref="TaskCache"/>.
    /// </summary>
    internal class TaskInfo
    {
        private readonly RunOperation run;
        private readonly DisposeOperation dispose;

        private TaskInfo (RunOperation run, DisposeOperation dispose, int id, Type type, int dataSize)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Type = type;

            DataSize = dataSize;

            // Round dataSize to number of IntPtr.Size data indices required to hold task data in a TaskQueue.
            DataIndices = (dataSize + IntPtr.Size - 1) / IntPtr.Size;

            Disposable = dispose != null;
        }

        private delegate void RunOperation (ref TaskQueue.TaskDataAccess access, TaskInfo task);

        private delegate void DisposeOperation (in TaskQueue.TaskDataAccess access, TaskInfo task);

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
        public bool Disposable { get; }

        /// <summary>
        /// Creates a new TaskInfo from a type and ID.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new TaskInfo representing the task <typeparamref name="T"/>.</returns>
        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            Type type = typeof (T);

            RunOperation run = null;
            DisposeOperation dispose = null;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                BindingFlags methodBindingFlags = BindingFlags.Static | BindingFlags.NonPublic;

                run = (RunOperation)typeof (TaskInfo).GetMethod (nameof (RunAndDispose), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (RunOperation));

                dispose = (DisposeOperation)typeof (TaskInfo).GetMethod (nameof (Dispose), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (DisposeOperation));
            } else
            {
                run = Run;
            }

            return new TaskInfo (Run, dispose, id, type, Unsafe.SizeOf<T> ());

            // Run the task
            static void Run (ref TaskQueue.TaskDataAccess access, TaskInfo task)
            {
                T data = access.GetTaskData<T> (task);
                access.Dispose ();

                data.Run ();
            }
        }

        /// <summary>
        /// Call the <see cref="ITask.Run"/> method of the task, with <see cref="TaskQueue.TaskDataAccess"/> for getting task data.
        /// </summary>
        /// <param name="access">Access to data from <see cref="TaskQueue"/>.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref TaskQueue.TaskDataAccess access) => run (ref access, this);

        /// <summary>
        /// Call the <see cref="IDisposable.Dispose"/> method of the task, with <see cref="TaskQueue.TaskDataAccess"/> for getting task data.
        /// </summary>
        /// <param name="access">Access to data from <see cref="TaskQueue"/>.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (in TaskQueue.TaskDataAccess access) => dispose (in access, this);

        // Run and dispose the task
        private static void RunAndDispose<T> (ref TaskQueue.TaskDataAccess access, TaskInfo task) where T : struct, ITask, IDisposable
        {
            T data = access.GetTaskData<T> (task);
            access.Dispose ();

            try
            {
                data.Run ();
            } finally
            {
                data.Dispose ();
            }
        }

        // Dispose the task
        private static void Dispose<T> (in TaskQueue.TaskDataAccess access, TaskInfo task) where T : struct, ITask, IDisposable
        {
            T data = access.GetTaskData<T> (task);

            data.Dispose ();
        }
    }
}
