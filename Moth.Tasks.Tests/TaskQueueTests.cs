using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;

namespace Moth.Tasks.Tests
{
    public class TaskQueueTests
    {
        /// <summary>
        /// Enqueues an <see cref="ITask"/> in an empty <see cref="TaskQueue"/>.
        /// </summary>
        [Test]
        public void EnqueueITask ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task ());

            Assert.AreEqual (1, queue.Count);
        }

        /// <summary>
        /// Enqueues an <see cref="Action"/> in an empty <see cref="TaskQueue"/>.
        /// </summary>
        [Test]
        public void EnqueueAction ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => { });

            Assert.AreEqual (1, queue.Count);
        }

        /// <summary>
        /// Enqueues multiple tasks, asserts that the internal <see cref="TaskQueue.taskData"/> array expands accordingly.
        /// Also runs a task, to ensure that the internal <see cref="TaskQueue.firstTask"/> index points correctly.
        /// </summary>
        [Test]
        public void EnqueueTask_ExpandTaskData ()
        {
            const int startDataCapacity = 8;
            TaskQueue queue = new TaskQueue (1, startDataCapacity);

            AssertTaskDataLength (startDataCapacity);

            queue.Enqueue (new Task ());

            AssertTaskDataLength (startDataCapacity);

            queue.Enqueue (new Task ());

            AssertTaskDataLength (startDataCapacity * 2);

            queue.TryRunNextTask ();

            AssertFirstTaskIndex (startDataCapacity);

            queue.Enqueue (new Task ());

            AssertFirstTaskIndex (0);
            AssertTaskDataLength (startDataCapacity * 2);

            queue.Enqueue (new Task ());

            AssertTaskDataLength (startDataCapacity * 4);

            void AssertTaskDataLength (int expectedValue)
            {
                int queue_taskData_Length = queue.GetPrivateValue<byte[]> ("taskData").Length;
                Assert.AreEqual (expectedValue, queue_taskData_Length);
            }

            void AssertFirstTaskIndex (int expectedValue)
            {
                int queue_firstTask = queue.GetPrivateValue<int> ("firstTask");
                Assert.AreEqual (expectedValue, queue_firstTask);
            }
        }

        /// <summary>
        /// Enqueues and runs multiple tasks, and asserts that the internal <see cref="TaskQueue.firstTask"/> index points to the right task.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_AssertValidData ()
        {
            TaskQueue queue = new TaskQueue (2, 8);

            Queue<TaskResult> results = new Queue<TaskResult> ();
            int nextTaskIndex = 1;

            int nextTaskResult = 1;

            int putValueUnmanagedDataSize = sizeof (int); // Should be sizeof (int), as PutValue contains an int and a reference.

            EnqueueTask ();
            EnqueueTask ();

            // Run the first task
            AssertNextTaskResult ();

            // queue.firstTask should now be equal putValueTaskDataIndices, pointing to the second task enqueued
            AssertTestFirstTaskIndex (putValueUnmanagedDataSize);

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

                Assert.AreEqual (expectedResult, results.Dequeue ().Value);
            }

            void AssertTestFirstTaskIndex (int expectedValue)
            {
                int queue_firstTask = queue.GetPrivateValue<int> ("firstTask");
                Assert.AreEqual (expectedValue, queue_firstTask);
            }
        }

        void AssertTaskResult (Exception ex, int correctValue, TaskResult result)
        {
            Assert.IsNull (ex, ex?.Message);

            Assert.AreEqual (correctValue, result.Value);
        }

        /// <summary>
        /// Enqueues and runs an <see cref="ITask"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ITask ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            Assert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

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

            Assert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

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

            Assert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (out Exception ex);

            Assert.AreEqual (queue.Count, 0);

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

            Assert.IsTrue (profiler.HasRun);
            Assert.IsFalse (profiler.Running);
            Assert.NotNull (profiler.LastTask);
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
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask.Run"/> method. Asserts that the exception is returned from <see cref="TaskQueue.TryRunNextTask(out Exception)"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Exception ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            queue.TryRunNextTask (out Exception ex);

            Assert.IsInstanceOf<InvalidOperationException> (ex);
        }

        /// <summary>
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask.Run"/> method, while running an <see cref="IProfiler"/>. Asserts profiling is stopped correctly.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ExceptionWhileProfiling ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            Profiler profiler = new Profiler ();

            queue.TryRunNextTask (out Exception ex, profiler);

            Assert.IsInstanceOf<InvalidOperationException> (ex);

            Assert.IsTrue (profiler.HasRun);
            Assert.IsFalse (profiler.Running);
            Assert.NotNull (profiler.LastTask);
        }

        /// <summary>
        /// Enqueues a task with <see cref="TaskQueue.Enqueue{T}(in T, out TaskHandle)"/> and asserts that the <see cref="TaskHandle"/> returned is valid.
        /// </summary>
        [Test]
        public void EnqueueWithTaskHandle ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            Assert.IsFalse (handle.IsComplete);
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

            Assert.IsFalse (handle.IsComplete);

            AutoResetEvent workerReadyEvent = new AutoResetEvent (false);

            Thread worker = new Thread (() =>
            {
                workerReadyEvent.Set ();

                queue.TryRunNextTask ();
            });

            worker.Start ();

            workerReadyEvent.WaitOne ();
            handle.WaitForCompletion ();

            Assert.IsTrue (handle.IsComplete);

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

            Assert.IsFalse (handle.IsComplete);

            AutoResetEvent workerReadyEvent = new AutoResetEvent (false);

            Thread worker = new Thread (() =>
            {
                workerReadyEvent.Set ();

                queue.TryRunNextTask ();
            });

            worker.Start ();

            workerReadyEvent.WaitOne ();
            handle.WaitForCompletion ();

            Assert.IsTrue (handle.IsComplete);

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

            Assert.IsFalse (queue.TryRunNextTask ());
        }

        /// <summary>
        /// Asserts that <see cref="TaskQueue.TryRunNextTask"/> will return <see langword="true"/> when the <see cref="TaskQueue"/> has a task enqueued and ready to be ran.
        /// </summary>
        [Test]
        public void TryRun_ReturnTrue ()
        {
            TaskQueue queue = new TaskQueue ();

            queue.Enqueue (new Task ());

            Assert.IsTrue (queue.TryRunNextTask ());
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

            Assert.AreEqual (0, queue.Count);

            foreach (Exception ex in exceptions)
            {
                Assert.IsInstanceOf<InvalidOperationException> (ex);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual (i, disposeResults[i].Value);
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

            Assert.AreEqual (0, queue.Count);

            Assert.AreEqual (disposeResults.Length, exceptions.Count);

            foreach (Exception ex in exceptions)
            {
                Assert.IsInstanceOf<InvalidOperationException> (ex);
            }
            
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual (i, disposeResults[i].Value);
            }
        }

        /// <summary>
        /// Disposes a <see cref="TaskQueue"/>, asserting that the internal <see cref="TaskQueue.disposed"/> flag is set. Also asserts that following calls to <see cref="TaskQueue.Enqueue{T}(in T)"/> & <see cref="TaskQueue.Enqueue{T}(in T, out TaskHandle)"/> throw an <see cref="ObjectDisposedException"/>.
        /// </summary>
        [Test]
        public void Dispose ()
        {
            TaskQueue queue = new TaskQueue ();

            Assert.IsFalse (GetPrivateValue<bool> ("disposed"));

            queue.Dispose ();

            Assert.IsTrue (GetPrivateValue<bool> ("disposed"));

            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task ()));
            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task (), out _));

            T GetPrivateValue<T> (string fieldName) => (T)typeof (TaskQueue).GetField (fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue (queue);
        }

        [Test]
        public void Dispose_GC ()
        {
            TaskQueue queue = new TaskQueue ();

            TrackedObject obj = new TrackedObject ();

            queue.Enqueue ((object obj) => obj.ToString (), obj);

            WeakReference objRef = new WeakReference (obj);
            obj = null;

            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true);

            Assert.IsTrue (objRef.IsAlive);

            queue.RunNextTask ();

            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true);

            Assert.IsFalse (objRef.IsAlive);
        }

        class TrackedObject
        {

        }

        /// <summary>
        /// Object for storing the result of a <see cref="PutValueTask"/> or <see cref="PutValueAndDisposeTask"/>.
        /// </summary>
        class TaskResult
        {
            public int Value { get; set; }
        }

        /// <summary>
        /// Mock task, with size of one Data Index
        /// </summary>
        struct Task : ITask
        {
            public IntPtr MockData;

            public void Run ()
            {

            }
        }

        /// <summary>
        /// Stores a value in a <see cref="TaskResult"/> object.
        /// </summary>
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

        /// <summary>
        /// Stores a value in a <see cref="TaskResult"/> object on <see cref="ITask.Run"/>, and another value in another <see cref="TaskResult"/> object on <see cref="IDisposable.Dispose"/>.
        /// </summary>
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

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> on <see cref="IDisposable.Dispose"/>.
        /// </summary>
        readonly struct ExceptionOnDisposeTask : ITask, IDisposable
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