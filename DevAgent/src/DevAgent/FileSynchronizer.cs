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
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            lock (_lockObj)
            {
                string relativePath = Path.GetFullPath(e.FullPath).Substring(_localRootDirectory.Length).Replace('\\', '/').TrimStart('/');

                var message = new Message()
                {
                    Id = relativePath,
                    Method = "ChangeFile",
                    Content = InvokeWithRetries(() => File.ReadAllBytes(e.FullPath))
                };

                _messageHub.SendMessage(message);

                Console.WriteLine("OnFileChanged");
            }
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Method != "ChangeFile")
            {
                return;
            }

            lock (_lockObj)
            {
                string relativePath = e.Message.Id;

                string fullPath = Path.GetFullPath(Path.Combine(_localRootDirectory, relativePath));

                var content = InvokeWithRetries(() => File.ReadAllBytes(fullPath));

                if (!Enumerable.SequenceEqual(InvokeWithRetries(() => File.ReadAllBytes(fullPath)), e.Message.Content))
                {
                    InvokeWithRetries(() => File.WriteAllBytes(fullPath, e.Message.Content));
                }

                Console.WriteLine("OnMessageReceived");
            }
        }

        private static T InvokeWithRetries<T>(Func<T> func, int maxRetries = 10)
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    return func.Invoke();
                }
                catch
                {
                    if (retry == maxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(100);
                }
            }

            return default(T);
        }

        private static void InvokeWithRetries(Action action, int maxRetries = 10)
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    action.Invoke();
                }
                catch
                {
                    if (retry == maxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(100);
                }
            }
        }
    }
}
