using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace DevAgent
{
    public class FileSynchronizer : IDisposable
    {
        private readonly string _localRootDirectory;
        private readonly MessageHub _messageHub;
        private readonly FileSystemWatcher _watcher;
        private readonly object _lockObj;

        public FileSynchronizer(string localRootDirectory, MessageHub messageHub)
        {
            _localRootDirectory = Path.GetFullPath(localRootDirectory);
            _messageHub = messageHub;
            _messageHub.MessageReceived += OnMessageReceived;
            _watcher = new FileSystemWatcher(_localRootDirectory);
            _watcher.Changed += OnFileChanged;
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            _lockObj = new object();

            _messageHub.WaitUntilConnectedAsync().Wait();
        }

        public void Dispose()
        {
            _watcher.Changed -= OnFileChanged;
            _watcher.Dispose();
            _messageHub.MessageReceived -= OnMessageReceived;
            _messageHub.Dispose();

            GC.SuppressFinalize(this);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lockObj)
            {
                if (e.ChangeType != WatcherChangeTypes.Changed)
                {
                    return;
                }

                string relativePath = Path.GetFullPath(e.FullPath).Substring(_localRootDirectory.Length).Replace('\\', '/').TrimStart('/');

                var message = new Message()
                {
                    Id = relativePath,
                    Method = "ChangeFile",
                    Content = RunWithRetries(() => File.ReadAllBytes(e.FullPath))
                };

                _messageHub.SendMessage(message);

                Console.WriteLine("OnFileChanged");
            }
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            lock (_lockObj)
            {
                var message = e.Message;

                if (message.Method != "ChangeFile")
                {
                    return;
                }

                string relativePath = message.Id;

                string fullPath = Path.GetFullPath(Path.Combine(_localRootDirectory, relativePath));

                var content = RunWithRetries(() => File.ReadAllBytes(fullPath));

                if (!Enumerable.SequenceEqual(RunWithRetries(() => File.ReadAllBytes(fullPath)), message.Content))
                {
                    RunWithRetries(() => RunWithRetries(() => File.WriteAllBytes(fullPath, message.Content)));
                }

                Console.WriteLine("OnMessageReceived");
            }
        }

        private static T RunWithRetries<T>(Func<T> func)
        {
            const int MaxRetries = 10;

            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    return func.Invoke();
                }
                catch
                {
                    if (retry == MaxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(100);
                }
            }

            return default(T);
        }

        private static void RunWithRetries(Action action)
        {
            const int MaxRetries = 10;

            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    action.Invoke();
                }
                catch
                {
                    if (retry == MaxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(100);
                }
            }
        }
    }
}
