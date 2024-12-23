namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Threading;

    [TestFixture ([typeof (object), typeof (object)])]
    public class WorkerTests<TArg, TResult>
    {
        [Test]
        public void Constructor_WithTaskQueue_InitializesCorrectly ()
        {
            ITaskQueue<TArg, TResult> taskQueue = Mock.Of<ITaskQueue<TArg, TResult>> ();

            using var worker = new Worker<TArg, TResult> (taskQueue, false, default);

            Assert.That (worker.Tasks, Is.EqualTo (taskQueue));
        }

        [Test]
        public void Constructor_WithProfiler_InitializesCorrectly ()
        {
            ITaskQueue<TArg, TResult> taskQueue = Mock.Of<ITaskQueue<TArg, TResult>> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = Mock.Of<IProfiler> (),
                WorkerThread = null,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker<TArg, TResult> (taskQueue, false, options);

            Assert.That (worker, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithWorkerThreadAndAutoStart_StartsThread ()
        {
            ITaskQueue<TArg, TResult> taskQueue = Mock.Of<ITaskQueue<TArg, TResult>> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker<TArg, TResult> (taskQueue, false, options);

            Assert.That (worker, Is.Not.Null);

            mockWorkerThread.Verify (t => t.Start (It.IsAny<ThreadStart> ()), Times.Once);
        }

        [Test]
        public void Constructor_WithWorkerThreadAndManualStart_DoesNotStartThread ()
        {
            ITaskQueue<TArg, TResult> taskQueue = Mock.Of<ITaskQueue<TArg, TResult>> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
                RequiresManualStart = true,
            };

            using var worker = new Worker<TArg, TResult> (taskQueue, false, options);

            Assert.That (worker != null);
            mockWorkerThread.Verify (t => t.Start (It.IsAny<ThreadStart> ()), Times.Never);
        }

        [Test]
        public void Start_CalledTwice_ThrowsInvalidOperationException ()
        {
            var mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
                RequiresManualStart = true,
            };

            using var worker = new Worker<TArg, TResult> (mockTaskQueue.Object, false, options);

            worker.Start ();

            Assert.Throws<InvalidOperationException> (worker.Start);
        }

        [Test]
        public void Dispose_DisposeCalled_DisposesTaskQueueIfSpecified ()
        {
            var mockDisposableTaskQueue = new Mock<ITaskQueue<TArg, TResult>> ().As<IDisposable> ();

            using var worker = new Worker<TArg, TResult> (mockDisposableTaskQueue.Object as ITaskQueue<TArg, TResult>, true, default);

            worker.Dispose ();

            mockDisposableTaskQueue.Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Dispose_WhenCalled_StopsWorker ()
        {
            var mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> (MockBehavior.Strict);
            mockTaskQueue.Setup (queue => queue.RunNextTask (It.IsAny<TArg> (), out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (() =>
            {
                Assert.Fail ("Worker<TArg, TResult> should have been stopped.");
            });

            var mockWorkerThread = new Mock<IWorkerThread> ();

            ThreadStart workerWorkMethod = null;
            mockWorkerThread.Setup (x => x.Start (It.IsAny<ThreadStart> ())).Callback<ThreadStart> (t => workerWorkMethod = t);

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker<TArg, TResult> (mockTaskQueue.Object, false, options);

            worker.Dispose ();

            workerWorkMethod ();

            Assert.That (worker.IsRunning, Is.False);
            Assert.That (worker.CancellationToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void Dispose_WhenNotStartedAndInitializedWithDisposeTaskQueue_DisposesTaskQueue ()
        {
            var mockDisposableTaskQueue = new Mock<ITaskQueue<TArg, TResult>> ().As<IDisposable> ();

            WorkerOptions options = new WorkerOptions
            {
                RequiresManualStart = true,
            };

            var worker = new Worker<TArg, TResult> (mockDisposableTaskQueue.Object as ITaskQueue<TArg, TResult>, true, options);
            worker.Dispose ();

            mockDisposableTaskQueue.Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Dispose_WhenStartedAndInitializedWithDisposeTaskQueue_DisposesTaskQueue ()
        {
            var mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> (MockBehavior.Strict);
            mockTaskQueue.Setup (queue => queue.RunNextTask (It.IsAny<TArg> (), out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (() =>
            {
                Assert.Fail ("Worker<TArg, TResult> should have been stopped.");
            });

            mockTaskQueue.As<IDisposable> ().Setup (t => t.Dispose ());

            var mockWorkerThread = new Mock<IWorkerThread> ();

            ThreadStart workerWorkMethod = null;
            mockWorkerThread.Setup (x => x.Start (It.IsAny<ThreadStart> ())).Callback<ThreadStart> (t => workerWorkMethod = t);

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
                RequiresManualStart = true,
            };

            var worker = new Worker<TArg, TResult> (mockTaskQueue.Object, true, options);
            worker.Start ();

            worker.Dispose ();

            workerWorkMethod ();

            mockTaskQueue.As<IDisposable> ().Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Worker_WhenRunning_RunsTask ()
        {
            var mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            mockTaskQueue.Setup (t => t.RunNextTask (It.IsAny<TArg> (), out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (Assert.Pass);

            mockWorkerThread.Setup (x => x.Start (It.IsAny<ThreadStart> ())).Callback<ThreadStart> (t => t ());

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker<TArg, TResult> (mockTaskQueue.Object, false, options);
        }

        delegate void RunNextTaskCallback (out Exception exception, IProfiler profiler, CancellationToken cancellationToken);

        delegate TResult RunNextTaskReturn (TArg arg, out Exception exception, IProfiler profiler, CancellationToken cancellationToken);

        [Test]
        public void Worker_WhenRunningTaskThatThrowsException_ReportsExceptionToExceptionHandler ()
        {
            var mockException = new Exception ();

            var mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> (MockBehavior.Strict);

            mockTaskQueue.Setup (t => t.RunNextTask (It.IsAny<TArg> (), out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Returns (new RunNextTaskReturn ((TArg arg, out Exception exception, IProfiler profiler, CancellationToken cancellationToken) =>
            {
                exception = mockException;
                return default;
            }));

            var mockWorkerThread = new Mock<IWorkerThread> ();
            mockWorkerThread.Setup (x => x.Start (It.IsAny<ThreadStart> ())).Callback<ThreadStart> (t => t ());

            var mockExceptionEventHandler = new Mock<EventHandler<TaskExceptionEventArgs>> (MockBehavior.Strict);

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = mockExceptionEventHandler.Object,
                RequiresManualStart = true,
            };
            using var worker = new Worker<TArg, TResult> (mockTaskQueue.Object, false, options);

            // Setup exception handler to pass test when called
            mockExceptionEventHandler.Setup (e => e (worker, It.Is<TaskExceptionEventArgs> (a => a.Exception == mockException))).Callback (Assert.Pass);

            worker.Start ();

            // Test should never be able to reach this point as the worker should either keep running or Assert.Pass should have been called by the exception handler handler
            Assume.That (true, Is.False);
        }
    }
}
