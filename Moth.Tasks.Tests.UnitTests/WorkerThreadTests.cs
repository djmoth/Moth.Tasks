namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System.Threading;

    [TestFixture]
    public class WorkerThreadTests
    {
        [TestCase (true)]
        [TestCase (false)]
        public void Constructor_WithIsBackground_SetsIsBackground (bool isBackground)
        {
            WorkerThread workerThread = new WorkerThread (isBackground);

            Assert.That (workerThread.IsBackground, Is.EqualTo (isBackground));
        }

        [Test, CancelAfter (1000)]
        public void Start_WhenNotStarted_CallsStartOnThread (CancellationToken token)
        {
            WorkerThread workerThread = new WorkerThread (false);

            ManualResetEventSlim startCalled = new ManualResetEventSlim (false);

            workerThread.Start (startCalled.Set);

            startCalled.Wait (token);

            Assert.That (token.IsCancellationRequested, Is.False);
        }

        [Test]
        public void Start_WhenStarted_ThrowsInvalidOperationException ()
        {
            WorkerThread workerThread = new WorkerThread (false);

            workerThread.Start (() => { });

            Assert.That (() => workerThread.Start (() => { }), Throws.InvalidOperationException);
        }

        [Test]
        public void Join_WhenStarted_WaitsForThreadToStop ()
        {
            WorkerThread workerThread = new WorkerThread (false);

            workerThread.Start (() => { });

            // Thread should end immediately

            Assert.That (workerThread.Join, Throws.Nothing);
        }

        [Test]
        public void Join_WhenNotStarted_ThrowsInvalidOperationException ()
        {
            WorkerThread workerThread = new WorkerThread (false);

            Assert.That (workerThread.Join, Throws.InvalidOperationException);
        }
    }
}
