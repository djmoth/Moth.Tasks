namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;

    internal struct DelegateTask : ITask
    {
        private Action action;

        public DelegateTask (Action action) => this.action = action;

        public void Run () => action ();
    }

    internal struct DelegateTask<T> : ITask
    {
        private Action<T> action;
        private T arg;

        public DelegateTask (Action<T> action, T arg) => (this.action, this.arg) = (action, arg);

        public void Run () => action (arg);
    }
}
