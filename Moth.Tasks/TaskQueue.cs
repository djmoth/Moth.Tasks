namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public readonly struct TaskRunInfo
    {
        public TaskQueue TaskQueue { get; }

        public IProfiler Profiler { get; }
    }

    public unsafe class TaskQueue<TArg> : TaskQueueBase
    {
        public TaskQueue ()
            : base () { }

        public TaskQueue (int taskCapacity, int dataCapacity)
            : base (taskCapacity, dataCapacity) { }

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (in T task) where T : struct, ITask<TArg> => EnqueueTask (task);


        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <typeparam name="TTask">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <param name="handle"><see cref="TaskHandle"/> for checking task status.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg>
        {
            handle = TaskHandleManager.CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<TTask, TArg> (task, handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<TTask, TArg> (task, handle));
            }
        }

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            WaitForTask (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return;
            }

            TryRunNextTask (arg, out exception, profiler);
        }

        /// <summary>
        /// Tries to run the next task in the queue, if present. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (TArg arg, IProfiler profiler = null) => TryRunNextTask (arg, out _, profiler);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails.
        /// May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (TArg arg,out Exception exception, IProfiler profiler = null)
        {
            exception = null;

            if (TryGetNextTask (out ITaskInfo task, out TaskDataAccess access))
            {
                bool isProfiling = false;

                try
                {
                    if (profiler != null)
                    {
                        profiler.BeginTask (task.Type.FullName);
                        isProfiling = true; // If profiler was started without throwing an exception
                    }

                    ((ITaskInfoRunnable<TArg>)task).Run (ref access, arg); // Run the task

                    if (isProfiling)
                    {
                        isProfiling = false;
                        profiler.EndTask ();
                    }
                } catch (Exception ex)
                {
                    exception = ex;

                    if (!access.Disposed) // Internal error, TaskInfo should always call TaskDataAccess.Dispose after getting task data in TaskInfo.RunAndDispose
                    {
                        access.Dispose ();
                    }

                    if (isProfiling)
                    {
                        profiler.EndTask ();
                    }
                }

                return true;
            } else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// A queue of tasks, which can be run in FIFO order.
    /// </summary>
    public unsafe class TaskQueue : TaskQueueBase
    {
        public TaskQueue ()
            : base () { }

        public TaskQueue (int taskCapacity, int dataCapacity)
            : base (taskCapacity, dataCapacity) { }

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (in T task) where T : struct, ITask => EnqueueTask (task);

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <typeparam name="TTask">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <param name="handle"><see cref="TaskHandle"/> for checking task status.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask
        {
            handle = TaskHandleManager.CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<TTask> (task, handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<TTask> (task, handle));
            }
        }

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (IProfiler profiler = null, CancellationToken token = default) => RunNextTask (out _, profiler, token);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            WaitForTask (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return;
            }

            TryRunNextTask (out exception, profiler);
        }

        /// <summary>
        /// Tries to run the next task in the queue, if present. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (IProfiler profiler = null) => TryRunNextTask (out _, profiler);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails.
        /// May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (out Exception exception, IProfiler profiler = null)
        {
            exception = null;

            if (TryGetNextTask (out ITaskInfo task, out TaskDataAccess access))
            {
                bool isProfiling = false;

                try
                {
                    if (profiler != null)
                    {
                        profiler.BeginTask (task.Type.FullName);
                        isProfiling = true; // If profiler was started without throwing an exception
                    }

                    ((ITaskInfoRunnable)task).Run (ref access); // Run the task

                    if (isProfiling)
                    {
                        isProfiling = false;
                        profiler.EndTask ();
                    }
                } catch (Exception ex)
                {
                    exception = ex;

                    if (!access.Disposed) // Internal error, TaskInfo should always call TaskDataAccess.Dispose after getting task data in TaskInfo.RunAndDispose
                    {
                        access.Dispose ();
                    }

                    if (isProfiling)
                    {
                        profiler.EndTask ();
                    }
                }

                return true;
            } else
            {
                return false;
            }
        }
    }
}
