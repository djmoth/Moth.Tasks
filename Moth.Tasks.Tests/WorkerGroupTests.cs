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
        [MaxTime (100)]
        public void Work ()
        {
            TaskQueue queue = new TaskQueue ();

            AutoResetEvent
                A_Wait_B_Set = new AutoResetEvent (false), 
                A_Set_B_Wait = new AutoResetEvent (false), 
                aDone = new AutoResetEvent (false), 
                bDone = new AutoResetEvent (false);

            // Task A
            queue.Enqueue ((AutoResetEvent A_Wait_B_Set, AutoResetEvent A_Set_B_Wait, AutoResetEvent done) =>
            {
                A_Wait_B_Set.WaitOne (); // Wait for Task B to set
                A_Set_B_Wait.Set (); // Set so Task B can continue
                aDone.Set ();
            }, A_Wait_B_Set, A_Set_B_Wait, aDone);

            // Task B
            queue.Enqueue ((AutoResetEvent A_Wait_B_Set, AutoResetEvent A_Set_B_Wait, AutoResetEvent done) =>
            {
                A_Wait_B_Set.Set (); // Set so Task A can continue
                A_Set_B_Wait.WaitOne (); // Wait for task A to set
                bDone.Set ();
            }, A_Wait_B_Set, A_Set_B_Wait, bDone);

            int workerCount = 2; // Must be >= 2

            using (WorkerGroup workers = new WorkerGroup (workerCount, queue, disposeTaskQueue: true))
            {
                // The WorkerGroup must run the two tasks in parallel for them both to complete, as they depend on unlocking eachother
                WaitHandle.WaitAll (new[] { aDone, bDone });
            }
        }

        [Test]
        public void Disposable ()
        {
            TaskQueue queue = new TaskQueue ();

            using (WorkerGroup worker = new WorkerGroup (1, queue, disposeTaskQueue: true)) // Pass true for disposeTaskQueue
            {

            }

            // Worker.Dispose must dispose of queue if disposeTaskQueue constructor parameter is true
            Assert.IsTrue (queue.GetPrivateValue<bool> ("disposed"));
        }
    }
}
