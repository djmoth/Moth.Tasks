using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Moth.Tasks.Tests
{
    public class WorkerGroupTests
    {
        [Test]
        public void Work ()
        {
            TaskQueue queue = new TaskQueue ();

            // Enqueue a task which sets an event
            AutoResetEvent a = new AutoResetEvent (false), b = new AutoResetEvent (false);
            queue.Enqueue ((AutoResetEvent wait, AutoResetEvent set) =>
            {
                wait.WaitOne ();
                set.Set ();
            }, a, b);

            using (Worker worker = new Worker (queue, disposeTaskQueue: true))
            {
                // The worker must execute the enqueued task for the test to continue and pass. Note MaxTime (100) attribute of method.
                waitEvent.WaitOne ();
            }
        }

        [Test]
        public void Disposable ()
        {
            TaskQueue queue = new TaskQueue ();

            using (Worker worker = new Worker (queue, disposeTaskQueue: true)) // Pass true for disposeTaskQueue
            {

            }

            // Worker.Dispose must dispose of queue if disposeTaskQueue constructor parameter is true
            Assert.IsTrue (queue.GetPrivateValue<bool> ("disposed"));
        }
    }
}
