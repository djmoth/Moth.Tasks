namespace Moth.Tasks
{
    public struct Task
    {
        public TaskAwaiter GetAwaiter ()
        {
            return new TaskAwaiter ();
        }
    }
}
