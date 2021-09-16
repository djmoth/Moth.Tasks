namespace Moth.Tasks
{
    using System.Runtime.InteropServices;

    internal struct ManagedTask : ITask
    {
        private GCHandle taskHandle;

        public ManagedTask (ITask task) => taskHandle = GCHandle.Alloc (task);

        public void Run ()
        {
            ITask task = (ITask)taskHandle.Target;
            taskHandle.Free ();

            task.Run ();
        }
    }
}
