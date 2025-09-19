namespace Moth.Tasks.Tests.UnitTests
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
            Assert.Throws<ArgumentOutOfRangeException> (() => new WorkerGroup<Unit, Unit> (0, Mock.Of<ITaskQueue<Unit, Unit>> (), false, null));
        }

        [Test]
        public void Constructor_WithNegativeWorkerCount_ThrowsArgumentOutOfRangeException ()
        {
            Assert.Throws<ArgumentOutOfRangeException> (() => new WorkerGroup<Unit, Unit> (-1, Mock.Of<ITaskQueue<Unit, Unit>> (), false, null));
        }

        [Test]
        public void Constructor_WithNullTaskQueue_ThrowsArgumentNullException ()
        {
            Assert.Throws<ArgumentNullException> (() => new WorkerGroup<Unit, Unit> (1, null, false, null));
        }

        [Test]
        public void Constructor_WithTaskQueue_InitializesCorrectly ()
        {
            ITaskQueue<Unit, Unit> taskQueue = Mock.Of<ITaskQueue<Unit, Unit>> ();

            WorkerGroup<Unit, Unit> workerGroup = new WorkerGroup<Unit, Unit> (1, taskQueue, false, null);

            Assert.That (workerGroup.Tasks == taskQueue);
        }

        [Test]
        public void Constructor_WithWorkerProvider_GetsAndStartsWorkers ()
        {
            ITaskQueue<Unit, Unit> taskQueue = Mock.Of<ITaskQueue<Unit, Unit>> ();
            int workerCount = 4;

            Mock<IWorker<Unit, Unit>>[] mockWorkers = new Mock<IWorker<Unit, Unit>>[workerCount];

            Mock<WorkerProvider<Unit, Unit>> mockWorkerProvider = new Mock<WorkerProvider<Unit, Unit>> ();
            mockWorkerProvider.Setup (x => x.Invoke (It.IsAny<WorkerGroup<Unit, Unit>> (), It.IsAny<int> ()))
                .Callback ((WorkerGroup<Unit, Unit> group, int i) => mockWorkers[i] = new Mock<IWorker<Unit, Unit>> ())
                .Returns ((WorkerGroup<Unit, Unit> group, int i) => mockWorkers[i].Object);


            WorkerGroup<Unit, Unit> workerGroup = new WorkerGroup<Unit, Unit> (workerCount, taskQueue, false, mockWorkerProvider.Object);

            for (int i = 0; i < workerCount; i++)
            {
                mockWorkerProvider.Verify (x => x.Invoke (workerGroup, i), Times.Once);
                mockWorkers[i].Verify (w => w.Start (), Times.Once);
            }

            mockWorkerProvider.VerifyNoOtherCalls ();
        }

        [Test]
        public void Dispose_WhenCalled_DisposesWorkers ()
        {
            ITaskQueue<Unit, Unit> taskQueue = Mock.Of<ITaskQueue<Unit, Unit>> ();
            int workerCount = 4;

            Mock<IWorker<Unit, Unit>>[] mockWorkers = new Mock<IWorker<Unit, Unit>>[workerCount];
            

            WorkerProvider<Unit, Unit> workerProvider = (group, i) =>
            {
                mockWorkers[i] = new Mock<IWorker<Unit, Unit>> ();
                mockWorkers[i].As<IDisposable> ().Setup (w => w.Dispose ());
                return mockWorkers[i].Object;
            };

            WorkerGroup<Unit, Unit> workerGroup = new WorkerGroup<Unit, Unit> (workerCount, taskQueue, false, workerProvider);
            workerGroup.Dispose ();

            for (int i = 0; i < workerCount; i++)
            {
                mockWorkers[i].As<IDisposable> ().Verify (w => w.Dispose (), Times.Once);
            }
        }

        [Test]
        public void Join_WhenCalled_JoinsWorkers ()
        {
            ITaskQueue<Unit, Unit> taskQueue = Mock.Of<ITaskQueue<Unit, Unit>> ();
            int workerCount = 4;

            Mock<IWorker<Unit, Unit>>[] mockWorkers = new Mock<IWorker<Unit, Unit>>[workerCount];
            WorkerProvider<Unit, Unit> workerProvider = (group, i) =>
            {
                mockWorkers[i] = new Mock<IWorker<Unit, Unit>> ();
                return mockWorkers[i].Object;
            };

            WorkerGroup<Unit, Unit> workerGroup = new WorkerGroup<Unit, Unit> (workerCount, taskQueue, false, workerProvider);
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
            ITaskQueue<Unit, Unit> taskQueue = Mock.Of<ITaskQueue<Unit, Unit>> ();
            int workerCount = 1;

            WorkerProvider<Unit, Unit> workerProvider = (group, i) => Mock.Of<IWorker<Unit, Unit>> ();

            WorkerGroup<Unit, Unit> workerGroup = new WorkerGroup<Unit, Unit> (workerCount, taskQueue, false, workerProvider);

            Assert.Throws<InvalidOperationException> (workerGroup.Join);
        }
    }
}
