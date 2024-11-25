namespace Moth.Tasks
{
    /// <summary>
    /// Interface for a task taking an argument.
    /// </summary>
    /// <typeparam name="TArg">Type of argument to pass to the task.</typeparam>
    public interface ITask<TArg> : ITaskType
    {
        /// <summary>
        /// Run the task.
        /// </summary>
        /// <param name="arg">Task argument.</param>
        void Run (TArg arg);
    }

    /// <summary>
    /// Interface for a task taking no arguments.
    /// </summary>
    public interface ITask : ITaskType
    {
        /// <summary>
        /// Run the task.
        /// </summary>
        void Run ();
    }

    public interface ITaskType { }
}
