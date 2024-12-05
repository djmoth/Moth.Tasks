namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    internal class TaskHandleTests
    {
        private Mock<ITaskHandleManager> mockTaskHandleManager;

        [SetUp]
        public void SetUp ()
        {
            mockTaskHandleManager = new Mock<ITaskHandleManager> (MockBehavior.Strict);
        }

        [Test]
        public void Constructor_WithManagerAndHandleID_InitializesCorrectly ()
        {
            int handleID = 42;
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, handleID);

            Assert.That (handle.Manager, Is.EqualTo (mockTaskHandleManager.Object));
            Assert.That (handle.ID, Is.EqualTo (handleID));
        }

        [Test]
        public void IsValid_WhenHandleIsValid_ReturnsTrue ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            Assert.That (handle.IsValid, Is.True);
        }

        [Test]
        public void IsValid_WhenHandleIsInvalid_ReturnsFalse ()
        {
            TaskHandle handle = new TaskHandle (null, 0);

            Assert.That (handle.IsValid, Is.False);
        }

        [Test]
        public void IsComplete_WhenTaskIsComplete_ReturnsTrue ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            mockTaskHandleManager.Setup (m => m.IsTaskComplete (handle)).Returns (true);

            Assert.That (handle.IsComplete, Is.True);
        }

        [Test]
        public void IsComplete_WhenTaskIsNotComplete_ReturnsFalse ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            mockTaskHandleManager.Setup (m => m.IsTaskComplete (handle)).Returns (false);

            Assert.That (handle.IsComplete, Is.False);
        }

        [Test]
        public void WaitForCompletion_WithNoTimeout_CallsTaskHandleManagerWaitForCompletionWithInfiniteTimeout ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            mockTaskHandleManager.Setup (m => m.WaitForCompletion (handle, System.Threading.Timeout.Infinite)).Returns (true);

            handle.WaitForCompletion ();

            mockTaskHandleManager.VerifyAll ();
        }

        [Test]
        public void WaitForCompletion_WithTimeout_CallsTaskHandleManagerWaitForCompletionWithTimeout ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            int timeout = 1000;
            mockTaskHandleManager.Setup (m => m.WaitForCompletion (handle, timeout)).Returns (true);

            handle.WaitForCompletion (timeout);

            mockTaskHandleManager.VerifyAll ();
        }

        [Test]
        public void NotifyTaskCompletion_WhenCalled_CallsTaskHandleManagerNotifyTaskCompletion ()
        {
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 42);

            mockTaskHandleManager.Setup (m => m.NotifyTaskCompletion (handle));

            handle.NotifyTaskCompletion ();

            mockTaskHandleManager.VerifyAll ();
        }

        [Test]
        public void Equals_WhenHandlesAreEqual_ReturnsTrue ()
        {
            TaskHandle handle1 = new TaskHandle (mockTaskHandleManager.Object, 42);
            TaskHandle handle2 = new TaskHandle (mockTaskHandleManager.Object, 42);

            Assert.That (handle1.Equals (handle2), Is.True);
        }

        [Test]
        public void Equals_WhenHandlesAreNotEqual_ReturnsFalse ()
        {
            TaskHandle handle1 = new TaskHandle (mockTaskHandleManager.Object, 42);
            TaskHandle handle2 = new TaskHandle (mockTaskHandleManager.Object, 123);

            Assert.That (handle1.Equals (handle2), Is.False);
        }
    }
}
