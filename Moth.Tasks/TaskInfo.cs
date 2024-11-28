namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Validation;

    public static class TaskInfo
    {
        /// <summary>
        /// Creates a new TaskInfo from a type and ID.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new TaskInfo representing the task <typeparamref name="T"/>.</returns>
        internal static unsafe ITaskInfo<TTask> Create<TTask> (int id)
             where TTask : struct, ITaskType
        {
            Type type = typeof (TTask);

            bool isDisposable = false;
            bool hasArgs = false;

            foreach (Type interfaceType in type.GetInterfaces ())
            {
                if (interfaceType == typeof (IDisposable))
                    isDisposable = true;
                else if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition () == typeof (ITask<>))
                    hasArgs = true;
            }

            Type taskInfoType;

            if (isDisposable)
            {
                if (hasArgs)
                    taskInfoType = typeof (DisposableTaskInfo<,>).MakeGenericType (type.GetGenericArguments ());
                else
                    taskInfoType = typeof (DisposableTaskInfo<>).MakeGenericType (type);
            } else
            {
                if (hasArgs)
                    taskInfoType = typeof (TaskInfo<,>).MakeGenericType (type.GetGenericArguments ());
                else
                    taskInfoType = typeof (TaskInfo<>).MakeGenericType (type);
            }

            return (ITaskInfo<TTask>)Activator.CreateInstance (taskInfoType, id);
        }
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
    }

    public interface ITaskInfoRunnable : ITaskInfo
    {
        void Run (ref TaskQueueBase.TaskDataAccess access);
    }

    public interface ITaskInfoRunnable<TArg> : ITaskInfo
    {
        void Run (ref TaskQueueBase.TaskDataAccess access, TArg arg);
    }

    public interface ITaskInfo<TTask> : ITaskInfo
    {
        void Serialize (in TTask task, Span<byte> destination, TaskReferenceStore references);

        void Deserialize (out TTask task, ReadOnlySpan<byte> source, TaskReferenceStore references);
    }

    /// <summary>
    /// Representation of a task in a <see cref="TaskCache"/>.
    /// </summary>
    public abstract class TaskInfoBase<TTask> : ITaskInfo<TTask>
        where TTask : struct, ITaskType
    {
        private Format<TTask> taskFormat;

        protected TaskInfoBase (int id)
        {
            ID = id;
            Type = typeof (TTask);

            taskFormat = (Format<TTask>)Formats.Get<TTask> ();

            if (taskFormat is VariableFormat<TTask> varFormat)
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

        public void Serialize (in TTask task, Span<byte> destination, TaskReferenceStore references)
        {
            Debug.Assert (destination.Length >= UnmanagedSize, "destination.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Serialize (task, destination, references.Write);
        }

        public void Deserialize (out TTask task, ReadOnlySpan<byte> source, TaskReferenceStore references)
        {
            Debug.Assert (source.Length >= UnmanagedSize, "source.Length was less than TaskInfo.UnmanagedSize");

            taskFormat.Deserialize (out task, source, references.Read);
        }
    }

    internal class TaskInfo<TTask> : TaskInfoBase<TTask>, ITaskInfoRunnable
        where TTask : struct, ITask
    {
        public TaskInfo (int id)
            : base (id) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => false;

        public void Run (ref TaskQueueBase.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);
            access.Dispose ();

            data.Run ();
        }
    }

    internal class TaskInfo<TTask, TArg> : TaskInfoBase<TTask>, ITaskInfoRunnable<TArg>
        where TTask : struct, ITask<TArg>
    {
        public TaskInfo (int id)
            : base (id) { }

        public override bool IsDisposable => false;

        public override bool HasArgs => true;

        public void Run (ref TaskQueueBase.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);
            access.Dispose ();

            data.Run (arg);
        }
    }

    internal interface IDisposableTaskInfo
    {
        void Dispose (in TaskQueueBase.TaskDataAccess access);
    }

    internal abstract class DisposableTaskInfoBase<TTask> : TaskInfoBase<TTask>, IDisposableTaskInfo
        where TTask : struct, ITaskType, IDisposable
    {
        protected DisposableTaskInfoBase (int id)
            : base (id) { }

        public override bool IsDisposable => true;

        public void Dispose (in TaskQueueBase.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            data.Dispose ();
        }
    }

    internal class DisposableTaskInfo<TTask> : DisposableTaskInfoBase<TTask>, ITaskInfoRunnable
        where TTask : struct, ITask, IDisposable
    {
        public DisposableTaskInfo (int id)
            : base (id) { }

        public override bool HasArgs => false;

        public void Run (ref TaskQueueBase.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);
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
    }

    internal class DisposableTaskInfo<TTask, TArg> : DisposableTaskInfoBase<TTask>, ITaskInfoRunnable<TArg>
        where TTask : struct, ITask<TArg>, IDisposable
    {
        public DisposableTaskInfo (int id)
            : base (id) { }

        public override bool HasArgs => true;

        public void Run (ref TaskQueueBase.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);
            access.Dispose ();

            try
            {
                data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }
    }
}
