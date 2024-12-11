namespace Moth.Tasks
{
    /// <summary>
    /// Interface for a task.
    /// </summary>
    public interface ITask
    {
    }

    /// <summary>
    /// Interface for a task taking an argument and returning a result.
    /// </summary>
    /// <typeparam name="TArg">Type of argument to pass to the task.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public interface ITask<TArg, TResult> : ITask
    {
        /// <summary>
        /// Run the task.
        /// </summary>
        /// <param name="arg">Task argument.</param>
        /// <returns>Task result.</returns>
        TResult Run (TArg arg);
    }
}
