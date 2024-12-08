namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;


    [TestFixture]
    internal class TaskDataStoreTests
    {
        private ITaskMetadata<TestTask> mockTaskMetadata;
        private Mock<ITaskReferenceStore> mockReferenceStore;

        [SetUp]
        public void SetUp ()
        {
            mockTaskMetadata = new MockTestTaskMetadata ();

            mockReferenceStore = new Mock<ITaskReferenceStore> ();
        }

        [Test]
        public void Constructor_WithDataCapacity_InitializesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0), "FirstTask has correct value");
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (0));
                Assert.That (taskData.Size, Is.EqualTo (0));
                Assert.That (taskData.Capacity, Is.EqualTo (0));
            });
        }

        [Test]
        public unsafe void Enqueue_WhenOutOfCapacity_IncreasesCapacity ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            mockReferenceStore.Verify (store => store.Write, Times.Once);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
                Assert.That (taskData.Capacity, Is.GreaterThanOrEqualTo (mockTaskMetadata.UnmanagedSize));
            });
        }

        [Test]
        public unsafe void Enqueue_WhenEmpty_EnqueuesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            mockReferenceStore.Verify (store => store.Write, Times.Once);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
            });
        }

        [Test]
        public void Enqueue_WhenNotEmpty_EnqueuesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Enqueue (new TestTask (), mockTaskMetadata);
            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            mockReferenceStore.Verify (store => store.Write, Times.Exactly (2));

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
            });
        }

        [Test]
        public void Dequeue_WhenNotEmpty_ReturnsCorrectTask ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            
            TestTask task = new TestTask (42);
            mockTaskMetadata = new MockTestTaskMetadata (task);

            taskData.Enqueue (task, mockTaskMetadata);
            TestTask dequeuedTask = taskData.Dequeue (mockTaskMetadata);

            mockReferenceStore.Verify (store => store.Read, Times.Once);

            Assert.That (dequeuedTask.Data, Is.EqualTo (task.Data));
        }

        [Test]
        public void Dequeue_WhenEmpty_ThrowsInvalidOperationException ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            Assert.Throws<InvalidOperationException> (() => taskData.Dequeue (new MockTestTaskMetadata ()));
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeAndNotEmptyAfter_IncrementsFirstTaskAndLastTaskEndIsUnchangedAndSizeIsDecremented ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            TestTask task = new TestTask (42);

            taskData.Enqueue (task, mockTaskMetadata);
            taskData.Enqueue (task, mockTaskMetadata);
            taskData.Skip (mockTaskMetadata);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
            });
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeButEmptyAfter_ResetsFirstTaskAndLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            TestTask task = new TestTask (42);

            taskData.Enqueue (task, mockTaskMetadata);
            taskData.Skip (mockTaskMetadata);

            Assert.That (taskData.FirstTask, Is.EqualTo (0));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (0));
            Assert.That (taskData.Size, Is.EqualTo (0));
        }

        [Test]
        public void Skip_WhenEmpty_ThrowsInvalidOperationException ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            Assert.Throws<InvalidOperationException> (() => taskData.Skip (new MockTestTaskMetadata ()));
        }

        [Test]
        public void Insert_WhenEmpty_ActsAsEnqueue ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;

            int dataIndex = 0;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskMetadata);

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize));
        }

        [Test]
        public void Insert_InFront_FirstTaskIsUnchangedAndIncrementsLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;

            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            int dataIndex = 0;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskMetadata);

            mockReferenceStore.Verify (store => store.EnterInsertContext (ref It.Ref<int>.IsAny, mockTaskMetadata.ReferenceCount, out It.Ref<ObjectWriter>.IsAny));

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
        }

        [Test]
        public void Insert_InEnd_ActsAsEnqueue ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;
            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            int dataIndex = mockTaskMetadata.UnmanagedSize;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskMetadata);

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 2));
        }

        [Test]
        public void Insert_InBetween_FirstTaskIsUnchangedAndIncrementsLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;
            taskData.Enqueue (new TestTask (), mockTaskMetadata);
            taskData.Enqueue (new TestTask (), mockTaskMetadata);

            int dataIndex = mockTaskMetadata.UnmanagedSize;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskMetadata);

            mockReferenceStore.Verify (store => store.EnterInsertContext (ref It.Ref<int>.IsAny, mockTaskMetadata.ReferenceCount, out It.Ref<ObjectWriter>.IsAny));

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 3));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskMetadata.UnmanagedSize * 3));
        }

        [Test]
        public void Clear_WhenNotEmpty_ResetsCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            TestTask task = new TestTask ();

            taskData.Enqueue (task, mockTaskMetadata);
            taskData.Clear ();

            Assert.That (taskData.FirstTask, Is.EqualTo (0));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (0));
        }

        [Test]
        public void Clear_WhenEmpty_DoesNothing ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Clear ();

            Assert.That (taskData.FirstTask, Is.EqualTo (0));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (0));
        }

        /// <summary>
        /// Test task
        /// </summary>
        struct TestTask : ITask
        {
            public int Data;

            public TestTask (int data)
            {
                Data = data;
            }

            public void Run ()
            {

            }
        }

        class MockTestTaskMetadata : MockTaskMetadata<TestTask>
        {
            public MockTestTaskMetadata (TestTask deserializeValue = default)
                : base (deserializeValue) { }
        }

        unsafe class MockTaskMetadata<TTask> : ITaskMetadata<TTask>
            where TTask : struct, ITask
        {
            public MockTaskMetadata (TTask deserializeValue = default)
            {
                DeserializeValue = deserializeValue;
            }

            public int ID => 0;

            public Type Type => typeof (TTask);

            public int UnmanagedSize => sizeof (TTask);

            public int ReferenceCount => 0;

            public bool IsManaged => false;

            public bool IsDisposable => throw new NotImplementedException ();

            public bool HasArgs => throw new NotImplementedException ();

            public bool HasResult => throw new NotImplementedException ();

            public TTask DeserializeValue { get; }

            public int SerializeCallCount { get; private set; }

            public int DeserializeCallCount { get; private set; }

            public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
            {
                SerializeCallCount++;
            }

            public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
            {
                DeserializeCallCount++;
                task = DeserializeValue;
            }

            public void Dispose (TaskQueue.TaskDataAccess access)
            {
                throw new NotImplementedException ();
            }
        }
    }
}
