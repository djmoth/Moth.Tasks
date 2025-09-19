namespace Moth.Tasks.Tests.IntegrationTests
{
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TaskGroupTests
    {
        [TestCase (0)]
        [TestCase (1)]
        [TestCase (2)]
        public void ActionSuppliedToWhenCompleteIsInvokedOnceWhenAllTasksComplete (int taskCount)
        {
            TaskGroup<Unit, Unit> group = new TaskGroup<Unit, Unit> ();
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            int actionInvokeCount = 0;
            group.WhenComplete (() => actionInvokeCount++);

            for (int i = 0; i < taskCount; i++)
            {
                Assume.That (actionInvokeCount, Is.Zero);
                queue.RunNextTask (default);
            }

            Assert.That (actionInvokeCount, Is.EqualTo (1));
        }

        [TestCase (0)]
        [TestCase (1)]
        [TestCase (2)]
        public void MultipleActionsSuppliedToWhenCompleteAreEachInvokedOnceWhenAllTasksComplete (int taskCount)
        {
            TaskGroup<Unit, Unit> group = new TaskGroup<Unit, Unit> ();
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            int firstActionInvokeCount = 0;
            int secondActionInvokeCount = 0;
            group.WhenComplete (() => firstActionInvokeCount++);
            group.WhenComplete (() => secondActionInvokeCount++);

            for (int i = 0; i < taskCount; i++)
            {
                Assume.That (firstActionInvokeCount, Is.Zero);
                Assume.That (secondActionInvokeCount, Is.Zero);
                queue.RunNextTask (default);
            }

            Assert.Multiple (() =>
            {
                Assert.That (firstActionInvokeCount, Is.EqualTo (1));
                Assert.That (secondActionInvokeCount, Is.EqualTo (1));
            });
        }

        [TestCase (0)]
        [TestCase (1)]
        [TestCase (2)]
        public void WhenCompleteCalledWhenAllTasksCompleteInvokesActionOnceImmediately (int taskCount)
        {
            TaskGroup<Unit, Unit> group = new TaskGroup<Unit, Unit> ();
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            for (int i = 0; i < taskCount; i++)
                group.Enqueue (queue, new TestTask ());

            for (int i = 0; i < taskCount; i++)
                queue.RunNextTask (default);

            int actionInvokeCount = 0;
            group.WhenComplete (() => actionInvokeCount++);

            Assert.That (actionInvokeCount, Is.EqualTo (1));
        }

        private struct TestTask : ITask<Unit, Unit>
        {
            public Unit Run (Unit _) => default;
        }
    }
}
