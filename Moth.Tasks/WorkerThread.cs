namespace Moth.Tasks
{
    using System;
    using System.Threading;

    /// <summary>
    /// Wraps a <see cref="Thread"/> as a worker thread.
    /// </summary>
    public class WorkerThread : IWorkerThread
    {
        private Thread thread;
        private bool isBackground;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerThread"/> class.
        /// </summary>
        /// <param name="isBackground"><see langword="true"/> if the thread should run in the background.</param>
        public WorkerThread (bool isBackground)
        {
            this.isBackground = isBackground;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the thread is a background thread.
        /// </summary>
        public bool IsBackground
        {
            get => isBackground;
            set
            {
                if (thread != null)
                {
                    thread.IsBackground = value;
                }

                isBackground = value;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">Thread is already started.</exception>"
        public void Start (ThreadStart method)
        {
            if (thread != null)
            {
                throw new InvalidOperationException ("Thread is already started.");
            }

            thread = new Thread (method)
            {
                IsBackground = isBackground,
            };

            thread.Start ();
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">Thread is not started.</exception>""
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
