namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;

    [TestFixture]
    public class TaskHandleManagerTests
    {
        [Test]
        public void Constructor_WhenCalled_ActiveHandlesIsZero ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();
            Assert.That (taskHandleManager.ActiveHandles, Is.Zero);
        }

        [Test]
        public void CreateTaskHandle_WhenCalled_ReturnsValidTaskHandle ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (taskHandle.ID, Is.GreaterThan (0));
            Assert.That (taskHandle.Manager, Is.SameAs (taskHandleManager));
        }

        [Test]
        public void CreateTaskHandle_WhenCalled_IncrementsActiveHandles ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            int previousActiveHandles = taskHandleManager.ActiveHandles;

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (taskHandleManager.ActiveHandles, Is.EqualTo (previousActiveHandles + 1));
        }

        [Test]
        public void CreateTaskHandle_WhenCalledTwice_DoesNotReturnSameHandleTwice ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle firstTaskHandle = taskHandleManager.CreateTaskHandle ();
            TaskHandle secondTaskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (firstTaskHandle, Is.Not.EqualTo (secondTaskHandle));
        }

        [Test]
        public void NotifyTaskCompletionThenIsTaskComplete_WhenTaskIsComplete_ReturnsTrue ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            taskHandleManager.NotifyTaskCompletion (taskHandle);

            bool isComplete = taskHandleManager.IsTaskComplete (taskHandle);

            Assert.That (isComplete, Is.True);
        }

        [Test]
        public void NotifyTaskCompletion_WithActiveHandle_DecrementsActiveHandles ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            int previousActiveHandles = taskHandleManager.ActiveHandles;

            taskHandleManager.NotifyTaskCompletion (taskHandle);

            Assert.That (taskHandleManager.ActiveHandles, Is.EqualTo (previousActiveHandles - 1));
        }

        [Test]
        public void NotifyTaskCompletion_WithInvalidTaskHandle_ThrowsArgumentException ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();
            TaskHandle taskHandle = default;

            Assert.That (() => taskHandleManager.NotifyTaskCompletion (taskHandle), Throws.ArgumentException);
        }

        [Test]
        public void NotifyTaskCompletion_CalledTwiceWithSameTaskHandle_ThrowsInvalidOperationException ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();
            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            taskHandleManager.NotifyTaskCompletion (taskHandle);

            Assert.That (() => taskHandleManager.NotifyTaskCompletion (taskHandle), Throws.InvalidOperationException);
        }

        [Test]
        public void IsTaskComplete_WhenTaskIsNotComplete_ReturnsFalse ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            bool isComplete = taskHandleManager.IsTaskComplete (taskHandle);

            Assert.That (isComplete, Is.False);
        }

        [Test]
        public void IsTaskComplete_WithInvalidTaskHandle_ThrowsArgumentException ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();
            TaskHandle taskHandle = default;

            Assert.That (() => taskHandleManager.IsTaskComplete (taskHandle), Throws.ArgumentException);
        }

        [Test]
        public void WaitForCompletion_WithInvalidTaskHandle_ThrowsArgumentException ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();
            TaskHandle taskHandle = default;
            Assert.That (() => taskHandleManager.WaitForCompletion (taskHandle, 0), Throws.ArgumentException);
        }

        [Test]
        public void Clear_WhenNotEmpty_ClearsAllTaskHandles ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle firstTaskHandle = taskHandleManager.CreateTaskHandle ();
            TaskHandle secondTaskHandle = taskHandleManager.CreateTaskHandle ();

            taskHandleManager.Clear ();

            Assert.Multiple (() =>
            {
                Assert.That (taskHandleManager.IsTaskComplete (firstTaskHandle), Is.True);
                Assert.That (taskHandleManager.IsTaskComplete (secondTaskHandle), Is.True);
                Assert.That (taskHandleManager.ActiveHandles, Is.Zero);
            });
        }

        [Test]
        public void Clear_WhenEmpty_DoesNothing ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            Assume.That (taskHandleManager.ActiveHandles, Is.Zero);

            taskHandleManager.Clear ();

            Assert.That (taskHandleManager.ActiveHandles, Is.Zero);
        }
    }
}
