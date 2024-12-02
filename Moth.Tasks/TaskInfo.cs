namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Diagnostics;
    using System.Linq;

    public static class TaskInfo
    {
        public static readonly TaskInfoProvider Provider = new TaskInfoProvider (Format.Provider);

        /// <summary>
        /// Creates a new TaskInfo from a type and ID.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new TaskInfo representing the task <typeparamref name="T"/>.</returns>
        internal static unsafe ITaskInfo<TTask> Create<TTask> (int id)
             where TTask : struct, ITaskType
            => Provider.Create<TTask> (id);
    }

    public interface ITaskInfo
    {
        int ID { get; }

        Type Type { get; }

        int UnmanagedSize { get; }

        int ReferenceCount { get; }

        bool IsManaged { get; }

        bool IsDisposable { get; }

        bool HasArgs { get; }

        bool HasResult { get; }
    }

    public interface IRunnableTaskInfo : ITaskInfo
    {
        void Run (TaskQueue.TaskDataAccess access);
    }

    public interface IRunnableTaskInfo<TArg> : IRunnableTaskInfo
    {
        void Run (TaskQueue.TaskDataAccess access, TArg arg);
    }

    public interface IRunnableTaskInfo<TArg, TResult> : IRunnableTaskInfo<TArg>
    {
        TResult Run (TaskQueue.TaskDataAccess access, TArg arg);
    }

    public interface ITaskInfo<TTask> : ITaskInfo
    {
        void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter);

        void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader);
    }

    /// <summary>
    /// Representation of a task in a <see cref="TaskCache"/>.
    /// </summary>
    public abstract class TaskInfoBase<TTask> : ITaskInfo<TTask>
        where TTask : struct, ITaskType
    {
        private readonly IFormat<TTask> taskFormat;

        protected TaskInfoBase (int id, IFormat<TTask> taskFormat)
        {
            ID = id;
            Type = typeof (TTask);

            this.taskFormat = taskFormat;

            if (taskFormat is IVariableFormat<TTask> varFormat)
            {
                // Count all references in format
                Span<byte> tmpTaskData = stackalloc byte[varFormat.MinSize];
                varFormat.Serialize (default, tmpTaskData, (in object obj, Span<byte> destination) => { ReferenceCount++; return 0; });

                IsManaged = true;
            } else
            {
                IsManaged = false;
            }
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
        public int UnmanagedSize => taskFormat.MinSize;

        /// <summary>
        /// Gets the number of reference fields in the task.
        /// </summary>
        public int ReferenceCount { get; set; }

        /// <summary>
        /// Gets whether the task contains reference types.
        /// </summary>
        public bool IsManaged { get; set; }

        public abstract bool IsDisposable { get; }

        public abstract bool HasArgs { get; }

        public abstract bool HasResult { get; }

        public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
        {
            Debug.Assert (destination.Length >= UnmanagedSize, "destination.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Serialize (task, destination, refWriter);
        }

        public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
        {
            Debug.Assert (source.Length >= UnmanagedSize, "source.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Deserialize (out task, source, refReader);
        }
    }

    internal class TaskInfo<TTask> : TaskInfoBase<TTask>, IRunnableTaskInfo
        where TTask : struct, ITask
    {
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => false;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Run ();
    }

    internal class TaskInfo<TTask, TArg> : TaskInfoBase<TTask>, IRunnableTaskInfo<TArg>
        where TTask : struct, ITask<TArg>
    {
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => true;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access) => Run (default);

        public void Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);
    }

    internal class TaskInfo<TTask, TArg, TResult> : TaskInfoBase<TTask>, IRunnableTaskInfo<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>
    {
        public TaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => true;

        public override bool HasResult => true;

        public void Run (TaskQueue.TaskDataAccess access) => Run (default);

        public void Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);

        TResult IRunnableTaskInfo<TArg, TResult>.Run (TaskQueue.TaskDataAccess access, TArg arg) => access.GetNextTaskData (this).Run (arg);
    }

    public interface IDisposableTaskInfo
    {
        void Dispose (TaskQueue.TaskDataAccess access);
    }

    internal abstract class DisposableTaskInfoBase<TTask> : TaskInfoBase<TTask>, IDisposableTaskInfo
        where TTask : struct, ITaskType, IDisposable
    {
        protected DisposableTaskInfoBase (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool IsDisposable => true;

        public void Dispose (TaskQueue.TaskDataAccess access) => access.GetNextTaskData (this).Dispose ();
    }

    internal class DisposableTaskInfo<TTask> : DisposableTaskInfoBase<TTask>, IRunnableTaskInfo
        where TTask : struct, ITask, IDisposable
    {
        public DisposableTaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool HasArgs => false;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run ();
            }
            finally
            {
                data.Dispose ();
            }
        }
    }

    internal class DisposableTaskInfo<TTask, TArg> : DisposableTaskInfoBase<TTask>, IRunnableTaskInfo<TArg>
        where TTask : struct, ITask<TArg>, IDisposable
    {
        public DisposableTaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool HasArgs => true;

        public override bool HasResult => false;

        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (default);
            } finally
            {
                data.Dispose ();
            }
        }

        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }
    }

    internal class DisposableTaskInfo<TTask, TArg, TResult> : DisposableTaskInfoBase<TTask>, IRunnableTaskInfo<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>, IDisposable
    {
        public DisposableTaskInfo (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        public override bool HasArgs => true;

        public override bool HasResult => true;

        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (default);
            } finally
            {
                data.Dispose ();
            }
        }

        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }

        TResult IRunnableTaskInfo<TArg, TResult>.Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                return data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }
    }
}
