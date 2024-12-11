namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class TaskGroupTests
    {
        private Mock<ITaskQueue> mockTaskQueue;

        [SetUp]
        public void SetUp ()
        {
            mockTaskQueue = new Mock<ITaskQueue> (MockBehavior.Strict);
        }

        [Test]
        public void Constructor_WhenCalled_InitializesCorrectly ()
        {
            TaskGroup group = new TaskGroup ();

            Assert.Multiple (() =>
            {
                Assert.That (group.IsDisposed, Is.False);
                Assert.That (group.Progress, Is.EqualTo (1));
                Assert.That (group.TaskCount, Is.Zero);
                Assert.That (group.CompletedCount, Is.Zero);
                Assert.That (group.IsComplete, Is.True);
            });
        }

        [Test]
        public void WhenComplete_WhenCalledWithCompletedGroup_CallsActionImmediately ()
        {
            TaskGroup group = new TaskGroup ();

            Assume.That (group.IsComplete, Is.True);

            bool actionCalled = false;

            group.WhenComplete (() => actionCalled = true);

            Assert.That (actionCalled, Is.True);
        }

        [Test]
        public void WhenComplete_WhenCalledWithNonCompletedGroup_DoesNotCallActionYet ()
        {
            TaskGroup group = new TaskGroup ();

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny));
            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assume.That (group.IsComplete, Is.False);

            bool actionCalled = false;

            group.WhenComplete (() => actionCalled = true);

            Assert.That (actionCalled, Is.False);
        }

        private delegate void TaskQueueEnqueueCallback (in TaskGroup.TaskGroupItem<TestTask> item);

        [Test]
        public void WhenComplete_WhenCalledWithInitiallyNonCompletedGroup_CallsActionWhenTaskDisposed ()
        {
            TaskGroup group = new TaskGroup ();

            TaskGroup.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup.TaskGroupItem<TestTask> x) => item = x));
            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assume.That (group.IsComplete, Is.False);

            bool actionCalled = false;

            group.WhenComplete (() => actionCalled = true);

            Assume.That (actionCalled, Is.False);

            item.Dispose ();

            Assert.That (actionCalled, Is.True);
        }

        [Test]
        public void WhenComplete_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup group = new TaskGroup ();

            group.Dispose ();

            Assert.That (() => group.WhenComplete (() => { }), Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void Enqueue_WhenCalledWithTask_IncrementsTaskCount ()
        {
            TaskGroup group = new TaskGroup ();

            Assume.That (group.TaskCount, Is.Zero);

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny));
            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assert.That (group.TaskCount, Is.EqualTo (1));
        }

        [TestCase (0, 0, 1)]
        [TestCase (1, 0, 0)]
        [TestCase (1, 1, 1)]
        [TestCase (2, 1, 0.5f)]
        public void Progress_WhenCalled_ReturnsCorrectProgress (int taskCount, int completedCount, float expectedProgress)
        {
            TaskGroup group = new TaskGroup ();

            TaskGroup.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup.TaskGroupItem<TestTask> x) => item = x));

            for (int i = 0; i < taskCount; i++)
            {
                group.Enqueue (mockTaskQueue.Object, new TestTask ());

                if (i < completedCount)
                {
                    item.Dispose ();
                }
            }

            Assume.That (group.TaskCount, Is.EqualTo (taskCount));
            Assume.That (group.CompletedCount, Is.EqualTo (completedCount));

            Assert.That (group.Progress, Is.EqualTo (expectedProgress).Within (1).Percent);
        }

        [Test]
        public void Progress_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup group = new TaskGroup ();
            group.Dispose ();
            Assert.That (() => group.Progress, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void TaskCount_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup group = new TaskGroup ();
            group.Dispose ();
            Assert.That (() => group.TaskCount, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void CompletedCount_WhenTaskCompletes_ValueIsIncremented ()
        {
            TaskGroup group = new TaskGroup ();

            TaskGroup.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup.TaskGroupItem<TestTask> x) => item = x));

            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assume.That (group.CompletedCount, Is.Zero);

            item.Dispose ();

            Assert.That (group.CompletedCount, Is.EqualTo (1));
        }

        [Test]
        public void CompletedCount_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup group = new TaskGroup ();
            group.Dispose ();
            Assert.That (() => group.CompletedCount, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void Dispose_WhenComplete_MarksGroupAsDisposed ()
        {
            TaskGroup group = new TaskGroup ();

            Assume.That (group.IsDisposed, Is.False);

            group.Dispose ();

            Assert.That (group.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_WhenNotComplete_ThrowsInvalidOperationException ()
        {
            TaskGroup group = new TaskGroup ();

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup.TaskGroupItem<TestTask>>.IsAny));

            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assert.That (group.Dispose, Throws.InstanceOf<InvalidOperationException> ());
        }

        private struct TestTask : ITask<Unit, Unit>
        {
            public void Run ()
            {

            }
        }
    }
}
