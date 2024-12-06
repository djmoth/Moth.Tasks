namespace Moth.Tasks
{
    /// <summary>
    /// Represents a task that can be run with no argument and returns no result.
    /// </summary>
    public interface IRunnableTaskMetadata : ITaskMetadata
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
    public interface IRunnableTaskMetadata<TArg> : IRunnableTaskMetadata
    {
        /// <summary>
        /// Runs the task with an argument.
        /// </summary>
        /// <inheritdoc cref="IRunnableTaskMetadata.Run(TaskQueue.TaskDataAccess)"/>/>
        /// <param name="access"/>
        /// <param name="arg">Argument to supply to task.</param>
        void Run (TaskQueue.TaskDataAccess access, TArg arg);
    }

    /// <summary>
    /// Represents a task that can be run with an argument and returns a result.
    /// </summary>
    /// <inheritdoc cref="IRunnableTaskMetadata{TArg}"/>
    /// <typeparam name="TArg"/>
    /// <typeparam name="TResult">The type of the task result.</typeparam>
    public interface IRunnableTaskMetadata<TArg, TResult> : IRunnableTaskMetadata<TArg>
    {
        /// <summary>
        /// Runs the task with an argument and returns a result.
        /// </summary>
        /// <inheritdoc cref="IRunnableTaskMetadata{TArg}.Run(TaskQueue.TaskDataAccess, TArg)"/>/>
        /// <param name="access"/>
        /// <param name="arg"/>
        /// <returns>The result of the task.</returns>
        new TResult Run (TaskQueue.TaskDataAccess access, TArg arg);
    }
}
