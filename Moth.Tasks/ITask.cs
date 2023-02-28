namespace Moth.Tasks
{
    /// <summary>
    /// Interface providing a <see cref="Run"/> method for executing task code.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        void Run ();
    }

    public interface ITask<TArg>
    {
        void Run (TArg arg);
    }
}
