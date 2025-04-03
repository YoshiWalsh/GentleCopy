using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GentleCopy.TaskProcessors
{
    public interface ITaskProcessor
    {
        public abstract string GetTaskType();

        public abstract List<NewQueueEntry>? ProcessQueueTask(QueueTask task);
    }
}
