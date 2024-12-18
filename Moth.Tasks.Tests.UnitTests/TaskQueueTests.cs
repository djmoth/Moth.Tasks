namespace Moth.Tasks.Tests.UnitTests
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [TestFixture ([typeof (object), typeof (object)])]
    [TestFixture ([typeof (Unit), typeof (Unit)])]
    [TestFixture ([typeof (int), typeof (int)])]
    public class TaskQueueTests<TArg, TResult>
    {
        private Mock<ITaskMetadataCache> mockTaskCache;
        private Mock<ITaskDataStore> mockTaskDataStore;
        private Mock<ITaskHandleManager> mockTaskHandleManager;
        private Mock<IProfiler> mockProfiler;
        private MockTaskMetadata<TestTask, TArg, TResult> mockTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<TestTask, TArg, TResult>, TArg, TResult> mockTestTaskWithHandle;
        private MockTaskMetadata<DisposableTestTask, TArg, TResult> mockDisposableTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<DisposableTestTask, TArg, TResult>, TArg, TResult> mockDisposableTestTaskWithHandle;

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

            mockTestTaskMetadata = CreateTaskMetadata<TestTask> ();

            mockDisposableTestTaskMetadata = CreateTaskMetadata<DisposableTestTask> ();

            mockTestTaskWithHandle = CreateTaskMetadata<TaskWithHandle<TestTask, TArg, TResult>> ();

            mockDisposableTestTaskWithHandle = CreateTaskMetadata<TaskWithHandle<DisposableTestTask, TArg, TResult>> ();

            MockTaskMetadata<TTask, TArg, TResult> CreateTaskMetadata<TTask> ()
                where TTask : struct, ITask<TArg, TResult>
            {
                MockTaskMetadata<TTask, TArg, TResult> taskMetadata = new MockTaskMetadata<TTask, TArg, TResult> (nextTaskMetadataID++);

                mockTaskCache.Setup (t => t.GetTask<TTask> ()).Returns (taskMetadata);
                mockTaskCache.Setup (t => t.GetTask (taskMetadata.ID)).Returns (taskMetadata);

                mockTaskDataStore.Setup (store => store.Enqueue (It.Ref<TTask>.IsAny, taskMetadata));

                return taskMetadata;
            }
        }

        [Test]
        public void Count_WhenQueueIsEmpty_IsZero ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Count_WhenTaskEnqueued_Increments ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Enqueue (new TestTask ());

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenEmpty_GetsFromTaskCacheAndStoresInTaskDataStore ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);

            mockTaskCache.Verify (t => t.GetTask<DisposableTestTask> (), Times.Once);

            mockTaskDataStore.Verify (store => store.Enqueue (task, mockDisposableTestTaskMetadata), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public unsafe void Enqueue_TaskEnqueuedWhenNotEmpty_GetsTaskMetadataFromTaskCacheAndStoresTaskDataInTaskDataStore ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            mockTaskHandleManager.Setup (x => x.CreateTaskHandle ()).Returns (default (TaskHandle));

            TestTask task = new TestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public void Enqueue_TaskWithHandle_ReturnsCorrectHandle ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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

        [CancelAfter (1000)]
        public unsafe void RunNextTask_OneTaskEnqueuedAndMethodCalled_RunsTaskWithArgAndReturnsResult (CancellationToken token)
        {
            Assume.That (token.CanBeCanceled, Is.True);

            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);

            TResult resultToReturn;

            if (typeof (TResult) == typeof (object))
                resultToReturn = (TResult)new object ();
            else if (typeof (TResult) == typeof (Unit))
                resultToReturn = default;
            else if (typeof (TResult) == typeof (int))
                resultToReturn = (TResult)(object)123;
            else
                throw new NotSupportedException ();

            mockTestTaskMetadata.ResultsToReturn.Enqueue (resultToReturn);

            TArg arg;

            if (typeof (TArg) == typeof (object))
                arg = (TArg)new object ();
            else if (typeof (TArg) == typeof (Unit))
                arg = default;
            else if (typeof (TArg) == typeof (int))
                arg = (TArg)(object)42;
            else
                throw new NotSupportedException ();

            TResult returnedResult = queue.RunNextTask (arg, profiler: null, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            Assert.That (mockTestTaskMetadata.SuppliedArgs, Is.EqualTo (new TArg[] { arg }));
            Assert.That  (returnedResult, Is.EqualTo (resultToReturn));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TwoTasksEnqueuedAndMethodCalled_RunsFirstTaskOnly (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };
            DisposableTestTask disposableTask = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (disposableTask);

            // Run first TestTask with default argument with no profiler and discard result
            queue.RunNextTask (arg: default, profiler: null, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            Assert.Multiple (() =>
            {
                // Assert that only the first TestTask was run
                Assert.That (mockTestTaskMetadata.SuppliedArgs.Count, Is.EqualTo (1));
                Assert.That (mockDisposableTestTaskMetadata.SuppliedArgs.Count, Is.Zero);
            });
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TaskThrowsException_RunsTaskAndCatchesException (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask ();

            // The Mock Task Metadata will now throw an exception on Run
            mockTestTaskMetadata.ExceptionToThrow = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (default, out Exception exception, null, token);

            Assume.That (token.IsCancellationRequested, Is.False);

            // Verify that the exception was caught and returned
            Assert.That (exception, Is.SameAs (mockTestTaskMetadata.ExceptionToThrow));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfiler_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (arg: default, profiler: mockProfiler.Object, token: token);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            TestTask task = new TestTask { };

            mockTestTaskMetadata.ExceptionToThrow = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (arg: default, profiler: mockProfiler.Object, token: token);

            if (token.IsCancellationRequested)
                Assert.Fail ("Test was cancelled by CancelAfter attribute");

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTask).FullName), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsEmpty_ReturnsFalse ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            bool taskWasRun = queue.TryRunNextTask (default, out TResult result);

            Assert.That (taskWasRun, Is.False);
        }

        [Test]
        public void Clear_WhenQueueIsEmpty_ClearsDependencies ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_NoDisposableTasks_ClearsQueueWhileSkippingTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

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
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Dispose_WhenCalledTwice_ClearsTasksThenDoesNothing ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> (0, mockTaskCache.Object, mockTaskDataStore.Object, mockTaskHandleManager.Object);

            queue.Dispose ();
            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        private unsafe struct TestTask : ITask<TArg, TResult>
        {
            public TResult Run (TArg arg) => default;
        }

        private unsafe struct DisposableTestTask : ITask<TArg, TResult>, IDisposable
        {
            public TResult Run (TArg arg) => default;

            public void Dispose () { }
        }
    }
}
