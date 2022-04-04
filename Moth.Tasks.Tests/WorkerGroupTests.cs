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
        [TestCase (1), TestCase (2), TestCase (3), TestCase (4)]
        [Timeout (1000)]
        public void Work (int workerCount)
        {
            using (WorkerGroup workers = new WorkerGroup (workerCount, new TaskQueue ()))
            {
                EngageAllWorkers (workers);
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

        [Test]
        public void WorkerCount_Get ()
        {
            int workerCount = 1;

            using (WorkerGroup workers = new WorkerGroup (workerCount, new TaskQueue ()))
            {
                Assert.AreEqual (workerCount, workers.WorkerCount);
            }
        }

        [Test]
        public void WorkerCount_Set_Increase ()
        {
            int workerCount = 1;

            using (WorkerGroup workers = new WorkerGroup (workerCount, new TaskQueue ()))
            {
                workerCount++;
                workers.WorkerCount = workerCount;

                Assert.AreEqual (workerCount, workers.WorkerCount);

                // Are all workers actually working?
                EngageAllWorkers (workers);
            }
        }

        [Test]
        public void WorkerCount_Set_Decrease ()
        {
            int workerCount = 2;

            using (WorkerGroup workers = new WorkerGroup (workerCount, new TaskQueue ()))
            {
                workerCount--;
                workers.WorkerCount = workerCount;

                Assert.AreEqual (workerCount, workers.WorkerCount);

                // Are all workers actually working?
                EngageAllWorkers (workers);
            }
        }

        /// <summary>
        /// Engages all Workers in a WorkerGroup.
        /// </summary>
        private void EngageAllWorkers (WorkerGroup workers)
        {
            int workerCount = workers.WorkerCount;
            TaskQueue tasks = workers.Tasks;

            // Set when all tasks have been enqueued
            ManualResetEvent startEvent = new ManualResetEvent (false);

            // Each worker must set it's own event
            ManualResetEvent[] stepEvents = new ManualResetEvent[workerCount];

            // Is set by each worker when it is done.
            ManualResetEvent[] workerDoneEvents = new ManualResetEvent[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                stepEvents[i] = new ManualResetEvent (false);
                workerDoneEvents[i] = new ManualResetEvent (false);

                tasks.Enqueue ((int index) =>
                {
                    startEvent.WaitOne ();

                    stepEvents[index].Set ();

                    WaitHandle.WaitAll (stepEvents);

                    workerDoneEvents[index].Set ();
                }, i);
            }

            startEvent.Set ();

            // Wait for all workers to finish
            WaitHandle.WaitAll (workerDoneEvents);
        }
    }
}
