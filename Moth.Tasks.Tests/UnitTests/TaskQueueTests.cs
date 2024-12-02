namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskQueueTests
    {
        private Mock<ITaskCache> mockTaskCache;
        private Mock<ITaskDataStore> mockTaskDataStore;
        private Mock<ITaskHandleManager> mockTaskHandleManager;
        private MockTaskInfo<TestTask> mockTestTaskInfo;
        private MockTaskInfo<TestTaskThrowingException> mockTestTaskThrowingExceptionInfo;
        private MockDisposableTaskInfo<DisposableTestTask> mockDisposableTestTaskInfo;
        private MockDisposableTaskInfo<DisposableTestTaskThrowingException> mockDisposableTestTaskThrowingExceptionInfo;
        private Mock<IProfiler> mockProfiler;
        

        [SetUp]
        public void SetUp ()
        {
            mockTaskCache = new Mock<ITaskCache> (MockBehavior.Strict);
            mockTaskDataStore = new Mock<ITaskDataStore> (MockBehavior.Strict);
            mockTaskDataStore.Setup (store => store.Skip (It.IsAny<ITaskInfo> ()));
            mockTaskDataStore.Setup (store => store.Clear ());

            int nextTaskInfoID = 0;

            mockTestTaskInfo = new MockTaskInfo<TestTask> (nextTaskInfoID++);
            SetupTaskInfo (mockTestTaskInfo);

            mockTestTaskThrowingExceptionInfo = new MockTaskInfo<TestTaskThrowingException> (nextTaskInfoID++);
            SetupTaskInfo (mockTestTaskThrowingExceptionInfo);

            mockDisposableTestTaskInfo = new MockDisposableTaskInfo<DisposableTestTask> (nextTaskInfoID++);
            SetupTaskInfo (mockDisposableTestTaskInfo);

            mockDisposableTestTaskThrowingExceptionInfo = new MockDisposableTaskInfo<DisposableTestTaskThrowingException> (nextTaskInfoID++);
            SetupTaskInfo (mockDisposableTestTaskThrowingExceptionInfo);

            mockTaskHandleManager = new Mock<ITaskHandleManager> (MockBehavior.Strict);
            mockTaskHandleManager.Setup (manager => manager.Clear ());

            mockProfiler = new Mock<IProfiler> (MockBehavior.Loose);

            void SetupTaskInfo<TTask> (ITaskInfo<TTask> taskInfo)
                where TTask : struct, ITask
            {
                mockTaskCache.Setup (t => t.GetTask<TTask> ()).Returns (taskInfo);
                mockTaskCache.Setup (t => t.GetTask (taskInfo.ID)).Returns (taskInfo);

                mockTaskDataStore.Setup (store => store.Enqueue (It.Ref<TTask>.IsAny, taskInfo));
            }
        }

        [Test]
        public void Count_WhenQueueIsEmpty_IsZero ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Count_WhenTaskEnqueued_Increments ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Enqueue (new TestTask ());

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            queue.Enqueue (task);

            // Verify that the correct ITaskInfo was retrieved from the task cache
            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            // Verify that the task was stored in the task data store
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskInfo), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_DisposableTaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Once);
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskInfo), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenNotEmpty_GetsTaskInfoFromTaskCacheAndStoresTaskDataInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskInfo), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (2));
        }

        [Test]
        public unsafe void Enqueue_DisposableTaskEnqueuedWhenNotEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskInfo), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (2));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_OneTaskEnqueuedAndMethodCalled_RetrievesFromTaskDataStoreAndRunsTask (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskInfo)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            // Verify that the correct ITaskInfo was retrieved from the task cache
            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            // Verify that the task was stored & retrieved from the task data store
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskInfo), Times.Once);
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskInfo), Times.Once);

            Assert.That (mockTestTaskInfo.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TwoTasksEnqueuedAndMethodCalled_RetrievesFirstFromTaskDataStoreAndRunsFirstTask (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.SetupSequence (store => store.Dequeue (mockTestTaskInfo)).Returns (task).Returns (task);

            queue.Enqueue (task);
            queue.Enqueue (task);

            queue.RunNextTask (token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Exactly (2));

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskInfo), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskInfo), Times.Once);

            // Assert that only one task was run
            Assert.That (mockTestTaskInfo.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TaskThrowsException_RunsTaskAndCatchesException (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            // Task will throw an exception with code 42
            TestTaskThrowingException task = new TestTaskThrowingException { ExceptionCode = 42 };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskThrowingExceptionInfo)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (out Exception exception, token: token);

            mockTaskCache.Verify (t => t.GetTask<TestTaskThrowingException> (), Times.Once);

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskThrowingExceptionInfo), Times.Once);
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskThrowingExceptionInfo), Times.Once);

            Assert.That (mockTestTaskThrowingExceptionInfo.RunCallCount, Is.EqualTo (1));

            // Verify that the exception was caught and returned
            Assert.That (exception, Is.Not.Null);
            Assert.That (exception.Message, Is.EqualTo (task.ExceptionCode.ToString ()));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfiler_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskInfo)).Returns (task);

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
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTaskThrowingException task = new TestTaskThrowingException { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskThrowingExceptionInfo)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (profiler: mockProfiler.Object, token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTaskThrowingException).FullName), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsEmpty_ReturnsFalse ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            bool result = queue.TryRunNextTask ();

            Assert.That (result, Is.False);
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsNotEmpty_ReturnsTrue ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskInfo)).Returns (task);

            queue.Enqueue (task);

            bool result = queue.TryRunNextTask ();

            Assert.That (result, Is.True);
        }

        [Test]
        public void Clear_WhenQueueIsEmpty_ClearsDependencies ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_NoDisposableTasks_ClearsQueueWhileSkippingTasks ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskInfo), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_WithOnlyDisposableTasks_ClearsQueueWhileDisposingTasks ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
            Assert.That (mockDisposableTestTaskInfo.DisposeCallCount, Is.EqualTo (2));
        }

        [Test]
        public void Clear_WithMixedTasks_ClearsQueueSkipsNonDisposableTasksAndDisposesDisposableTasks ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };
            DisposableTestTask disposableTask = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (disposableTask);
            queue.Enqueue (task);
            queue.Enqueue (disposableTask);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskInfo), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (0));
            Assert.That (mockDisposableTestTaskInfo.DisposeCallCount, Is.EqualTo (2));
        }

        [Test]
        public void Dispose_WhenCalledOnce_ClearsTasks ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Dispose_WhenCalledTwice_ClearsTasksThenDoesNothing ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Dispose ();
            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        private unsafe struct TestTask : ITask
        {
            public void Run () { }
        }

        private struct TestTaskThrowingException : ITask
        {
            public int ExceptionCode;

            public void Run () => throw new Exception (ExceptionCode.ToString ());
        }

        private unsafe struct DisposableTestTask : ITask, IDisposable
        {
            public void Run () { }

            public void Dispose () { }
        }

        private unsafe struct DisposableTestTaskThrowingException : ITask, IDisposable
        {
            public int ExceptionCode;

            public void Run () => throw new Exception (ExceptionCode.ToString ());

            public void Dispose () { }
        }

        private unsafe class MockTaskInfo<TTask> : ITaskInfo<TTask>, IRunnableTaskInfo
            where TTask : struct, ITask
        {
            public MockTaskInfo (int id) => ID = id;

            public int ID { get; }

            public Type Type => typeof (TTask);

            public int UnmanagedSize => sizeof (TTask);

            public int ReferenceCount => 0;

            public bool IsManaged => false;

            public virtual bool IsDisposable => false;

            public bool HasArgs => false;

            public bool HasResult => false;


            public int RunCallCount { get; private set; }

            public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
            {
                
            }

            public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
            {
                task = default;
            }

            public void Run (TaskQueue.TaskDataAccess access)
            {
                RunCallCount++;
                access.GetNextTaskData (this).Run ();
            }
        }

        private unsafe class MockDisposableTaskInfo<TTask> : MockTaskInfo<TTask>, IDisposableTaskInfo
            where TTask : struct, ITask, IDisposable
        {
            public MockDisposableTaskInfo (int id)
                : base (id) { }

            public override bool IsDisposable => true;

            public int DisposeCallCount { get; private set; }

            public void Dispose (TaskQueue.TaskDataAccess access)
            {
                DisposeCallCount++;
                access.GetNextTaskData (this).Dispose ();
            }
        }
    }
}
