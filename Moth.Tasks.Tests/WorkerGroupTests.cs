namespace Moth.Tasks.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Threading;

    [TestFixture]
    public class WorkerGroupTests
    {
        [Test]
        public void Constructor_WithZeroWorkerCount_ThrowsArgumentOutOfRangeException ()
        {
            Assert.Throws<ArgumentOutOfRangeException> (() => new WorkerGroup (0, Mock.Of<ITaskQueue> (), false, null));
        }

        [Test]
        public void Constructor_WithNegativeWorkerCount_ThrowsArgumentOutOfRangeException ()
        {
            Assert.Throws<ArgumentOutOfRangeException> (() => new WorkerGroup (-1, Mock.Of<ITaskQueue> (), false, null));
        }

        [Test]
        public void Constructor_WithNullTaskQueue_ThrowsArgumentNullException ()
        {
            Assert.Throws<ArgumentNullException> (() => new WorkerGroup (1, null, false, null));
        }

        [Test]
        public void Constructor_WithTaskQueue_InitializesCorrectly ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();

            WorkerGroup workerGroup = new WorkerGroup (1, taskQueue, false, null);

            Assert.That (workerGroup.Tasks == taskQueue);
        }

        [Test]
        public void Constructor_WithWorkerProvider_GetsAndStartsWorkers ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            int workerCount = 4;

            Mock<IWorker>[] mockWorkers = new Mock<IWorker>[workerCount];

            Mock<WorkerProvider> mockWorkerProvider = new Mock<WorkerProvider> ();
            mockWorkerProvider.Setup (x => x.Invoke (It.IsAny<WorkerGroup> (), It.IsAny<int> ()))
                .Callback ((WorkerGroup group, int i) => mockWorkers[i] = new Mock<IWorker> ())
                .Returns ((WorkerGroup group, int i) => mockWorkers[i].Object);

            WorkerGroup workerGroup = new WorkerGroup (workerCount, taskQueue, false, mockWorkerProvider.Object);

            mockWorkerProvider.Verify (x => x.Invoke (It.IsAny<WorkerGroup> (), It.IsAny<int> ()), Times.Exactly (workerCount));

            for (int i = 0; i < workerCount; i++)
            {
                mockWorkers[i].Verify (w => w.Start (), Times.Once);
            }
        }

        [Test]
        public void Dispose_WhenCalled_DisposesWorkers ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            int workerCount = 4;

            Mock<IWorker>[] mockWorkers = new Mock<IWorker>[workerCount];

            WorkerProvider workerProvider = (group, i) =>
            {
                mockWorkers[i] = new Mock<IWorker> ();
                return mockWorkers[i].Object;
            };

            WorkerGroup workerGroup = new WorkerGroup (workerCount, taskQueue, false, workerProvider);
            workerGroup.Dispose ();

            for (int i = 0; i < workerCount; i++)
            {
                mockWorkers[i].Verify (w => w.Dispose (), Times.Once);
            }
        }

        [Test]
        public void Join_WhenCalled_JoinsWorkers ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            int workerCount = 4;

            Mock<IWorker>[] mockWorkers = new Mock<IWorker>[workerCount];
            WorkerProvider workerProvider = (group, i) =>
            {
                mockWorkers[i] = new Mock<IWorker> ();
                return mockWorkers[i].Object;
            };

            WorkerGroup workerGroup = new WorkerGroup (workerCount, taskQueue, false, workerProvider);
            workerGroup.Dispose ();

            workerGroup.Join ();

            for (int i = 0; i < workerCount; i++)
            {
                mockWorkers[i].Verify (w => w.Join (), Times.Once);
            }
        }

        [Test]
        public void Join_WhenNotDisposed_ThrowsObjectDisposedException ()
        {
            ITaskQueue taskQueue = Mock.Of<ITaskQueue> ();
            int workerCount = 1;

            WorkerProvider workerProvider = (group, i) => Mock.Of<IWorker> ();

            WorkerGroup workerGroup = new WorkerGroup (workerCount, taskQueue, false, workerProvider);

            Assert.Throws<InvalidOperationException> (workerGroup.Join);
        }
    }
}
