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

    internal struct DelegateTask<T1, T2> : ITask
    {
        private Action<T1, T2> action;
        private T1 arg1;
        private T2 arg2;

        public DelegateTask (Action<T1, T2> action, T1 arg1, T2 arg2) => (this.action, this.arg1, this.arg2) = (action, arg1, arg2);

        public void Run () => action (arg1, arg2);
    }

    internal struct DelegateTask<T1, T2, T3> : ITask
    {
        private Action<T1, T2, T3> action;
        private T1 arg1;
        private T2 arg2;
        private T3 arg3;

        public DelegateTask (Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => (this.action, this.arg1, this.arg2, this.arg3) = (action, arg1, arg2, arg3);

        public void Run () => action (arg1, arg2, arg3);
    }
}
