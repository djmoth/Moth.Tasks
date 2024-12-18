namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;
    using System;

    [TestFixture ([typeof (object), typeof (object)])]
    public class TaskGroupTests<TArg, TResult>
    {
        private Mock<ITaskQueue<TArg, TResult>> mockTaskQueue;

        [SetUp]
        public void SetUp ()
        {
            mockTaskQueue = new Mock<ITaskQueue<TArg, TResult>> (MockBehavior.Strict);
        }

        [Test]
        public void Constructor_WhenCalled_InitializesCorrectly ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

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
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            Assume.That (group.IsComplete, Is.True);

            bool actionCalled = false;

            group.WhenComplete (() => actionCalled = true);

            Assert.That (actionCalled, Is.True);
        }

        [Test]
        public void WhenComplete_WhenCalledWithNonCompletedGroup_DoesNotCallActionYet ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny));
            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assume.That (group.IsComplete, Is.False);

            bool actionCalled = false;

            group.WhenComplete (() => actionCalled = true);

            Assert.That (actionCalled, Is.False);
        }

        private delegate void TaskQueueEnqueueCallback (in TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> item);

        [Test]
        public void WhenComplete_WhenCalledWithInitiallyNonCompletedGroup_CallsActionWhenTaskDisposed ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> x) => item = x));
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
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            group.Dispose ();

            Assert.That (() => group.WhenComplete (() => { }), Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void Enqueue_WhenCalledWithTask_IncrementsTaskCount ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            Assume.That (group.TaskCount, Is.Zero);

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny));
            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assert.That (group.TaskCount, Is.EqualTo (1));
        }

        [TestCase (0, 0, 1)]
        [TestCase (1, 0, 0)]
        [TestCase (1, 1, 1)]
        [TestCase (2, 1, 0.5f)]
        public void Progress_WhenCalled_ReturnsCorrectProgress (int taskCount, int completedCount, float expectedProgress)
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> x) => item = x));

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
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();
            group.Dispose ();
            Assert.That (() => group.Progress, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void TaskCount_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();
            group.Dispose ();
            Assert.That (() => group.TaskCount, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void CompletedCount_WhenTaskCompletes_ValueIsIncremented ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> item = default;
            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny))
                .Callback (new TaskQueueEnqueueCallback ((in TaskGroup<TArg, TResult>.TaskGroupItem<TestTask> x) => item = x));

            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assume.That (group.CompletedCount, Is.Zero);

            item.Dispose ();

            Assert.That (group.CompletedCount, Is.EqualTo (1));
        }

        [Test]
        public void CompletedCount_WhenDisposed_ThrowsObjectDisposedException ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();
            group.Dispose ();
            Assert.That (() => group.CompletedCount, Throws.InstanceOf<ObjectDisposedException> ());
        }

        [Test]
        public void Dispose_WhenComplete_MarksGroupAsDisposed ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            Assume.That (group.IsDisposed, Is.False);

            group.Dispose ();

            Assert.That (group.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_WhenNotComplete_ThrowsInvalidOperationException ()
        {
            TaskGroup<TArg, TResult> group = new TaskGroup<TArg, TResult> ();

            mockTaskQueue.Setup (x => x.Enqueue (It.Ref<TaskGroup<TArg, TResult>.TaskGroupItem<TestTask>>.IsAny));

            group.Enqueue (mockTaskQueue.Object, new TestTask ());

            Assert.That (group.Dispose, Throws.InstanceOf<InvalidOperationException> ());
        }

        private struct TestTask : ITask<TArg, TResult>
        {
            public TResult Run (TArg _) => default;
        }
    }
}
