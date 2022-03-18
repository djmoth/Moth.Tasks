namespace Moth.Tasks
{
    internal struct TaskWithResult<TProducer, TConsumer> : ITask where TProducer : ITask<>
    {
        private Task t;

        public void Run ()
        {
            

        }
        struct Task : ITask<Void>
        {
            public void Run () => 2;
        }
    }
}
