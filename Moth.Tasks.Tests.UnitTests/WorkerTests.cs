namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Threading;

    [TestFixture]
    public class WorkerTests
    {
        [Test]
        public void Constructor_WithTaskQueue_InitializesCorrectly ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();

            using var worker = new Worker (taskQueue, false, default);

            Assert.That (worker.Tasks == taskQueue);
        }

        [Test]
        public void Constructor_WithProfiler_InitializesCorrectly ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = Mock.Of<IProfiler> (),
                WorkerThread = null,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker (taskQueue, false, options);

            Assert.That (worker != null);
        }

        [Test]
        public void Constructor_WithProfilerProvider_InitializesCorrectly ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();

            var mockProfilerProvider = new Mock<ProfilerProvider> ();
            mockProfilerProvider.Setup (p => p (It.IsAny<Worker> ())).Returns (Mock.Of<IProfiler> ());

            WorkerOptions options = new WorkerOptions
            {
                ProfilerProvider = mockProfilerProvider.Object,
                WorkerThread = null,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker (taskQueue, false, options);

            Assert.That (worker != null);

            mockProfilerProvider.Verify (p => p (It.IsAny<Worker> ()), Times.Once);
        }

        [Test]
        public void Constructor_WithWorkerThreadAndAutoStart_StartsThread ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker (taskQueue, false, options);

            Assert.That (worker != null);

            mockWorkerThread.Verify (t => t.Start (It.IsAny<ThreadStart> ()), Times.Once);
        }

        [Test]
        public void Construct_WithWorkerThreadProvider_InitializesCorrectly ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();

            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            mockWorkerThreadProvider.Setup (p => p (It.IsAny<Worker> ())).Returns (Mock.Of<IWorkerThread> ());

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker (taskQueue, false, options);

            Assert.That (worker != null);

            mockWorkerThreadProvider.Verify (p => p (It.IsAny<Worker> ()), Times.Once);
        }

        [Test]
        public void Constructor_WithWorkerThreadAndManualStart_DoesNotStartThread ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
                RequiresManualStart = true,
            };

            using var worker = new Worker (taskQueue, false, options);

            Assert.That (worker != null);
            mockWorkerThread.Verify (t => t.Start (It.IsAny<ThreadStart> ()), Times.Never);
        }

        [Test]
        public void Start_CalledTwice_ThrowsInvalidOperationException ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
                RequiresManualStart = true,
            };

            using var worker = new Worker (mockTaskQueue.Object, false, options);

            worker.Start ();

            Assert.Throws<InvalidOperationException> (worker.Start);
        }

        [Test]
        public void Dispose_DisposeCalled_DisposesTaskQueueIfSpecified ()
        {
            var mockDisposableTaskQueue = new Mock<ITaskQueue> ().As<IDisposable> ();

            using var worker = new Worker (mockDisposableTaskQueue.Object as ITaskQueue, true, default);

            worker.Dispose ();

            mockDisposableTaskQueue.Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Dispose_WhenCalled_StopsWorker ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> (MockBehavior.Strict);
            mockTaskQueue.Setup (queue => queue.RunNextTask (out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (() =>
            {
                Assert.Fail ("Worker should have been stopped.");
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

            using var worker = new Worker (mockTaskQueue.Object, false, options);

            worker.Dispose ();

            workerWorkMethod ();

            Assert.That (worker.IsRunning, Is.False);
            Assert.That (worker.CancellationToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void Dispose_WhenNotStartedAndInitializedWithDisposeTaskQueue_DisposesTaskQueue ()
        {
            var mockDisposableTaskQueue = new Mock<ITaskQueue> ().As<IDisposable> ();

            var worker = new Worker (mockDisposableTaskQueue.Object as ITaskQueue, true, default);
            worker.Dispose ();

            mockDisposableTaskQueue.Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Dispose_WhenStartedAndInitializedWithDisposeTaskQueue_DisposesTaskQueue ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> (MockBehavior.Strict);
            mockTaskQueue.Setup (queue => queue.RunNextTask (out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (() =>
            {
                Assert.Fail ("Worker should have been stopped.");
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

            var worker = new Worker (mockTaskQueue.Object, true, options);
            worker.Start ();

            worker.Dispose ();

            workerWorkMethod ();

            mockTaskQueue.As<IDisposable> ().Verify (t => t.Dispose (), Times.Once);
        }

        [Test]
        public void Worker_WhenRunning_RunsTask ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThread = new Mock<IWorkerThread> ();

            mockTaskQueue.Setup (t => t.RunNextTask (out It.Ref<Exception>.IsAny, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ())).Callback (Assert.Pass);

            mockWorkerThread.Setup (x => x.Start (It.IsAny<ThreadStart> ())).Callback<ThreadStart> (t => t ());

            WorkerOptions options = new WorkerOptions
            {
                Profiler = null,
                WorkerThread = mockWorkerThread.Object,
                ExceptionEventHandler = null,
            };

            using var worker = new Worker (mockTaskQueue.Object, false, options);
        }

        delegate void RunNextTaskCallback (out Exception exception, IProfiler profiler, CancellationToken cancellationToken);

        [Test]
        public void Worker_WhenRunningTaskThatThrowsException_ReportsExceptionToExceptionHandler ()
        {
            var mockException = new Exception ();

            var mockTaskQueue = new Mock<ITaskQueue> (MockBehavior.Strict);

            mockTaskQueue.Setup (t => t.RunNextTask (out mockException, It.IsAny<IProfiler> (), It.IsAny<CancellationToken> ()));

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

            using var worker = new Worker (mockTaskQueue.Object, false, options);
            mockExceptionEventHandler.Setup (e => e (worker, It.Is<TaskExceptionEventArgs> (a => a.Exception == mockException))).Callback (Assert.Pass);

            worker.Start ();

            mockExceptionEventHandler.Verify (e => e (worker, It.Is<TaskExceptionEventArgs> (a => a.Exception == mockException)), Times.Once);
        }
    }
}
