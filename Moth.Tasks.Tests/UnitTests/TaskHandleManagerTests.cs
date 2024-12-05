namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;

    [TestFixture]
    public class TaskHandleManagerTests
    {
        [Test]
        public void CreateTaskHandle_WhenCalled_ReturnsValidTaskHandle ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (taskHandle.IsValid, Is.True);
        }

        [Test]
        public void CreateTaskHandle_WhenCalled_ReturnsTaskHandleWithCorrectManager ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle taskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (taskHandle.Manager, Is.SameAs (taskHandleManager));
        }

        [Test]
        public void CreateTaskHandle_WhenCalledTwice_DoesNotReturnSameHandleTwice ()
        {
            TaskHandleManager taskHandleManager = new TaskHandleManager ();

            TaskHandle firstTaskHandle = taskHandleManager.CreateTaskHandle ();
            TaskHandle secondTaskHandle = taskHandleManager.CreateTaskHandle ();

            Assert.That (firstTaskHandle, Is.Not.EqualTo (secondTaskHandle));
        }
    }
}
