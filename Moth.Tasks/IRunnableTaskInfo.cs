namespace Moth.Tasks
{
    /// <summary>
    /// Represents a task that can be run with no argument and returns no result.
    /// </summary>
    public interface IRunnableTaskInfo : ITaskInfo
    {
        /// <summary>
        /// Runs the task.
        /// </summary>
        /// <param name="access"><see cref="TaskQueue.TaskDataAccess"/> instance allowing for retrieval of task data.</param>
        void Run (TaskQueue.TaskDataAccess access);
    }

    /// <summary>
    /// Represents a task that can be run with an argument and returns no result.
    /// </summary>
    /// <typeparam name="TArg">Type of the argument.</typeparam>
    public interface IRunnableTaskInfo<TArg> : IRunnableTaskInfo
    {
        /// <summary>
        /// Runs the task with an argument.
        /// </summary>
        /// <inheritdoc cref="IRunnableTaskInfo.Run(TaskQueue.TaskDataAccess)"/>/>
        /// <param name="access"/>
        /// <param name="arg">Argument to supply to task.</param>
        void Run (TaskQueue.TaskDataAccess access, TArg arg);
    }

    /// <summary>
    /// Represents a task that can be run with an argument and returns a result.
    /// </summary>
    /// <inheritdoc cref="IRunnableTaskInfo{TArg}"/>
    /// <typeparam name="TArg"/>
    /// <typeparam name="TResult">The type of the task result.</typeparam>
    public interface IRunnableTaskInfo<TArg, TResult> : IRunnableTaskInfo<TArg>
    {
        /// <summary>
        /// Runs the task with an argument and returns a result.
        /// </summary>
        /// <inheritdoc cref="IRunnableTaskInfo{TArg}.Run(TaskQueue.TaskDataAccess, TArg)"/>/>
        /// <param name="access"/>
        /// <param name="arg"/>
        /// <returns>The result of the task.</returns>
        new TResult Run (TaskQueue.TaskDataAccess access, TArg arg);
    }
}
