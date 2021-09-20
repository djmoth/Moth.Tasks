namespace Moth.Tasks
{
    public struct TaskHandle
    {
        private TaskQueue queue;
        private int handleID;

        public void WaitForCompletion () => WaitForCompletion (-1);

        public bool WaitForCompletion (int timeout)
        {
            return queue.WaitForCompletion (handleID, timeout);
        }

        public void Cancel ()
        {

        }
    }
}
