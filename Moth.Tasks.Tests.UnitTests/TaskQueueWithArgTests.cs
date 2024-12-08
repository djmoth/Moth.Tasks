namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;

    [TestFixture]
    public class TaskQueueWithArgTests
    {
        public void Enqueue_WithArg_EnqueuesTask ()
        {
            var taskQueue = new TaskQueue<int> ();
            var task = new TestTask ();

            taskQueue.Enqueue (task);

            Assert.That (taskQueue.Count, Is.EqualTo (1));
        }

        public void Enqueue_WithArgAndHandle_EnqueuesTask ()
        {
            var taskQueue = new TaskQueue<int> ();

            var task = new TestTask ();

            taskQueue.Enqueue (task, out var handle);

            Assert.That (taskQueue.Count, Is.EqualTo (1));
        }

        public struct TestTask : ITask<int>
        {
            public void Run (int arg) { }
        }
    }
}
