namespace Moth.Tasks
{
    /// <summary>
    /// Wraps an <see cref="ITask{TArg, TResult}"/> as an <see cref="ITask"/>, taking no arguments and returning no result.
    /// </summary>
    /// <typeparam name="TTask">Task type to wrap.</typeparam>
    /// <typeparam name="TArg">Argument of task.</typeparam>
    /// <typeparam name="TResult">Result of task.</typeparam>
    internal struct TaskWrapper<TTask, TArg, TResult> : ITask
        where TTask : struct, ITask<TArg, TResult>
    {
        private TTask task;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWrapper{TTask, TArg, TResult}"/> struct.
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        public TaskWrapper (TTask task)
        {
            this.task = task;
        }

        /// <summary>
        /// Run the wrapped task with <see langword="default"/> as argument and then discarding result.
        /// </summary>
        public void Run () => task.Run (default);
    }

    /// <summary>
    /// Wraps an <see cref="ITask{TArg}"/> as an <see cref="ITask{TArg, Unit}"/>, returning an instance of <see cref="Unit"/>.
    /// </summary>
    /// <typeparam name="TTask">Task type to wrap.</typeparam>
    /// <typeparam name="TArg">Argument of task.</typeparam>
    internal struct TaskWrapper<TTask, TArg> : ITask<TArg, Unit>
        where TTask : struct, ITask<TArg>
    {
        private TTask task;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWrapper{TTask, TArg}"/> struct.
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        public TaskWrapper (TTask task)
        {
            this.task = task;
        }

        /// <summary>
        /// Run the wrapped task with the specified argument.
        /// </summary>
        /// <param name="arg">Value to pass as argument.</param>
        /// <returns>An instance of <see cref="Unit"/>.</returns>
        public Unit Run (TArg arg)
        {
            task.Run (arg);
            return default;
        }
    }

    /// <summary>
    /// Wraps an <see cref="ITask"/> as an <see cref="ITask{Unit, Unit}"/>, taking a <see cref="Unit"/> as argument and returning an instance of <see cref="Unit"/>.
    /// </summary>
    /// <typeparam name="TTask">Task type to wrap.</typeparam>
    internal struct TaskWrapper<TTask> : ITask<Unit, Unit>
        where TTask : struct, ITask
    {
        private TTask task;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWrapper{TTask}"/> struct.
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        public TaskWrapper (TTask task)
        {
            this.task = task;
        }

        /// <summary>
        /// Run the wrapped task.
        /// </summary>
        /// <param name="arg">Argument is ignored.</param>
        /// <returns>An instance of <see cref="Unit"/>.</returns>
        public Unit Run (Unit arg)
        {
            task.Run ();
            return default;
        }
    }
}
