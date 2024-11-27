namespace Moth.Tasks.Tests
{
    using Moq;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Threading;

    [TestFixture]
    public class WorkerTests
    {
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

            ClassicAssert.IsNotNull (worker);
        }

        [Test]
        public void Constructor_WithWorkerThread_StartsThread ()
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

            ClassicAssert.IsNotNull (worker);
            mockWorkerThread.Verify (t => t.Start (It.IsAny<ThreadStart> ()), Times.Once);
        }

        [Test]
        public void Dispose_DisposesTaskQueueIfSpecified ()
        {
            var mockDisposableTaskQueue = new Mock<ITaskQueue> ().As<IDisposable> ();

            using var worker = new Worker (mockDisposableTaskQueue.Object as ITaskQueue, true, default);

            worker.Dispose ();

            mockDisposableTaskQueue.Verify (t => t.Dispose (), Times.Once);
        }
    }
}
