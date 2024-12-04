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
        private ITaskInfo<TestTask> mockTaskInfo;
        private Mock<ITaskReferenceStore> mockReferenceStore;

        [SetUp]
        public void SetUp ()
        {
            mockTaskInfo = new MockTestTaskInfo ();

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

            taskData.Enqueue (new TestTask (), mockTaskInfo);

            mockReferenceStore.Verify (store => store.Write, Times.Once);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize));
                Assert.That (taskData.Capacity, Is.GreaterThanOrEqualTo (mockTaskInfo.UnmanagedSize));
            });
        }

        [Test]
        public unsafe void Enqueue_WhenEmpty_EnqueuesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Enqueue (new TestTask (), mockTaskInfo);

            mockReferenceStore.Verify (store => store.Write, Times.Once);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize));
            });
        }

        [Test]
        public void Enqueue_WhenNotEmpty_EnqueuesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            taskData.Enqueue (new TestTask (), mockTaskInfo);
            taskData.Enqueue (new TestTask (), mockTaskInfo);

            mockReferenceStore.Verify (store => store.Write, Times.Exactly (2));

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (0));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
            });
        }

        [Test]
        public void Dequeue_WhenNotEmpty_ReturnsCorrectTask ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            
            TestTask task = new TestTask (42);
            mockTaskInfo = new MockTestTaskInfo (task);

            taskData.Enqueue (task, mockTaskInfo);
            TestTask dequeuedTask = taskData.Dequeue (mockTaskInfo);

            mockReferenceStore.Verify (store => store.Read, Times.Once);

            Assert.That (dequeuedTask.Data, Is.EqualTo (task.Data));
        }

        [Test]
        public void Dequeue_WhenEmpty_ThrowsInvalidOperationException ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            Assert.Throws<InvalidOperationException> (() => taskData.Dequeue (new MockTestTaskInfo ()));
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeAndNotEmptyAfter_IncrementsFirstTaskAndLastTaskEndIsUnchangedAndSizeIsDecremented ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            TestTask task = new TestTask (42);

            taskData.Enqueue (task, mockTaskInfo);
            taskData.Enqueue (task, mockTaskInfo);
            taskData.Skip (mockTaskInfo);

            Assert.Multiple (() =>
            {
                Assert.That (taskData.FirstTask, Is.EqualTo (mockTaskInfo.UnmanagedSize));
                Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
                Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize));
            });
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeButEmptyAfter_ResetsFirstTaskAndLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            TestTask task = new TestTask (42);

            taskData.Enqueue (task, mockTaskInfo);
            taskData.Skip (mockTaskInfo);

            Assert.That (taskData.FirstTask, Is.EqualTo (0));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (0));
            Assert.That (taskData.Size, Is.EqualTo (0));
        }

        [Test]
        public void Skip_WhenEmpty_ThrowsInvalidOperationException ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);
            Assert.Throws<InvalidOperationException> (() => taskData.Skip (new MockTestTaskInfo ()));
        }

        [Test]
        public void Insert_WhenEmpty_ActsAsEnqueue ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;

            int dataIndex = 0;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskInfo);

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize));
        }

        [Test]
        public void Insert_InFront_FirstTaskIsUnchangedAndIncrementsLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;

            taskData.Enqueue (new TestTask (), mockTaskInfo);

            int dataIndex = 0;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskInfo);

            mockReferenceStore.Verify (store => store.EnterInsertContext (ref It.Ref<int>.IsAny, mockTaskInfo.ReferenceCount, out It.Ref<ObjectWriter>.IsAny));

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
        }

        [Test]
        public void Insert_InEnd_ActsAsEnqueue ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;
            taskData.Enqueue (new TestTask (), mockTaskInfo);

            int dataIndex = mockTaskInfo.UnmanagedSize;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskInfo);

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize * 2));
        }

        [Test]
        public void Insert_InBetween_FirstTaskIsUnchangedAndIncrementsLastTaskEndAndSize ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            int firstTask = taskData.FirstTask;
            taskData.Enqueue (new TestTask (), mockTaskInfo);
            taskData.Enqueue (new TestTask (), mockTaskInfo);

            int dataIndex = mockTaskInfo.UnmanagedSize;
            int refIndex = 0;
            taskData.Insert (ref dataIndex, ref refIndex, new TestTask (), mockTaskInfo);

            mockReferenceStore.Verify (store => store.EnterInsertContext (ref It.Ref<int>.IsAny, mockTaskInfo.ReferenceCount, out It.Ref<ObjectWriter>.IsAny));

            Assert.That (taskData.FirstTask, Is.EqualTo (firstTask));
            Assert.That (taskData.LastTaskEnd, Is.EqualTo (mockTaskInfo.UnmanagedSize * 3));
            Assert.That (taskData.Size, Is.EqualTo (mockTaskInfo.UnmanagedSize * 3));
        }

        [Test]
        public void Clear_WhenNotEmpty_ResetsCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (0, mockReferenceStore.Object);

            TestTask task = new TestTask ();

            taskData.Enqueue (task, mockTaskInfo);
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

        class MockTestTaskInfo : MockTaskInfo<TestTask>
        {
            public MockTestTaskInfo (TestTask deserializeValue = default)
                : base (deserializeValue) { }
        }

        unsafe class MockTaskInfo<TTask> : ITaskInfo<TTask>
            where TTask : struct, ITask
        {
            public MockTaskInfo (TTask deserializeValue = default)
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
        }
    }
}
