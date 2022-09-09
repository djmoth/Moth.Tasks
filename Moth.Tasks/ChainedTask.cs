namespace Moth.Tasks
{
    using System;

    public struct ChainedTask<T1, T2> : ITask
        where T1 : struct, ITask
        where T2 : struct, ITask
    {
        T1 first;
        T2 second;

        public ChainedTask (T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }

        public void Run ()
        {
            first.Run ();
            second.Run ();
        }
    }
}
