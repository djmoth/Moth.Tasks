namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    public struct TaskHandleEvent
    {
        private ManualResetEventSlim waitEvent;

        public int Waiters { get; private set; }

        public bool SignalComplete ()
        {
            waitEvent.Set ();

            if (Waiters == 0)
            {
                waitEvent.Dispose ();
                return true;
            }

            return false;
        }

        public bool Wait (int timeout) => waitEvent.Wait (timeout);

        public void AddWaiter ()
        {
            if (Waiters == 0)
            {
                waitEvent = new ManualResetEventSlim ();
            }

            Waiters++;
        }

        public bool RemoveWaiter (bool complete)
        {
            Waiters--;

            if (complete && Waiters == 0)
            {
                waitEvent.Dispose ();
                return true;
            }
        }
    }
}
