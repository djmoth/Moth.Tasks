namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;

    public struct TaskAwaiter : INotifyCompletion
    {
        public void GetResult () { }

        public bool IsCompleted => false;

        public void OnCompleted (Action continuation)
        {
            throw new NotImplementedException ();
        }
    }
}
