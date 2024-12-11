using Moth.IO.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Tasks.Tests.UnitTests
{
    public unsafe abstract class MockTaskMetadataBase<TTask> : ITaskMetadata<TTask>
            where TTask : struct, ITaskType
    {
        public MockTaskMetadataBase (int id)
        {
            ID = id;
            IsDisposable = typeof (IDisposable).IsAssignableFrom (typeof (TTask));
        }

        public int ID { get; }

        public Type Type => typeof (TTask);

        public int UnmanagedSize => sizeof (TTask);

        public int ReferenceCount => 0;

        public bool IsManaged => false;

        public bool HasArgs => false;

        public bool HasResult => false;

        public bool IsDisposable { get; }

        public int DisposeCallCount { get; private set; }

        public Exception ExceptionToThrow { get; set; }

        public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
        {

        }

        public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
        {
            task = default;
        }

        public void Dispose (TaskQueue.TaskDataAccess access) => DisposeCallCount++;
    }

    public unsafe class MockTaskMetadata<TTask> : MockTaskMetadataBase<TTask>, IRunnableTaskMetadata
        where TTask : struct, ITask
    {
        public MockTaskMetadata (int id)
            : base (id) { }

        public int RunCallCount { get; private set; }

        public void Run (TaskQueue.TaskDataAccess access)
        {
            RunCallCount++;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            //access.GetNextTaskData (this).Run ();
        }
    }

    public unsafe class MockTaskMetadata<TTask, TArg> : MockTaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg>
        where TTask : struct, ITask<TArg>
    {
        public MockTaskMetadata (int id)
            : base (id) { }

        public List<TArg> SuppliedArgs { get; } = new List<TArg> ();

        public void Run (TaskQueue.TaskDataAccess access)
        {
            SuppliedArgs.Add (default);

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            //access.GetNextTaskData (this).Run (default);
        }

        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            SuppliedArgs.Add (arg);

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            //access.GetNextTaskData (this).Run (arg);
        }
    }

    public unsafe class MockTaskMetadata<TTask, TArg, TResult> : MockTaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>
    {
        public MockTaskMetadata (int id)
            : base (id) { }

        public List<TArg> SuppliedArgs { get; } = new List<TArg> ();

        public Queue<TResult> ResultsToReturn { get; } = new Queue<TResult> ();

        public void Run (TaskQueue.TaskDataAccess access)
        {
            SuppliedArgs.Add (default);

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            //access.GetNextTaskData (this).Run (default);
        }

        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            SuppliedArgs.Add (arg);

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            //access.GetNextTaskData (this).Run (arg);
        }

        public void Run (TaskQueue.TaskDataAccess access, TArg arg, out TResult result)
        {
            SuppliedArgs.Add (arg);

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            //result = access.GetNextTaskData (this).Run (arg);
            result = ResultsToReturn.Dequeue ();
        }
    }
}
