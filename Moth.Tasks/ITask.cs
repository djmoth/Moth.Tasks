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
    /// Interface providing a <see cref="Run"/> method for executing chained task code, passing on a result of type <typeparamref name="TOut"/> to the next task in the chain.
    /// </summary>
    /// <typeparam name="TOut">Return type of <see cref="Run"/> method.</typeparam>
    public interface ITaskOut<TOut>
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        /// <returns>Data to pass on to next task in the chain.</returns>
        TOut Run ();
    }

    /// <summary>
    /// Interface providing a <see cref="Run"/> method for executing chained task code, taking an argument of type <typeparamref name="TIn"/> from the previous task in the chain.
    /// </summary>
    /// <typeparam name="TIn">Argument type for <see cref="Run"/> method.</typeparam>
    public interface ITaskIn<TIn>
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        /// <param name="input">Input data from previous task in the chain.</param>
        void Run (TIn input);
    }

    /// <summary>
    /// Interface providing a <see cref="Run"/> method for executing chained task code, taking an argument of type <typeparamref name="TIn"/> from the previous task, and passing on a result of type <typeparamref name="TOut"/> to the next task in the chain.
    /// </summary>
    /// <typeparam name="TIn">Return type of <see cref="Run"/> method.</typeparam>
    /// <typeparam name="TOut">Argument type for <see cref="Run"/> method.</typeparam>
    public interface ITaskInOut<TIn, TOut>
    {
        /// <summary>
        /// Task code to run.
        /// </summary>
        /// <param name="input">Input data from previous task in the chain.</param>
        /// <returns>Data to pass on to next task in the chain.</returns>
        TOut Run (TIn input);
    }
}
