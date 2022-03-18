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

    /// <summary>
    /// Interface providing a <see cref="Run"/> method for executing task code, returning a result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">Return type of <see cref="Run"/> method.</typeparam>
    public interface ITask<out TResult> : ITask
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        TResult Run ();

        void ITask.Run() => Run ();
    }

    /*public interface ITask<in TInput> : ITask
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        TOutput Run ();

        void ITask.Run () => Run ();
    }*/
}
