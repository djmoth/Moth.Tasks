namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class TaskQueueTests
    {
        private Mock<ITaskMetadataCache> mockTaskCache;
        private Mock<ITaskDataStore> mockTaskDataStore;
        private Mock<ITaskHandleManager> mockTaskHandleManager;
        private MockTaskMetadata<TestTask> mockTestTaskMetadata;
        private MockTaskMetadata<TestTaskThrowingException> mockTestTaskThrowingExceptionMetadata;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<TestTask>, Unit, Unit>, Unit, Unit> mockTestTaskWithHandle;
        private MockTaskMetadata<DisposableTestTask> mockDisposableTestTaskMetadata;
        private MockTaskMetadata<DisposableTestTaskThrowingException> mockDisposableTestTaskThrowingExceptionInfo;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<DisposableTestTask>, Unit, Unit>, Unit, Unit> mockDisposableTestTaskWithHandle;
        private Mock<IProfiler> mockProfiler;
        

        [SetUp]
        public void SetUp ()
        {
            mockTaskCache = new Mock<ITaskMetadataCache> (MockBehavior.Strict);
            mockTaskDataStore = new Mock<ITaskDataStore> (MockBehavior.Strict);
            mockTaskDataStore.Setup (store => store.Skip (It.IsAny<ITaskMetadata> ()));
            mockTaskDataStore.Setup (store => store.Clear ());

            int nextTaskMetadataID = 0;

            mockTestTaskMetadata = new MockTaskMetadata<TestTask> (nextTaskMetadataID++);
            SetupTaskMetadata (mockTestTaskMetadata);

            mockTestTaskThrowingExceptionMetadata = new MockTaskMetadata<TestTaskThrowingException> (nextTaskMetadataID++);
            SetupTaskMetadata (mockTestTaskThrowingExceptionMetadata);

            mockDisposableTestTaskMetadata = new MockTaskMetadata<DisposableTestTask> (nextTaskMetadataID++);
            SetupTaskMetadata (mockDisposableTestTaskMetadata);

            mockDisposableTestTaskThrowingExceptionInfo = new MockTaskMetadata<DisposableTestTaskThrowingException> (nextTaskMetadataID++);
            SetupTaskMetadata (mockDisposableTestTaskThrowingExceptionInfo);

            mockTestTaskWithHandle = new MockTaskMetadata<TaskWithHandle<TaskWrapper<TestTask>, Unit, Unit>, Unit, Unit> (nextTaskMetadataID++);
            SetupTaskMetadata (mockTestTaskWithHandle);

            mockDisposableTestTaskWithHandle = new MockTaskMetadata<TaskWithHandle<TaskWrapper<DisposableTestTask>, Unit, Unit>, Unit, Unit> (nextTaskMetadataID++);
            SetupTaskMetadata (mockDisposableTestTaskWithHandle);

            mockTaskHandleManager = new Mock<ITaskHandleManager> (MockBehavior.Strict);
            mockTaskHandleManager.Setup (manager => manager.Clear ());

            mockProfiler = new Mock<IProfiler> (MockBehavior.Loose);

            void SetupTaskMetadata<TTask> (ITaskMetadata<TTask> taskInfo)
                where TTask : struct, ITaskType
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

            // Verify that the correct ITaskMetadata was retrieved from the task cache
            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Once);

            // Verify that the task was stored in the task data store
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_DisposableTaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Once);
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskMetadata), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenNotEmpty_GetsTaskMetadataFromTaskCacheAndStoresTaskDataInTaskDataStore ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (2));
        }

        [Test]
        public void Enqueue_TaskWithHandle_ReturnsCorrectHandle ()
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskMetadata), Times.Once);

            Assert.That (mockTestTaskMetadata.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TwoTasksEnqueuedAndMethodCalled_RetrievesFirstFromTaskDataStoreAndRunsFirstTask (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.SetupSequence (store => store.Dequeue (mockTestTaskMetadata)).Returns (task).Returns (task);

            queue.Enqueue (task);
            queue.Enqueue (task);

            queue.RunNextTask (token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockTaskCache.Verify (t => t.GetTask<TestTask> (), Times.Exactly (2));

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskMetadata), Times.Exactly (2));
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskMetadata), Times.Once);

            // Assert that only one task was run
            Assert.That (mockTestTaskMetadata.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TaskThrowsException_RunsTaskAndCatchesException (CancellationToken token)
        {
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            // Task will throw an exception with code 42
            TestTaskThrowingException task = new TestTaskThrowingException { ExceptionCode = 42 };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskThrowingExceptionMetadata)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (out Exception exception, token: token);

            mockTaskCache.Verify (t => t.GetTask<TestTaskThrowingException> (), Times.Once);

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockTestTaskThrowingExceptionMetadata), Times.Once);
            mockTaskDataStore.Verify (store => store.Dequeue (mockTestTaskThrowingExceptionMetadata), Times.Once);

            Assert.That (mockTestTaskThrowingExceptionMetadata.RunCallCount, Is.EqualTo (1));

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
            TaskQueue queue = new TaskQueue (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTaskThrowingException task = new TestTaskThrowingException { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskThrowingExceptionMetadata)).Returns (task);

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

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

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

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Exactly (2));

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
            Assert.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (2));
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

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (0));
            Assert.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (2));
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

        private unsafe abstract class MockTaskMetadataBase<TTask> : ITaskMetadata<TTask>
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

            public void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter)
            {

            }

            public void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader)
            {
                task = default;
            }

            public void Dispose (TaskQueue.TaskDataAccess access) => DisposeCallCount++;
        }

        private unsafe class MockTaskMetadata<TTask> : MockTaskMetadataBase<TTask>, IRunnableTaskMetadata
            where TTask : struct, ITask
        {
            public MockTaskMetadata (int id)
                : base (id) { }

            public int RunCallCount { get; private set; }

            public void Run (TaskQueue.TaskDataAccess access)
            {
                RunCallCount++;
                access.GetNextTaskData (this).Run ();
            }
        }

        private unsafe class MockTaskMetadata<TTask, TArg, TResult> : MockTaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg, TResult>
            where TTask : struct, ITask<TArg, TResult>
        {
            public MockTaskMetadata (int id)
                : base (id) { }

            public int RunCallCount { get; private set; }

            public void Run (TaskQueue.TaskDataAccess access)
            {
                RunCallCount++;
                access.GetNextTaskData (this).Run (default);
            }

            public void Run (TaskQueue.TaskDataAccess access, TArg arg)
            {
                RunCallCount++;
                access.GetNextTaskData (this).Run (arg);
            }

            public void Run (TaskQueue.TaskDataAccess access, TArg arg, out TResult result)
            {
                RunCallCount++;
                result = access.GetNextTaskData (this).Run (arg);
            }
        }
    }
}
