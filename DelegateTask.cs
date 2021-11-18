namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Task encapsulating an <see cref="Action"/> with no parameters.
    /// </summary>
    internal readonly struct DelegateTask : ITask
    {
        private readonly Action action;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateTask"/> struct.
        /// </summary>
        /// <param name="action"><see cref="Action"/> to invoke on <see cref="Run"/>.</param>
        public DelegateTask (Action action) => this.action = action;

        /// <summary>
        /// Invokes the encapsulated <see cref="Action"/>.
        /// </summary>
        public void Run () => action ();
    }

    /// <summary>
    /// Task encapsulating an <see cref="Action"/> with one parameter.
    /// </summary>
    /// <typeparam name="T">Type of task.</typeparam>
    internal struct DelegateTask<T> : ITask
    {
        private readonly Action<T> action;
        private readonly T arg;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateTask{T}"/> struct.
        /// </summary>
        /// <param name="action"><see cref="Action"/> to invoke on <see cref="Run"/>.</param>
        /// <param name="arg">Argument to invoke <paramref name="action"/> with.</param>
        public DelegateTask (Action<T> action, T arg) => (this.action, this.arg) = (action, arg);

        /// <summary>
        /// Invokes the encapsulated <see cref="Action"/> with the provided argument.
        /// </summary>
        public void Run () => action (arg);
    }
}
