using System;
using System.Collections.Generic;
using System.Text;

namespace Moth.Tasks
{
    public interface ITaskInfoProvider
    {
        ITaskInfo<T> Create<T> (int id) where T : struct, ITaskType;
    }
}
