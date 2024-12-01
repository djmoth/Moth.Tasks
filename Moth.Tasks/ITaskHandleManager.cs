namespace Moth.Tasks
{
    public interface ITaskHandleManager
    {
        void Clear ();

        TaskHandle CreateTaskHandle ();

        bool IsTaskComplete (int handleID);
    }
}