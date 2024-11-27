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
        public void Constructor_InitializesCorrectly ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            var mockProfilerProvider = new Mock<ProfilerProvider> ();

            WorkerGroupOptions options = new WorkerGroupOptions
            {
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ProfilerProvider = mockProfilerProvider.Object,
                ExceptionEventHandler = null,
            };

            using var workerGroup = new WorkerGroup (3, mockTaskQueue.Object, false, options);

            Assert.That (workerGroup != null);
        }

        [Test]
        public void WorkerCount_SetToNewValue_ResizesWorkersArray ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            var mockProfilerProvider = new Mock<ProfilerProvider> ();

            WorkerGroupOptions options = new WorkerGroupOptions
            {
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ProfilerProvider = mockProfilerProvider.Object,
                ExceptionEventHandler = null,
            };

            using var workerGroup = new WorkerGroup (3, mockTaskQueue.Object, false, options);

            workerGroup.WorkerCount = 5;

            Assert.That (workerGroup.WorkerCount, Is.EqualTo (5));
        }

        [Test]
        public void Dispose_DisposesAllWorkers ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            var mockProfilerProvider = new Mock<ProfilerProvider> ();

            WorkerGroupOptions options = new WorkerGroupOptions
            {
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ProfilerProvider = mockProfilerProvider.Object,
                ExceptionEventHandler = null,
            };

            using var workerGroup = new WorkerGroup (3, mockTaskQueue.Object, false, options);

            workerGroup.Dispose ();

            foreach (var worker in workerGroup.Workers)
            {
                Assert.That (worker.IsDisposed, Is.True);
            }
        }

        [Test]
        public void DisposeAndJoin_DisposesAndJoinsAllWorkers ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            var mockProfilerProvider = new Mock<ProfilerProvider> ();

            WorkerGroupOptions options = new WorkerGroupOptions
            {
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ProfilerProvider = mockProfilerProvider.Object,
                ExceptionEventHandler = null,
            };

            using var workerGroup = new WorkerGroup (3, mockTaskQueue.Object, false, options);

            workerGroup.DisposeAndJoin ();

            foreach (var worker in workerGroup.Workers)
            {
                Assert.That (worker.IsDisposed, Is.True);
                Assert.That (worker.IsJoined, Is.True);
            }
        }

        [Test]
        public void Join_JoinsAllWorkers ()
        {
            var mockTaskQueue = new Mock<ITaskQueue> ();
            var mockWorkerThreadProvider = new Mock<WorkerThreadProvider> ();
            var mockProfilerProvider = new Mock<ProfilerProvider> ();

            WorkerGroupOptions options = new WorkerGroupOptions
            {
                WorkerThreadProvider = mockWorkerThreadProvider.Object,
                ProfilerProvider = mockProfilerProvider.Object,
                ExceptionEventHandler = null,
            };

            using var workerGroup = new WorkerGroup (3, mockTaskQueue.Object, false, options);

            workerGroup.Dispose ();
            workerGroup.Join ();

            foreach (var worker in workerGroup.Workers)
            {
                Assert.That (worker.IsJoined, Is.True);
            }
        }
    }
}
