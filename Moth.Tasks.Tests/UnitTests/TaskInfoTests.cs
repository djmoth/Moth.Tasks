namespace Moth.Tasks.Tests.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Moq;
    using Moth.IO.Serialization;
    using Moth.Tasks;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    public class TaskInfoTests
    {
        [Test]
        public void Constructor_OfUnmanagedTestTask_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<TestTask<int>> (42);
            AssertTaskInfoProperties (new TaskInfo<TestTask<int>> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfManagedTestTask_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<TestTask<object>> (42, typeof (object));
            AssertTaskInfoProperties (new TaskInfo<TestTask<object>> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfUnmanagedTestTaskArg_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<TestTaskArg<int>> (42);
            AssertTaskInfoProperties (new TaskInfo<TestTaskArg<int>, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfManagedTestTaskArg_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<TestTaskArg<object>> (42, typeof (object));
            AssertTaskInfoProperties (new TaskInfo<TestTaskArg<object>, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfUnmanagedTestTaskArgResult_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<TestTaskArgResult<int>> (42);
            AssertTaskInfoProperties (new TaskInfo<TestTaskArgResult<int>, int, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfManagedTestTaskArgResult_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<TestTaskArgResult<object>> (42, typeof (object));
            AssertTaskInfoProperties (new TaskInfo<TestTaskArgResult<object>, int, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableUnmanagedTestTask_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<DisposableTestTask<int>> (42);
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTask<int>> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableManagedTestTask_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<DisposableTestTask<object>> (42, typeof (object));
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTask<object>> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableUnmanagedTestTaskArg_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<DisposableTestTaskArg<int>> (42);
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTaskArg<int>, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableManagedTestTaskArg_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<DisposableTestTaskArg<object>> (42, typeof (object));
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTaskArg<object>, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableUnmanagedTestTaskArgResult_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<DisposableTestTaskArgResult<int>> (42);
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTaskArgResult<int>, int, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Constructor_OfDisposableManagedTestTaskArgResult_InitializesCorrectly ()
        {
            int taskID = 1;
            var taskFormat = new MockVariableFormat<DisposableTestTaskArgResult<object>> (42, typeof (object));
            AssertTaskInfoProperties (new DisposableTaskInfo<DisposableTestTaskArgResult<object>, int, int> (taskID, taskFormat), taskID, taskFormat);
        }

        [Test]
        public void Serialize_WhenCalled_CallsFormatSerialize ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<TestTask<int>> (42);
            var taskInfo = new TaskInfo<TestTask<int>> (taskID, taskFormat);

            Span<byte> destination = new byte[taskFormat.MinSize];
            taskInfo.Serialize (default, destination, Mock.Of<ObjectWriter> ());

            Assert.That (taskFormat.SerializeCallCount, Is.EqualTo (1));
        }

        [Test]
        public void Deserialize_WhenCalled_CallsFormatDeserialize ()
        {
            int taskID = 1;
            var taskFormat = new MockFixedFormat<TestTask<int>> (42);
            var taskInfo = new TaskInfo<TestTask<int>> (taskID, taskFormat);

            ReadOnlySpan<byte> source = new byte[taskFormat.MinSize];
            taskInfo.Deserialize (out _, source, Mock.Of<ObjectReader> ());

            Assert.That (taskFormat.DeserializeCallCount, Is.EqualTo (1));
        }


        void AssertTaskInfoProperties<T> (ITaskInfo taskInfo, int id, IFormat<T> taskFormat) where T : struct, ITaskType
        {
            Assert.That (taskInfo.ID, Is.EqualTo (id));
            Assert.That (taskInfo.Type, Is.EqualTo (typeof (T)));
            Assert.That (taskInfo.UnmanagedSize, Is.EqualTo (taskFormat.MinSize));

            if (taskFormat is MockFixedFormat<T>)
                Assert.That (taskInfo.ReferenceCount, Is.EqualTo (0));
            else if (taskFormat is MockVariableFormat<T> taskVarFormat)
                Assert.That (taskInfo.ReferenceCount, Is.EqualTo (taskVarFormat.ReferenceTypes.Length));
            else
                Assert.Fail ("Unknown format type");

            Assert.That (taskInfo.IsManaged, Is.EqualTo (taskFormat is IVariableFormat));
            Assert.That (taskInfo.IsDisposable, Is.EqualTo (taskInfo is IDisposableTaskInfo));

            bool shouldHaveResult = typeof (ITask<int, int>).IsAssignableFrom (typeof (T));
            bool shouldHaveArgs = typeof (ITask<int>).IsAssignableFrom (typeof (T)) || shouldHaveResult;

            Assert.That (taskInfo.HasArgs, Is.EqualTo (shouldHaveArgs));
            Assert.That (taskInfo.HasResult, Is.EqualTo (shouldHaveResult));
        }

        private class MockFixedFormat<TTask> : IFixedFormat<TTask>
        {
            public MockFixedFormat (int minSize)
            {
                MinSize = minSize;
            }

            public int MinSize { get; }

            public int SerializeCallCount { get; private set; }

            public int DeserializeCallCount { get; private set; }

            public int Size => MinSize;

            public bool Blittable => true;

            public int Serialize (in TTask value, Span<byte> destination, ObjectWriter writer)
            {
                SerializeCallCount++;

                return MinSize;
            }

            public int Deserialize (out TTask value, ReadOnlySpan<byte> source, ObjectReader reader)
            {
                DeserializeCallCount++;

                value = default;

                return MinSize;
            }

            public void Serialize (in TTask value, Span<byte> destination)
            {
                throw new NotImplementedException ();
            }

            public TTask Deserialize (ReadOnlySpan<byte> source)
            {
                throw new NotImplementedException ();
            }
        }

        public class MockVariableFormat<TTask> : IVariableFormat<TTask>
        {
            public MockVariableFormat (int minSize, params Type[] referenceTypes)
            {
                MinSize = minSize;
                ReferenceTypes = referenceTypes;
            }

            public int MinSize { get; }

            public Type[] ReferenceTypes { get; }

            public int SerializeCallCount { get; private set; }

            public int DeserializeCallCount { get; private set; }

            public int Serialize (in TTask value, Span<byte> destination, ObjectWriter writer)
            {
                SerializeCallCount++;

                for (int i = 0; i < ReferenceTypes.Length; i++)
                {
                    writer (null, destination);
                }

                return MinSize;
            }
            public int Deserialize (out TTask value, ReadOnlySpan<byte> source, ObjectReader reader)
            {
                DeserializeCallCount++;

                value = default;

                for (int i = 0; i < ReferenceTypes.Length; i++)
                {
                    reader (out _, ReferenceTypes[i], source);
                }

                return MinSize;
            }
        }

        public struct TestTask<TData> : ITask
        {
            public TData Data;

            public void Run () { }
        }

        public struct TestTaskArg<TData> : ITask<int>
        {
            public TData Data;

            public void Run (int i) { }
        }

        public struct TestTaskArgResult<TData> : ITask<int, int>
        {
            public TData Data;

            public int Run (int i) => i;
        }

        public struct DisposableTestTask<TData> : ITask, IDisposable
        {
            public TData Data;

            public void Dispose () { }

            public void Run () { }
        }

        public struct DisposableTestTaskArg<TData> : ITask<int>, IDisposable
        {
            public TData Data;

            public void Dispose () { }

            public void Run (int i) { }
        }

        public struct DisposableTestTaskArgResult<TData> : ITask<int, int>, IDisposable
        {
            public TData Data;

            public void Dispose () { }

            public int Run (int i) => i;
        }
    }
}
