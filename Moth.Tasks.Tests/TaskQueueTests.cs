using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Moth.Tasks.Tests
{
    public class TaskQueueTests
    {
        [Test]
        public void EnqueueITask ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new PutValueTask ());
        }

        [Test]
        public void EnqueueAction ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => { });
        }

        void AssertTaskResult (Exception ex, int correctValue, TaskResult result)
        {
            Assert.IsNull (ex, ex?.Message);

            Assert.AreEqual (correctValue, result.Value);
        }

        [Test]
        public void EnqueueAndTryRun_ITask ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            Assert.AreEqual (queue.Count, 1);

            queue.RunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        [Test]
        public void EnqueueAndTryRun_Action ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (() => result.Value = value);

            Assert.AreEqual (queue.Count, 1);

            queue.RunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        [Test]
        public void EnqueueAndTryRun_ActionWithArg ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (x => result.Value = x, value);

            Assert.AreEqual (queue.Count, 1);

            queue.RunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        [Test]
        public void EnqueueAndTryRun_Profiler ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            Profiler profiler = new Profiler ();

            queue.RunNextTask (profiler, out Exception ex);

            AssertTaskResult (ex, value, result);

            Assert.IsTrue (profiler.HasRun);
            Assert.IsFalse (profiler.Running);
            Assert.NotNull (profiler.LastTask);
        }

        [Test]
        public void EnqueueAndTryRun_IDisposable ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult runResult = new TaskResult ();
            int runValue = 42;

            TaskResult disposeResult = new TaskResult ();
            int disposeValue = 21;

            queue.Enqueue (new PutValueAndDisposeTask (runValue, runResult, disposeValue, disposeResult));

            queue.RunNextTask (out Exception ex);

            AssertTaskResult (ex, runValue, runResult);
            AssertTaskResult (ex, disposeValue, disposeResult);
        }

        [Test]
        public void EnqueueWithTaskHandle ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            Assert.IsFalse (handle.IsComplete);
        }

        [Test]
        public void EnqueueAndWait ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result), out TaskHandle handle);

            Assert.IsFalse (handle.IsComplete);

            AutoResetEvent workerReadyEvent = new AutoResetEvent (false);

            Thread worker = new Thread (() =>
            {
                workerReadyEvent.Set ();

                queue.RunNextTask ();
            });

            worker.Start ();

            workerReadyEvent.WaitOne ();
            handle.WaitForCompletion ();

            Assert.IsTrue (handle.IsComplete);
        }

        class TaskResult
        {
            public int Value { get; set; }
        }

        struct Task : ITask
        {
            public void Run ()
            {

            }
        }

        readonly struct PutValueTask : ITask
        {
            private readonly int value;
            private readonly TaskResult result;

            public PutValueTask (int value, TaskResult result)
            {
                this.value = value;
                this.result = result;
            }

            public void Run ()
            {
                result.Value = value;
            }
        }

        readonly struct PutValueAndDisposeTask : ITask, IDisposable
        {
            private readonly int runValue;
            private readonly TaskResult runResult;
            private readonly int disposeValue;
            private readonly TaskResult disposeResult;

            public PutValueAndDisposeTask (int runValue, TaskResult runResult, int disposeValue, TaskResult disposeResult)
            {
                this.runValue = runValue;
                this.runResult = runResult;
                this.disposeValue = disposeValue;
                this.disposeResult = disposeResult;
            }

            public void Run ()
            {
                runResult.Value = runValue;
            }

            public void Dispose ()
            {
                disposeResult.Value = disposeValue;
            }
        }

        class Profiler : IProfiler
        {
            public bool HasRun { get; private set; }

            public bool Running { get; private set; }

            public string LastTask { get; private set; }

            public void BeginTask (string task)
            {
                Running = true;

                HasRun = true;

                LastTask = task;
            }

            public void EndTask ()
            {
                Running = false;
            }
        }
    }
}