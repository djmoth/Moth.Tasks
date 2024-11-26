using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Moth.Tasks.Tests
{
    public class WorkerTests
    {
        [Test]
        [Timeout (100)]
        public void Work ()
        {
            TaskQueue queue = new TaskQueue ();

            // Enqueue a task which sets an event
            AutoResetEvent waitEvent = new AutoResetEvent (false);
            queue.Enqueue ((AutoResetEvent e) => e.Set (), waitEvent);

            using (Worker worker = new Worker (queue, true, true))
            {
                // The worker must execute the enqueued task for the test to continue and pass. Note Timeout (100) attribute of method.
                waitEvent.WaitOne (); 
            }
        }

        [Test]
        public void Disposable ()
        {
            TaskQueue queue = new TaskQueue ();

            using (Worker worker = new Worker (queue, true, true)) // Pass true for disposeTaskQueue
            {
                
            }

            // Worker.Dispose must dispose of queue if disposeTaskQueue constructor parameter is true
            Assert.Fail ();
        }
    }
}
