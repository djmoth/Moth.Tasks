namespace Moth.Tasks.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TaskGroupTests
    {
        [Test]
        public void WhenComplete_InvokesAction_WhenAllTasksComplete ()
        {
            TaskGroup group = new TaskGroup ();
            TaskQueue queue = new TaskQueue ();

            const int taskCount = 3;

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            bool actionInvoked = false;
            group.WhenComplete (() => actionInvoked = true);

            for (int i = 0; i < taskCount; i++)
            {
                Assert.IsFalse (actionInvoked);
                queue.RunNextTask ();
            }

            Assert.IsTrue (actionInvoked);
        }

        [Test]
        public void WhenComplete_InvokesAction_WhenAllTasksComplete_WithMultipleActions ()
        {
            TaskGroup group = new TaskGroup ();
            TaskQueue queue = new TaskQueue ();

            const int taskCount = 3;

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            int actionInvokedCount = 0;
            group.WhenComplete (() => actionInvokedCount++);
            group.WhenComplete (() => actionInvokedCount++);

            for (int i = 0; i < taskCount; i++)
            {
                Assert.AreEqual (0, actionInvokedCount);
                queue.RunNextTask ();
            }

            Assert.AreEqual (2, actionInvokedCount);
        }

        [Test]
        public void WhenComplete_InvokesAction_WhenAllTasksComplete_CalledAfterTasksComplete ()
        {
            TaskGroup group = new TaskGroup ();
            TaskQueue queue = new TaskQueue ();

            const int taskCount = 3;

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            for (int i = 0; i < taskCount; i++)
                queue.RunNextTask ();

            bool actionInvoked = false;
            group.WhenComplete (() => actionInvoked = true);

            Assert.IsTrue (actionInvoked);
        }

        private struct TestTask : ITask
        {
            public void Run ()
            {
                
            }
        }
    }
}
