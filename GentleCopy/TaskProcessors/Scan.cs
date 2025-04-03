using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GentleCopy.TaskProcessors
{
    internal class Scan : ITaskProcessor
    {
        public static string TaskType = "scan";
        private string sourceRoot;
        private string destRoot;

        public string GetTaskType()
        {
            return TaskType;
        }

        public Scan(string source, string destination)
        {
            sourceRoot = source;
            destRoot = destination;
        }

        public List<NewQueueEntry>? ProcessQueueTask(QueueTask task)
        {
            var subtasks = new List<NewQueueEntry>();
            var info = new DirectoryInfo(task.Path);

            var destinationPath = Path.Combine(destRoot, Path.GetRelativePath(sourceRoot, task.Path));

            if (info.LinkTarget != null)
            {
                var target = Path.Combine(destRoot, Path.GetRelativePath(sourceRoot, info.ResolveLinkTarget(true).FullName));

                Directory.CreateSymbolicLink(destinationPath, target);
            }
            else
            {
                var directories = Directory.EnumerateDirectories(task.Path);
                var files = Directory.EnumerateFiles(task.Path);

                Console.WriteLine("Scanning directory " + task.Path);

                foreach (var d in directories)
                {
                    subtasks.Add(new NewQueueEntry
                    {
                        TaskType = Scan.TaskType,
                        Path = Path.Combine(task.Path, d),
                    });
                }
                foreach (var f in files)
                {
                    subtasks.Add(new NewQueueEntry
                    {
                        TaskType = CopyFile.TaskType,
                        Path = Path.Combine(task.Path, "..", f),
                    });
                }

                Directory.CreateDirectory(destinationPath);
                Directory.SetCreationTimeUtc(destinationPath, info.CreationTimeUtc);
                Directory.SetLastWriteTimeUtc(destinationPath, info.LastWriteTimeUtc);
                Directory.SetLastAccessTimeUtc(destinationPath, info.CreationTimeUtc);
            }

            return subtasks;
        }
    }
}
