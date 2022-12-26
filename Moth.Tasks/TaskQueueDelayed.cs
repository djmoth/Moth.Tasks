namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskQueueDelayed
    {
        private readonly object tasklock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();

        public void Enqueue<T> (T task, int millisecondDelay) where T : ITask
        {
            
        }
    }
}
