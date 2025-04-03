using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GentleCopy.TaskProcessors
{
    internal class CopyFile : ITaskProcessor
    {
        public static string TaskType = "copy-file";
        private string sourceRoot;
        private string destRoot;

        public string GetTaskType() {
            return TaskType;
        }

        public CopyFile(string source, string destination)
        {
            sourceRoot = source;
            destRoot = destination;
        }

        public List<NewQueueEntry>? ProcessQueueTask(QueueTask task)
        {
            Console.WriteLine("Copying file " + task.Path);

            var info = new DirectoryInfo(task.Path);

            var destinationPath = Path.Combine(destRoot, Path.GetRelativePath(sourceRoot, task.Path));

            if (info.LinkTarget != null)
            {
                var target = Path.Combine(destRoot, Path.GetRelativePath(sourceRoot, info.ResolveLinkTarget(true).FullName));

                File.CreateSymbolicLink(destinationPath, target);
            }
            else
            {

                File.Copy(task.Path, destinationPath, true);
                File.SetCreationTimeUtc(destinationPath, info.CreationTimeUtc);
                File.SetLastWriteTimeUtc(destinationPath, info.LastWriteTimeUtc);
                File.SetLastAccessTimeUtc(destinationPath, info.LastAccessTimeUtc);
            }

            return null;
        }
    }
}
