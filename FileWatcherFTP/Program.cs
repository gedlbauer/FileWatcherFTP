using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FileWatcherFTP
{
    class Program
    {
        private static SftpManager sftpManager;
        static void Main(string[] args)
        {
            try
            {
                sftpManager = new SftpManager("192.168.68.127", "root", "root");
                var watchedDirectory = Directory.GetCurrentDirectory().Split("\\").ToList();
                watchedDirectory.RemoveAt(watchedDirectory.Count - 1);
                var watcher = new FileSystemWatcher(string.Join("\\", watchedDirectory));
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                Console.WriteLine("Watcher initialized:");
                Console.WriteLine($"Directory: {watcher.Path}");
                Console.WriteLine($"Subdirectories: {watcher.IncludeSubdirectories}");
                Console.WriteLine($"Events: {watcher.EnableRaisingEvents}");
                Console.WriteLine("Watcher status end");
                Console.WriteLine("Starting upload");
                sftpManager.UploadDirectory();
                Console.WriteLine("Upload ended");
                watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Created;
                watcher.Deleted += Watcher_Deleted;
                watcher.Renamed += Watcher_Renamed;
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        private static void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("rename");
            Console.ResetColor();
            Console.WriteLine($"] \"{e.OldName}\" to \"{e.Name}\"");
            sftpManager.RenameSingleFileOrDirectory(e.OldFullPath, e.FullPath);
            Console.WriteLine("upload successfull");
        }

        private static void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("delete");
            Console.ResetColor();
            Console.WriteLine($"] \"{e.Name}\"");
            sftpManager.DeleteSingleFileOrDirectory(e.FullPath);
        }

        private static void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("create");
            Console.ResetColor();
            Console.WriteLine($"] {e.Name}");
            sftpManager.UploadSingleFileOrDirectory(e.FullPath);
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("change");
            Console.ResetColor();
            Console.WriteLine($"] \"{e.Name}\"");
            if (File.Exists(e.FullPath)){
                sftpManager.UploadSingleFileOrDirectory(e.FullPath);
            }
        }
    }
}
