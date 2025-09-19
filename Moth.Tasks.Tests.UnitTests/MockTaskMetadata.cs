using Moth.IO.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Tasks.Tests.UnitTests
{
    public unsafe abstract class MockTaskMetadataBase<TTask> : ITaskMetadata<TTask>
            where TTask : struct, ITask
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

        public Exception ExceptionToThrowOnRun { get; set; }

        public Exception ExceptionToThrowOnDispose { get; set; }

        public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
        {

        }

        public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
        {
            task = default;
        }

        public void Dispose (TaskDataAccess access)
        {
            DisposeCallCount++;

            if (ExceptionToThrowOnDispose != null)
                throw ExceptionToThrowOnDispose;
        }
    }

    public unsafe class MockTaskMetadata<TTask, TArg, TResult> : MockTaskMetadataBase<TTask>, ITaskMetadata<TArg, TResult>
        where TTask : struct, ITask<TArg, TResult>
    {
        public MockTaskMetadata (int id)
            : base (id) { }

        public List<TArg> SuppliedArgs { get; } = new List<TArg> ();

        public Queue<TResult> ResultsToReturn { get; } = new Queue<TResult> ();

        public bool ReturnDefaultWhenNoResults { get; set; } = true;

        public void Run (TaskDataAccess access)
        {
            SuppliedArgs.Add (default);

            if (ExceptionToThrowOnRun != null)
                throw ExceptionToThrowOnRun;
        }

        public void Run (TaskDataAccess access, TArg arg)
        {
            SuppliedArgs.Add (arg);

            if (ExceptionToThrowOnRun != null)
                throw ExceptionToThrowOnRun;
        }

        public void Run (TaskDataAccess access, TArg arg, out TResult result)
        {
            SuppliedArgs.Add (arg);

            if (ExceptionToThrowOnRun != null)
                throw ExceptionToThrowOnRun;

            if (!ResultsToReturn.TryDequeue (out result))
            {
                if (ReturnDefaultWhenNoResults)
                    result = default;
                else
                    throw new InvalidOperationException ("No results to return.");
            }
        }
    }
}
