using System.Threading;

namespace Moth.Tasks
{
    public interface IWorkerThread
    {
        void Start (ThreadStart method);

        void Join ();
    }
}
