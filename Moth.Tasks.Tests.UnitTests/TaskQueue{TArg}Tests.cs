namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [TestFixture]
    public class TaskQueueWithArgTests
    {
        private Mock<ITaskMetadataCache> mockTaskCache;
        private Mock<ITaskDataStore> mockTaskDataStore;
        private Mock<ITaskHandleManager> mockTaskHandleManager;
        private MockTaskMetadata<TestTask, int> mockTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<TestTask, int>, int, Unit>, int, Unit> mockTestTaskWithHandle;
        private MockTaskMetadata<DisposableTestTask, int> mockDisposableTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<DisposableTestTask, int>, int, Unit>, int, Unit> mockDisposableTestTaskWithHandle;
        private Mock<IProfiler> mockProfiler;

        [SetUp]
        public void SetUp ()
        {
            mockTaskCache = new Mock<ITaskMetadataCache> (MockBehavior.Strict);
            mockTaskDataStore = new Mock<ITaskDataStore> (MockBehavior.Strict);
            mockTaskDataStore.Setup (store => store.Skip (It.IsAny<ITaskMetadata> ()));
            mockTaskDataStore.Setup (store => store.Clear ());

            mockTaskHandleManager = new Mock<ITaskHandleManager> (MockBehavior.Strict);
            mockTaskHandleManager.Setup (manager => manager.Clear ());

            mockProfiler = new Mock<IProfiler> (MockBehavior.Loose);

            int nextTaskMetadataID = 0;

            mockTestTaskMetadata = new MockTaskMetadata<TestTask, int> (nextTaskMetadataID++);
            SetupTaskMetadata (mockTestTaskMetadata);

            mockDisposableTestTaskMetadata = new MockTaskMetadata<DisposableTestTask, int> (nextTaskMetadataID++);
            SetupTaskMetadata (mockDisposableTestTaskMetadata);

            mockTestTaskWithHandle = new MockTaskMetadata<TaskWithHandle<TaskWrapper<TestTask, int>, int, Unit>, int, Unit> (nextTaskMetadataID++);
            SetupTaskMetadata (mockTestTaskWithHandle);

            mockDisposableTestTaskWithHandle = new MockTaskMetadata<TaskWithHandle<TaskWrapper<DisposableTestTask, int>, int, Unit>, int, Unit> (nextTaskMetadataID++);
            SetupTaskMetadata (mockDisposableTestTaskWithHandle);

            void SetupTaskMetadata<TTask> (ITaskMetadata<TTask> taskInfo)
                where TTask : struct, ITaskType
            {
                mockTaskCache.Setup (t => t.GetTask<TTask> ()).Returns (taskInfo);
                mockTaskCache.Setup (t => t.GetTask (taskInfo.ID)).Returns (taskInfo);

                mockTaskDataStore.Setup (store => store.Enqueue (It.Ref<TTask>.IsAny, taskInfo));
            }
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            queue.Enqueue (task);

            // Verify that the correct ITaskMetadata was retrieved from the task cache
            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            // Verify that the task was stored in the task data store
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_DisposableTaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Once);
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskMetadata), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenNotEmpty_GetsTaskMetadataFromTaskCacheAndStoresTaskDataInTaskDataStore ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (2));
        }

        [Test]
        public unsafe void Enqueue_DisposableTaskEnqueuedWhenNotEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (2));
        }

        [Test]
        public void Enqueue_TaskWithHandle_EnqueuesTask ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            mockTaskHandleManager.Setup (x => x.CreateTaskHandle ()).Returns (default (TaskHandle));

            TestTask task = new TestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public void Enqueue_TaskWithHandle_ReturnsCorrectHandle ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            int handleID = 42;
            mockTaskHandleManager.Setup (x => x.CreateTaskHandle ()).Returns (new TaskHandle (mockTaskHandleManager.Object, handleID));

            TestTask task = new TestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.Multiple (() =>
            {
                Assert.That (handle.Manager, Is.SameAs (mockTaskHandleManager.Object));
                Assert.That (handle.ID, Is.EqualTo (handleID));
            });
        }

        [Test]
        public void Enqueue_DisposableTaskWithHandle_ReturnsCorrectHandle ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            int handleID = 42;
            mockTaskHandleManager.Setup (x => x.CreateTaskHandle ()).Returns (new TaskHandle (mockTaskHandleManager.Object, handleID));

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.Multiple (() =>
            {
                Assert.That (handle.Manager, Is.SameAs (mockTaskHandleManager.Object));
                Assert.That (handle.ID, Is.EqualTo (handleID));
            });
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_OneTaskEnqueuedAndMethodCalled_RetrievesFromTaskDataStoreAndRunsTask (CancellationToken token)
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            // Verify that the correct ITaskMetadata was retrieved from the task cache
            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            // Verify that the task was stored & retrieved from the task data store
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Once);

            Assert.That (mockTestTaskMetadata.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TwoTasksEnqueuedAndMethodCalledWithNoArgs_RetrievesFirstFromTaskDataStoreAndRunsFirstTaskWithDefaultArg (CancellationToken token)
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.SetupSequence (store => store.Dequeue (mockTestTaskMetadata)).Returns (task).Returns (task);

            queue.Enqueue (task);
            queue.Enqueue (task);

            queue.RunNextTask (token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Exactly (2));

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Exactly (2));

            // Assert that only one task was run
            Assert.That (mockTestTaskMetadata.SuppliedArgs, Is.EqualTo (new int[] { default }));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TaskThrowsException_RunsTaskAndCatchesException (CancellationToken token)
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask ();

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            mockTestTaskMetadata.ExceptionToThrow = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (out Exception exception, token: token);

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Once);

            Assert.That (mockTestTask.RunCallCount, Is.EqualTo (1));

            // Verify that the exception was caught and returned
            Assert.That (exception, Is.SameAs (mockTestTaskMetadata.ExceptionToThrow));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfiler_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (profiler: mockProfiler.Object, token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTask).FullName), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfilerAndTaskThrowsException_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            mockTestTaskMetadata.ExceptionToThrow = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (profiler: mockProfiler.Object, token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTask).FullName), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsEmpty_ReturnsFalse ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            bool result = queue.TryRunNextTask ();

            Assert.That (result, Is.False);
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsNotEmpty_ReturnsTrue ()
        {
            TaskQueue<int> queue = new TaskQueue<int> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);

            bool result = queue.TryRunNextTask ();

            Assert.That (result, Is.True);
        }

        private unsafe struct TestTask : ITask<int>
        {
            public void Run (int arg) { }
        }

        private unsafe struct DisposableTestTask : ITask<int>, IDisposable
        {
            public void Run (int arg) { }

            public void Dispose () { }
        }
    }
}
