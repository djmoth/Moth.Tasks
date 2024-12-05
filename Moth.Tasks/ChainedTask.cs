namespace Moth.Tasks
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a task that is composed of two other tasks that are run in sequence.
    /// </summary>
    /// <typeparam name="T1">Type of first task.</typeparam>
    /// <typeparam name="T2">Type of second task.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    public struct ChainedTask<T1, T2> : ITask
        where T1 : struct, ITask
        where T2 : struct, ITask
    {
        private T1 first;
        private T2 second;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainedTask{T1, T2}"/> struct.
        /// </summary>
        /// <param name="first">First task.</param>
        /// <param name="second">Second task.</param>
        public ChainedTask (T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Runs the first task, then the second task.
        /// </summary>
        public void Run ()
        {
            first.Run ();
            second.Run ();
        }
    }

    /// <summary>
    /// Represents a task that is composed of two other tasks that are run in sequence. The result of the first task is passed as an argument to the second task.
    /// </summary>
    /// <typeparam name="T1">Type of first task.</typeparam>
    /// <typeparam name="T2">Type of second task.</typeparam>
    /// <typeparam name="T1Arg">Type of the first task's argument.</typeparam>
    /// <typeparam name="T1ResultT2Arg">Type of the first task's result and second task's argument.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    public struct ChainedTask<T1, T2, T1Arg, T1ResultT2Arg> : ITask<T1Arg>
        where T1 : struct, ITask<T1Arg, T1ResultT2Arg>
        where T2 : struct, ITask<T1ResultT2Arg>
    {
        private T1 first;
        private T2 second;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainedTask{T1, T2, T1Arg, T1ResultT2Arg}"/> struct.
        /// </summary>
        /// <param name="first">First task.</param>
        /// <param name="second">Second task.</param>
        public ChainedTask (T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Runs the first task, then the second task. The result of the first task is passed as an argument to the second task.
        /// </summary>
        /// <param name="arg">Argument to supply to first task.</param>
        public void Run (T1Arg arg) => second.Run (first.Run (arg));
    }

    /// <summary>
    /// Represents a task that is composed of two other tasks that are run in sequence. The result of the first task is passed as an argument to the second task.
    /// </summary>
    /// <typeparam name="T1">Type of first task.</typeparam>
    /// <typeparam name="T2">Type of second task.</typeparam>
    /// <typeparam name="T1Arg">Type of the first task's argument.</typeparam>
    /// <typeparam name="T1ResultT2Arg">Type of the first task's result and second task's argument.</typeparam>
    /// <typeparam name="T2Result">Type of the second tasks result.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    public struct ChainedTask<T1, T2, T1Arg, T1ResultT2Arg, T2Result> : ITask<T1Arg, T2Result>
        where T1 : struct, ITask<T1Arg, T1ResultT2Arg>
        where T2 : struct, ITask<T1ResultT2Arg, T2Result>
    {
        private T1 first;
        private T2 second;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainedTask{T1, T2, T1Arg, T1ResultT2Arg, T2Result}"/> struct.
        /// </summary>
        /// <param name="first">First task.</param>
        /// <param name="second">Second task.</param>
        public ChainedTask (T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Runs the first task, then the second task. The result of the first task is passed as an argument to the second task.
        /// </summary>
        /// <param name="arg">Argument to supply to first task.</param>
        /// <returns>Result returned by second task.</returns>
        public T2Result Run (T1Arg arg) => second.Run (first.Run (arg));
    }
}
