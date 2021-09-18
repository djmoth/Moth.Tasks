namespace Moth.Tasks
{
    public interface IProfiler
    {
        void BeginTask (string task);

        void EndTask ();
    }
}
