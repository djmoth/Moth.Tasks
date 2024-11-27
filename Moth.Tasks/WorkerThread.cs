namespace Moth.Tasks
{
    using System;
    using System.Threading;

    public class WorkerThread : IWorkerThread
    {
        private Thread thread;

        public WorkerThread (bool isBackground)
        {
            IsBackground = isBackground;
        }

        public bool IsBackground { get; private set; }

        public void Start (ThreadStart method)
        {
            if (thread != null)
            {
                throw new InvalidOperationException ("Thread is already started.");
            }

            thread = new Thread (method)
            {
                IsBackground = IsBackground,
            };

            thread.Start ();
        }

        public void Join ()
        {
            if (thread == null)
            {
                throw new InvalidOperationException ("Thread is not started.");
            }

            thread.Join ();
        }
    }
}
