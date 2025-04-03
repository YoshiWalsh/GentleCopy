using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using GentleCopy.TaskProcessors;
using NodaTime;
using Open.Collections;

namespace GentleCopy
{
    public enum QueueEntryStatus
    {
        Queued,
        Attempted,
        Completed
    }

    [Delimiter("\t")]
    public class NewQueueEntry
    {
        public QueueEntryStatus Status { get; set; }
        public string TaskType { get; set; }
        public string Path { get; set; }

        public string GetKey()
        {
            return TaskType + "|" + Path;
        }
    }

    public class QueueEntry : NewQueueEntry
    {
        public Int64 Time { get; set; }
    }

    public sealed class QueueEntryMap : ClassMap<QueueEntry>
    {
        public QueueEntryMap()
        {
            Map(m => m.Time);
            Map(m => m.Status);
            Map(m => m.TaskType).Name("Type");
            Map(m => m.Path);
        }
    }

    public class QueueTask : NewQueueEntry
    {
        public int AttemptCount { get; set; }
    }

    public class TaskQueue
    {
        private CsvWriter persist;
        private OrderedDictionary<string, QueueTask> taskQueue;

        public TaskQueue(NewQueueEntry initialTask, string queueFile)
        {
            taskQueue = new OrderedDictionary<string, QueueTask>
            {
                {
                    initialTask.GetKey(),
                    new QueueTask()
                    {
                        AttemptCount = 0,
                        Status = QueueEntryStatus.Queued,
                        TaskType = initialTask.TaskType,
                        Path = initialTask.Path,
                    }
                }
            };

            LoadQueueFromFile(queueFile);

            try
            {
                var writeStream = File.Open(queueFile, FileMode.CreateNew, FileAccess.Write);
                var writer = new StreamWriter(writeStream);
                persist = new CsvWriter(writer, CultureInfo.InvariantCulture);
            }
            catch (IOException ex)
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    // Don't write the header again.
                    HasHeaderRecord = false,
                };
                var writeStream = File.Open(queueFile, FileMode.Append, FileAccess.Write);
                var writer = new StreamWriter(writeStream);
                persist = new CsvWriter(writer, config);
            }
            persist.Context.RegisterClassMap<QueueEntryMap>();
        }

        private Dictionary<string, ITaskProcessor> taskProcessors = new Dictionary<string, ITaskProcessor>();

        private void LoadQueueFromFile(string queueFile)
        {
            try
            {
                var file = File.OpenRead(queueFile);
                var reader = new StreamReader(file, Encoding.UTF8);
                var tsv = new CsvReader(reader, CultureInfo.InvariantCulture);
                tsv.Context.RegisterClassMap<QueueEntryMap>();

                var entries = tsv.GetRecords<QueueEntry>().ToList();
                UpdatePendingTasks(entries);

                tsv.Dispose();
                reader.Close();
                file.Close();
            } catch (FileNotFoundException ex)
            {

            }
        }

        public void RegisterProcessor(ITaskProcessor processor)
        {
            taskProcessors.Add(processor.GetTaskType(), processor);
        }

        public void AddQueueEntries(List<NewQueueEntry> tasks)
        {
            var newQueueEntries = new List<QueueEntry>();
            foreach (var task in tasks)
            {
                newQueueEntries.Add(new QueueEntry
                {
                    Time = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
                    Status = task.Status,
                    TaskType = task.TaskType,
                    Path = task.Path,
                });
            }

            persist.WriteRecords(newQueueEntries);
            persist.Flush();
            UpdatePendingTasks(newQueueEntries);
        }

        public void UpdatePendingTasks(List<QueueEntry> entries)
        {
            foreach(var entry in entries)
            {
                switch(entry.Status)
                {
                    case QueueEntryStatus.Queued:
                        taskQueue.Add(
                            entry.GetKey(),
                            new QueueTask()
                            {
                                AttemptCount = 0,
                                TaskType = entry.TaskType,
                                Path = entry.Path,
                            }
                        );
                        break;
                    case QueueEntryStatus.Attempted:
                        var key = entry.GetKey();
                        var existing = taskQueue[key];
                        existing.AttemptCount++;

                        // Move to end of queue
                        taskQueue.Remove(key);
                        taskQueue.Add(key, existing);
                        break;
                    case QueueEntryStatus.Completed:
                        taskQueue.Remove(entry.GetKey());
                        break;
                    default:
                        break;
                }
            }
        }

        public bool RunTask()
        {
            if(taskQueue.Count < 1)
            {
                return false;
            }

            var task = taskQueue.First().Value;

            // Log the task as pending
            AddQueueEntries(new List<NewQueueEntry>() { new NewQueueEntry() {
                Status = QueueEntryStatus.Attempted,
                TaskType = task.TaskType,
                Path = task.Path,
            }});

            try
            {
                var processor = taskProcessors[task.TaskType];
                var subtasks = processor.ProcessQueueTask(task);
                if (subtasks == null)
                {
                    subtasks = new List<NewQueueEntry>();
                }

                // Mark the current task as completed
                subtasks.Add(new NewQueueEntry()
                {
                    Status = QueueEntryStatus.Completed,
                    TaskType = task.TaskType,
                    Path = task.Path,
                });

                AddQueueEntries(subtasks);
            } catch (Exception ex)
            {
                Console.WriteLine("err - " + ex.ToString());
            }

            return true;
        }

        public void RunTasks()
        {
            while(RunTask())
            {
                Console.WriteLine("Task queue length:" + taskQueue.Count.ToString());
            }
        }
    }
}
