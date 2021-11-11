namespace Moth.Tasks
{
    public readonly struct TaskHandle
    {
        private readonly TaskQueue queue;
        private readonly int handleID;

        internal TaskHandle (TaskQueue queue, int handleID)
        {
            this.queue = queue;
            this.handleID = handleID;
        }

        public bool IsComplete => queue.IsTaskComplete (handleID);

        public void WaitForCompletion () => WaitForCompletion (-1);

        public bool WaitForCompletion (int timeout) => queue.WaitForCompletion (handleID, timeout);
    }
}
