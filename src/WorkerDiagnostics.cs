namespace Moth.Tasks
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public interface IProfiler
    {
        void BeginTask (string task);

        void EndTask ();
    }
}
