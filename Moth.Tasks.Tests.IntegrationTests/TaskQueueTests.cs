namespace Moth.Tasks.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    public class TaskQueueTests
    {
        /// <summary>
        /// Enqueues an <see cref="ITask<Unit, Unit>"/> in an empty <see cref="TaskQueue"/>.
        /// </summary>
        [Test]
        public void EnqueueITask ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task ());

            ClassicAssert.AreEqual (1, queue.Count);
        }

        /// <summary>
        /// Enqueues an <see cref="Action"/> in an empty <see cref="TaskQueue"/>.
        /// </summary>
        [Test]
        public void EnqueueAction ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => { });

            ClassicAssert.AreEqual (1, queue.Count);
        }

        /// <summary>
        /// Enqueues and runs multiple tasks, and asserts that the internal <see cref="TaskQueue.firstTask"/> index points to the right task.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_AssertValidData ()
        {
            TaskQueue queue = new TaskQueue (2, 8, 8);

            Queue<TaskResult> results = new Queue<TaskResult> ();
            int nextTaskIndex = 1;

            int nextTaskResult = 1;

            EnqueueTask ();
            EnqueueTask ();

            // Run the first task
            AssertNextTaskResult ();

            // Enqueue another task, the contents of queue.taskData should now be moved to the front to make space
            EnqueueTask ();

            // Check if task data matches
            AssertNextTaskResult ();
            AssertNextTaskResult ();

            void EnqueueTask ()
            {
                TaskResult result = new TaskResult ();
                int value = nextTaskIndex++;

                results.Enqueue (result);
                queue.Enqueue (new PutValueTask (value, result));
            }

            void AssertNextTaskResult ()
            {
                queue.TryRunNextTask ();

                int expectedResult = nextTaskResult++;

                ClassicAssert.AreEqual (expectedResult, results.Dequeue ().Value);
            }
        }

        void AssertTaskResult (Exception ex, int correctValue, TaskResult result)
        {
            ClassicAssert.IsNull (ex, ex?.Message);

            ClassicAssert.AreEqual (correctValue, result.Value);
        }

        /// <summary>
        /// Enqueues and runs an <see cref="ITask<Unit, Unit>"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ITask ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            ClassicAssert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            ClassicAssert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        /// <summary>
        /// Enqueues and runs an <see cref="Action"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Action ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (() => result.Value = value);

            ClassicAssert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            ClassicAssert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        /// <summary>
        /// Enqueues and runs an <see cref="Action"/> with a supplied argument.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ActionWithArg ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (x => result.Value = x, value);

            ClassicAssert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            ClassicAssert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        /// <summary>
        /// Enqueues and runs a task, while running an <see cref="IProfiler"/>
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Profiler ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            Profiler profiler = new Profiler ();

            queue.TryRunNextTask (out Exception ex, profiler);

            AssertTaskResult (ex, value, result);

            ClassicAssert.IsTrue (profiler.HasRun);
            ClassicAssert.IsFalse (profiler.Running);
            ClassicAssert.NotNull (profiler.LastTask);
        }

        /// <summary>
        /// Enqueues and runs a task implementing <see cref="IDisposable"/>, asserting that <see cref="IDisposable"/> is run.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_IDisposable ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult runResult = new TaskResult ();
            int runValue = 42;

            TaskResult disposeResult = new TaskResult ();
            int disposeValue = 21;

            queue.Enqueue (new PutValueAndDisposeTask (runValue, runResult, disposeValue, disposeResult));

            queue.TryRunNextTask (out Exception ex);

            AssertTaskResult (ex, runValue, runResult);
            AssertTaskResult (ex, disposeValue, disposeResult);
        }

        /// <summary>
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask<Unit, Unit>.Run"/> method. Asserts that the exception is returned from <see cref="TaskQueue.TryRunNextTask(out Exception)"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Exception ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            queue.TryRunNextTask (out Exception ex);

            ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);
        }

        /// <summary>
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask<Unit, Unit>.Run"/> method, while running an <see cref="IProfiler"/>. Asserts profiling is stopped correctly.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ExceptionWhileProfiling ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            Profiler profiler = new Profiler ();

            queue.TryRunNextTask (out Exception ex, profiler);

            ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);

            ClassicAssert.IsTrue (profiler.HasRun);
            ClassicAssert.IsFalse (profiler.Running);
            ClassicAssert.NotNull (profiler.LastTask);
        }

        /// <summary>
        /// Enqueues a task with <see cref="TaskQueue.Enqueue{T}(in T, out TaskHandle)"/> and asserts that the <see cref="TaskHandle"/> returned is valid.
        /// </summary>
        [Test]
        public void EnqueueWithTaskHandle ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            ClassicAssert.IsFalse (handle.IsComplete);
        }

        /// <summary>
        /// Enqueues a task and runs it in a seperate thread, while waiting for its completion with <see cref="TaskHandle.WaitForCompletion"/>.
        /// </summary>
        [Test]
        public void EnqueueAndWait ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result), out TaskHandle handle);

            ClassicAssert.IsFalse (handle.IsComplete);

            AutoResetEvent workerReadyEvent = new AutoResetEvent (false);

            Thread worker = new Thread (() =>
            {
                workerReadyEvent.Set ();

                queue.TryRunNextTask ();
            });

            worker.Start ();

            workerReadyEvent.WaitOne ();
            handle.WaitForCompletion ();

            ClassicAssert.IsTrue (handle.IsComplete);

            AssertTaskResult (null, value, result);
        }

        /// <summary>
        /// Enqueues a task implementing <see cref="IDisposable"/>, and asserts that its <see cref="IDisposable.Dispose"/> method is called from <see cref="DisposableTaskWithHandle{T}.Dispose"/>.
        /// </summary>
        [Test]
        public void EnqueueAndWait_IDisposable ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult runResult = new TaskResult ();
            int runValue = 42;

            TaskResult disposeResult = new TaskResult ();
            int disposeValue = 21;

            queue.Enqueue (new PutValueAndDisposeTask (runValue, runResult, disposeValue, disposeResult), out TaskHandle handle);

            ClassicAssert.IsFalse (handle.IsComplete);

            AutoResetEvent workerReadyEvent = new AutoResetEvent (false);

            Thread worker = new Thread (() =>
            {
                workerReadyEvent.Set ();

                queue.TryRunNextTask ();
            });

            worker.Start ();

            workerReadyEvent.WaitOne ();
            handle.WaitForCompletion ();

            ClassicAssert.IsTrue (handle.IsComplete);

            AssertTaskResult (null, runValue, runResult);
            AssertTaskResult (null, disposeValue, disposeResult);
        }

        /// <summary>
        /// Enqueues and runs a task, calling <see cref="TaskHandle.WaitForCompletion"/> after its supposed completion. Asserts that the call does not hang.
        /// </summary>
        [Test]
        public void EnqueueAndWait_Completed ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            queue.TryRunNextTask ();

            handle.WaitForCompletion ();
        }

        /// <summary>
        /// Asserts that <see cref="TaskQueue.TryRunNextTask"/> will return <see langword="false"/> when the <see cref="TaskQueue"/> is empty.
        /// </summary>
        [Test]
        public void TryRun_ReturnFalse ()
        {
            TaskQueue queue = new TaskQueue ();

            ClassicAssert.IsFalse (queue.TryRunNextTask ());
        }

        /// <summary>
        /// Asserts that <see cref="TaskQueue.TryRunNextTask"/> will return <see langword="true"/> when the <see cref="TaskQueue"/> has a task enqueued and ready to be ran.
        /// </summary>
        [Test]
        public void TryRun_ReturnTrue ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task ());

            ClassicAssert.IsTrue (queue.TryRunNextTask ());
        }

        /// <summary>
        /// Enqueues a series of alternating <see cref="Task"/> and <see cref="PutValueAndDisposeTask"/>, asserting that non-disposable tasks will not disrupt the execution of <see cref="PutValueAndDisposeTask.Dispose"/>.
        /// </summary>
        [Test]
        public void Clear_Disposable ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult[] disposeResults = new TaskResult[10];

            for (int i = 0; i < disposeResults.Length; i++)
            {
                queue.Enqueue (new Task ()); // Dummy task which is not disposable

                disposeResults[i] = new TaskResult ();

                queue.Enqueue (new PutValueAndDisposeTask (0, null, i, disposeResults[i]));
            }

            List<Exception> exceptions = new List<Exception> ();

            queue.Clear (ex => exceptions.Add (ex));

            ClassicAssert.AreEqual (0, queue.Count);

            foreach (Exception ex in exceptions)
            {
                ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);
            }

            for (int i = 0; i < 10; i++)
            {
                ClassicAssert.AreEqual (i, disposeResults[i].Value);
            }
        }

        /// <summary>
        /// Enqueues a series of alternating <see cref="ExceptionOnDisposeTask"/> and <see cref="PutValueAndDisposeTask"/>, asserting that the exceptions thrown from <see cref="ExceptionOnDisposeTask.Dispose"/> will not disrupt the execution of <see cref="PutValueAndDisposeTask.Dispose"/>.
        /// </summary>
        [Test]
        public void Clear_Exception ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult[] disposeResults = new TaskResult[10];

            for (int i = 0; i < disposeResults.Length; i++)
            {
                queue.Enqueue (new ExceptionOnDisposeTask ());

                disposeResults[i] = new TaskResult ();

                queue.Enqueue (new PutValueAndDisposeTask (0, null, i, disposeResults[i]));
            }

            List<Exception> exceptions = new List<Exception> ();

            queue.Clear (ex => exceptions.Add (ex));

            ClassicAssert.AreEqual (0, queue.Count);

            ClassicAssert.AreEqual (disposeResults.Length, exceptions.Count);

            foreach (Exception ex in exceptions)
            {
                ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);
            }

            for (int i = 0; i < 10; i++)
            {
                ClassicAssert.AreEqual (i, disposeResults[i].Value);
            }
        }

        /// <summary>
        /// Disposes a <see cref="TaskQueue"/>, asserting that the internal <see cref="TaskQueue.disposed"/> flag is set. Also asserts that following calls to <see cref="TaskQueue.Enqueue{T}(in T)"/> & <see cref="TaskQueue.Enqueue{T}(in T, out TaskHandle)"/> throw an <see cref="ObjectDisposedException"/>.
        /// </summary>
        [Test]
        public void Dispose ()
        {
            TaskQueue queue = new TaskQueue ();

            ClassicAssert.IsFalse (GetPrivateValue<bool> ("disposed"));

            queue.Dispose ();

            ClassicAssert.IsTrue (GetPrivateValue<bool> ("disposed"));

            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task ()));
            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task (), out _));

            T GetPrivateValue<T> (string fieldName) => (T)typeof (TaskQueue).GetField (fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).GetValue (queue);
        }

        [Test]
        public void Dispose_GC ()
        {
            TaskQueue queue = new TaskQueue ();

            WeakReference objRef = new WeakReference (null);

            queue.Enqueue ((queue, objRef) =>
            {
                object obj = new object ();
                objRef.Target = obj;
                queue.Enqueue ((obj) => obj.ToString (), obj);
            }, queue, objRef);

            queue.RunNextTask ();

            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers ();

            ClassicAssert.IsTrue (objRef.IsAlive);

            queue.RunNextTask ();

            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers ();

            ClassicAssert.IsFalse (objRef.IsAlive);
        }

        class TrackedObject
        {
            public object Obj;
        }

        /// <summary>
        /// Object for storing the result of a <see cref="PutValueTask"/> or <see cref="PutValueAndDisposeTask"/>.
        /// </summary>
        class TaskResult
        {
            public int Value { get; set; }
        }

        /// <summary>
        /// Test task
        /// </summary>
        struct Task : ITask<Unit, Unit>
        {
            public nint Data;

            public void Run ()
            {

            }
        }

        /// <summary>
        /// Stores a value in a <see cref="TaskResult"/> object.
        /// </summary>
        readonly struct PutValueTask : ITask<Unit, Unit>
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

        /// <summary>
        /// Stores a value in a <see cref="TaskResult"/> object on <see cref="ITask<Unit, Unit>.Run"/>, and another value in another <see cref="TaskResult"/> object on <see cref="IDisposable.Dispose"/>.
        /// </summary>
        readonly struct PutValueAndDisposeTask : ITask<Unit, Unit>, IDisposable
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

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> on <see cref="IDisposable.Dispose"/>.
        /// </summary>
        readonly struct ExceptionOnDisposeTask : ITask<Unit, Unit>, IDisposable
        {
            public void Run ()
            {

            }

            public void Dispose ()
            {
                throw new InvalidOperationException ();
            }
        }

        /// <summary>
        /// Mock profiler, with flags for checking if <see cref="BeginTask(string)"/> and <see cref="EndTask"/> has been called.
        /// </summary>
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