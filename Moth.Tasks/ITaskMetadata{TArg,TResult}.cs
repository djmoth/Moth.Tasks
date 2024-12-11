namespace Moth.Tasks
{
    /// <summary>
    /// Represents a task that can be run with an argument and returns a result.
    /// </summary>
    /// <typeparam name="TArg">The type of the task argument.</typeparam>
    /// <typeparam name="TResult">The type of the task result.</typeparam>
    public interface ITaskMetadata<TArg, TResult>
    {
        /// <summary>
        /// Runs the task with an argument and returns a result.
        /// </summary>
        /// <param name="access"><see cref="TaskQueue.TaskDataAccess"/> instance allowing for retrieval of task data.</param>
        /// <param name="arg">Argument to supply to task.</param>
        /// <param name="result">The result of the task.</param>
        void Run (TaskQueue.TaskDataAccess access, TArg arg, out TResult result);
    }
}
