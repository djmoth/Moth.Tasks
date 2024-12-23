namespace Moth.Tasks.Tests.IntegrationTests
{
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Timers;

    [TestFixture ([typeof (Unit), typeof (Unit)])]
    public class TaskQueueWithTaskMetadataCacheTests<TArg, TResult>
    {
        [Test]
        public void Count_WhenQueueIsEmpty_IsZero ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Count_WhenTaskEnqueued_Increments ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            queue.Enqueue (new TestTask ());

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public void Enqueue_TaskWithHandle_EnqueuesTask ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        [Test]
        public void Enqueue_TaskWithHandle_ReturnsValidHandle ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.That (handle.IsValid, Is.True);
        }

        [Test]
        public void Enqueue_DisposableTaskWithHandle_ReturnsValidHandle ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task, out TaskHandle handle);

            Assert.That (handle.IsValid, Is.True);
        }

        [CancelAfter (1000)]
        public void RunNextTask_OneTaskEnqueuedAndMethodCalled_RunsTaskWithArgAndReturnsResult (CancellationToken token)
        {
            Assume.That (token.CanBeCanceled, Is.True);

            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            queue.Enqueue (task);

            TResult resultToReturn;

            if (typeof (TResult) == typeof (object))
                resultToReturn = (TResult)new object ();
            else if (typeof (TResult) == typeof (Unit))
                resultToReturn = default;
            else if (typeof (TResult) == typeof (int))
                resultToReturn = (TResult)(object)123;
            else
                throw new NotSupportedException ();

            TArg arg;

            if (typeof (TArg) == typeof (object))
                arg = (TArg)new object ();
            else if (typeof (TArg) == typeof (Unit))
                arg = default;
            else if (typeof (TArg) == typeof (int))
                arg = (TArg)(object)42;
            else
                throw new NotSupportedException ();

            TResult returnedResult = queue.RunNextTask (arg, profiler: null, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            Assert.That (mockTestTaskMetadata.SuppliedArgs, Is.EqualTo (new TArg[] { arg }));
            Assert.That (returnedResult, Is.EqualTo (resultToReturn));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TwoTasksEnqueuedAndMethodCalled_RunsFirstTaskOnly (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };
            DisposableTestTask disposableTask = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (disposableTask);

            // Run first TestTask with default argument with no profiler and discard result
            queue.RunNextTask (arg: default, profiler: null, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            Assert.Multiple (() =>
            {
                // Assert that only the first TestTask was run
                Assert.That (mockTestTaskMetadata.SuppliedArgs.Count, Is.EqualTo (1));
                Assert.That (mockDisposableTestTaskMetadata.SuppliedArgs.Count, Is.Zero);
            });
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_TaskThrowsException_RunsTaskAndCatchesException (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask ();

            // The Mock Task Metadata will now throw an exception on Run
            mockTestTaskMetadata.ExceptionToThrowOnRun = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (default, out Exception exception, null, token);

            Assume.That (token.IsCancellationRequested, Is.False);

            // Verify that the exception was caught and returned
            Assert.That (exception, Is.SameAs (mockTestTaskMetadata.ExceptionToThrowOnRun));
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfiler_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            mockTaskDataStore.Setup (store => store.Dequeue (mockTestTaskMetadata)).Returns (task);

            queue.Enqueue (task);
            queue.RunNextTask (arg: default, profiler: mockProfiler.Object, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTask)), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        [CancelAfter (1000)]
        public unsafe void RunNextTask_WithProfilerAndTaskThrowsException_CallsBeginTaskWithTaskTypeFullNameAndStops (CancellationToken token)
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            mockTestTaskMetadata.ExceptionToThrowOnRun = new Exception ();

            queue.Enqueue (task);
            queue.RunNextTask (arg: default, profiler: mockProfiler.Object, token: token);

            Assume.That (token.IsCancellationRequested, Is.False);

            mockProfiler.Verify (profiler => profiler.BeginTask (typeof (TestTask)), Times.Once);
            mockProfiler.Verify (profiler => profiler.EndTask (), Times.Once);
            mockProfiler.VerifyNoOtherCalls ();
        }

        [Test]
        public unsafe void TryRunNextTask_WhenQueueIsEmpty_ReturnsFalse ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            bool taskWasRun = queue.TryRunNextTask (default, out TResult result);

            Assert.That (taskWasRun, Is.False);
        }

        [Test]
        public void Clear_WhenQueueIsEmpty_ClearsDependencies ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();
            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_NoDisposableTasks_ClearsQueueWhileSkippingTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);
            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_WithOnlyDisposableTasks_ClearsQueueWhileDisposingTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            DisposableTestTask task = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (task);
            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
            Assert.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (2));
        }

        [Test]
        public void Clear_WithMixedTasks_ClearsQueueSkipsNonDisposableTasksAndDisposesDisposableTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            TestTask task = new TestTask { };
            DisposableTestTask disposableTask = new DisposableTestTask { };

            queue.Enqueue (task);
            queue.Enqueue (disposableTask);
            queue.Enqueue (task);
            queue.Enqueue (disposableTask);

            queue.Clear ();

            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Exactly (2));

            Assert.That (queue.Count, Is.EqualTo (0));
            Assert.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (2));
        }

        [Test]
        public void Clear_WithExceptionHandlerDisposableTaskThrowingException_ReportsExceptionToExceptionHandlerAndContinuesDisposingTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            DisposableTestTask disposableTask = new DisposableTestTask { };
            TestTask task = new TestTask { };

            queue.Enqueue (disposableTask);
            queue.Enqueue (task);

            Exception exceptionToThrow = new Exception ();
            mockDisposableTestTaskMetadata.ExceptionToThrowOnDispose = exceptionToThrow;
            List<Exception> exceptionsThrown = new List<Exception> ();

            queue.Clear (exceptionsThrown.Add);

            Assume.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (1));

            // Verify that Clear continued and skipped the non-disposable TestTask
            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Once);

            Assert.That (exceptionsThrown, Is.EquivalentTo (new Exception[] { mockDisposableTestTaskMetadata.ExceptionToThrowOnDispose }));
        }

        [Test]
        public void Clear_WithoutExceptionHandlerDisposableTaskThrowingException_ThrowsExceptionAndStopsDisposingTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            DisposableTestTask disposableTask = new DisposableTestTask { };
            TestTask task = new TestTask { };

            queue.Enqueue (disposableTask);
            queue.Enqueue (task);

            Exception exceptionToThrow = new Exception ();
            mockDisposableTestTaskMetadata.ExceptionToThrowOnDispose = exceptionToThrow;

            Assert.That (() => queue.Clear (), Throws.InstanceOf<Exception> ());

            Assume.That (mockDisposableTestTaskMetadata.DisposeCallCount, Is.EqualTo (1));

            // Verify that Clear stopped before skipping the non-disposable TestTask
            mockTaskDataStore.Verify (store => store.Skip (mockTestTaskMetadata), Times.Never);
        }

        [Test]
        public void Dispose_WhenCalledOnce_ClearsTasks ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        [Test]
        public void Dispose_WhenCalledTwice_ClearsTasksThenDoesNothing ()
        {
            TaskQueue<TArg, TResult> queue = new TaskQueue<TArg, TResult> ();

            queue.Dispose ();
            queue.Dispose ();

            mockTaskDataStore.Verify (store => store.Clear (), Times.Once);
            mockTaskHandleManager.Verify (manager => manager.Clear (), Times.Once);

            Assert.That (queue.Count, Is.EqualTo (0));
        }

        private unsafe struct TestTask : ITask<TArg, TResult>
        {
            public TResult Run (TArg arg) => default;
        }

        private unsafe struct DisposableTestTask : ITask<TArg, TResult>, IDisposable
        {
            public TResult Run (TArg arg) => default;

            public void Dispose () { }
        }

        /// <summary>
        /// Enqueues an <see cref="ITask{Unit, Unit}"/> in an empty <see cref="TaskQueue{Unit, Unit}"/>.
        /// </summary>
        [Test]
        public void Enqueue_IncrementsCount ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (new Task ());

            Assert.That (queue.Count, Is.EqualTo (1));
        }

        /// <summary>
        /// Enqueues and runs multiple tasks, and asserts that the internal <see cref="TaskQueue<Unit, Unit>.firstTask"/> index points to the right task.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_AssertValidData ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> (2, 8, 8);

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
                queue.TryRunNextTask (default, out _);

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
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            ClassicAssert.AreEqual (queue.Count, 1);

            queue.TryRunNextTask (default, out _, out Exception ex);

            ClassicAssert.AreEqual (queue.Count, 0);

            AssertTaskResult (ex, value, result);
        }

        /// <summary>
        /// Enqueues and runs a task, while running an <see cref="IProfiler"/>
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Profiler ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            Profiler profiler = new Profiler ();

            queue.TryRunNextTask (default, out _, out Exception ex, profiler);

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
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            TaskResult runResult = new TaskResult ();
            int runValue = 42;

            TaskResult disposeResult = new TaskResult ();
            int disposeValue = 21;

            queue.Enqueue (new PutValueAndDisposeTask (runValue, runResult, disposeValue, disposeResult));

            queue.TryRunNextTask (default, out _, out Exception ex);

            AssertTaskResult (ex, runValue, runResult);
            AssertTaskResult (ex, disposeValue, disposeResult);
        }

        /// <summary>
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask<Unit, Unit>.Run"/> method. Asserts that the exception is returned from <see cref="TaskQueue<Unit, Unit>.TryRunNextTask(out Exception)"/>.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_Exception ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            queue.TryRunNextTask (default, out _, out Exception ex);

            ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);
        }

        /// <summary>
        /// Enqueues and runs a task which throws an <see cref="InvalidOperationException"/> in its <see cref="ITask<Unit, Unit>.Run"/> method, while running an <see cref="IProfiler"/>. Asserts profiling is stopped correctly.
        /// </summary>
        [Test]
        public void EnqueueAndTryRun_ExceptionWhileProfiling ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (() => throw new InvalidOperationException ());

            Profiler profiler = new Profiler ();

            queue.TryRunNextTask (out Exception ex, profiler);

            ClassicAssert.IsInstanceOf<InvalidOperationException> (ex);

            ClassicAssert.IsTrue (profiler.HasRun);
            ClassicAssert.IsFalse (profiler.Running);
            ClassicAssert.NotNull (profiler.LastTask);
        }

        /// <summary>
        /// Enqueues a task with <see cref="TaskQueue<Unit, Unit>.Enqueue{T}(in T, out TaskHandle)"/> and asserts that the <see cref="TaskHandle"/> returned is valid.
        /// </summary>
        [Test]
        public void EnqueueWithTaskHandle ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            ClassicAssert.IsFalse (handle.IsComplete);
        }

        /// <summary>
        /// Enqueues a task and runs it in a seperate thread, while waiting for its completion with <see cref="TaskHandle.WaitForCompletion"/>.
        /// </summary>
        [Test]
        public void EnqueueAndWait ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

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
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

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
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (new Task (), out TaskHandle handle);

            queue.TryRunNextTask ();

            handle.WaitForCompletion ();
        }

        /// <summary>
        /// Asserts that <see cref="TaskQueue<Unit, Unit>.TryRunNextTask"/> will return <see langword="false"/> when the <see cref="TaskQueue<Unit, Unit>"/> is empty.
        /// </summary>
        [Test]
        public void TryRun_ReturnFalse ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            ClassicAssert.IsFalse (queue.TryRunNextTask ());
        }

        /// <summary>
        /// Asserts that <see cref="TaskQueue<Unit, Unit>.TryRunNextTask"/> will return <see langword="true"/> when the <see cref="TaskQueue<Unit, Unit>"/> has a task enqueued and ready to be ran.
        /// </summary>
        [Test]
        public void TryRun_ReturnTrue ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            queue.Enqueue (new Task ());

            ClassicAssert.IsTrue (queue.TryRunNextTask ());
        }

        /// <summary>
        /// Enqueues a series of alternating <see cref="Task"/> and <see cref="PutValueAndDisposeTask"/>, asserting that non-disposable tasks will not disrupt the execution of <see cref="PutValueAndDisposeTask.Dispose"/>.
        /// </summary>
        [Test]
        public void Clear_Disposable ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

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
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

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
        /// Disposes a <see cref="TaskQueue<Unit, Unit>"/>, asserting that the internal <see cref="TaskQueue<Unit, Unit>.disposed"/> flag is set. Also asserts that following calls to <see cref="TaskQueue<Unit, Unit>.Enqueue{T}(in T)"/> & <see cref="TaskQueue<Unit, Unit>.Enqueue{T}(in T, out TaskHandle)"/> throw an <see cref="ObjectDisposedException"/>.
        /// </summary>
        [Test]
        public void Dispose ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

            ClassicAssert.IsFalse (GetPrivateValue<bool> ("disposed"));

            queue.Dispose ();

            ClassicAssert.IsTrue (GetPrivateValue<bool> ("disposed"));

            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task ()));
            Assert.Throws<ObjectDisposedException> (() => queue.Enqueue (new Task (), out _));

            T GetPrivateValue<T> (string fieldName) => (T)typeof (TaskQueue<Unit, Unit>).GetField (fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).GetValue (queue);
        }

        [Test]
        public void Dispose_GC ()
        {
            TaskQueue<Unit, Unit> queue = new TaskQueue<Unit, Unit> ();

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
        struct Task : ITask<TArg, TResult>
        {
            public TResult ResultToReturn;
            public List<TArg> suppliedArgs;

            public TResult Run (TArg arg)
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