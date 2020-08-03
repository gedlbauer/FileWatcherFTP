using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace FileWatcherFTP
{
    public class SftpManager
    {
        private readonly string host, username, password;

        public SftpManager(string host, string username, string password)
        {
            this.host = host;
            this.username = username;
            this.password = password;
        }

        public void UploadSingleFileOrDirectory(string path)
        {
            if (path.Contains(".vs\\") || path.Contains(".git\\")) return;
            var remotePath = ConvertToRemotePath(path);
            using (var client = new SftpClient(host, username, password))
            {
                client.Connect();
                client.ChangeDirectory("/root");
                if (File.Exists(path))
                {
                    using (var file = File.OpenRead(path))
                    {
                        client.UploadFile(file, remotePath, true);
                    }
                }
                else if (Directory.Exists(path) && !client.Exists(remotePath))
                {
                    client.CreateDirectory(remotePath);
                }
                client.Disconnect();
            }
        }

        public void DeleteSingleFileOrDirectory(string path)
        {
            if (path.Contains(".vs\\") || path.Contains(".git\\")) return;
            var remotePath = ConvertToRemotePath(path);
            //using (var client = new SftpClient(host, username, password))
            //{
            //    client.Connect();
            //    client.ChangeDirectory("/root");
            //    if(client.Exists(remotePath)) client.Delete(remotePath);
            //}
            using(var sshClient = new SshClient(host, username, password))
            {
                sshClient.Connect();
                sshClient.CreateCommand($"cd /root; rm -rf '{remotePath}'").Execute();
                sshClient.Disconnect();
            }
        }

        public void RenameSingleFileOrDirectory(string oldPath, string newPath)
        {
            if (newPath.Contains(".vs\\") || newPath.Contains(".git\\")) return;
            var remoteOldPath = ConvertToRemotePath(oldPath);
            var remoteNewPath = ConvertToRemotePath(newPath);
            using (var client = new SftpClient(host, username, password))
            {
                client.Connect();
                if (File.Exists(newPath))
                {
                    if (client.Exists(remoteOldPath))
                    {
                        client.RenameFile(remoteOldPath, remoteNewPath);
                    }
                    else
                    {
                        using(var file = File.OpenRead(newPath))
                        {
                            client.UploadFile(file, remoteNewPath);
                        }
                    }
                }
                else if (Directory.Exists(newPath))
                {
                    DeleteSingleFileOrDirectory(oldPath);
                    UploadSingleFileOrDirectory(newPath);
                }
            }
        }

        private string ConvertToRemotePath(string path)
        {
            var currentDirectoryList = Directory.GetCurrentDirectory().Split("\\").ToList();
            currentDirectoryList.RemoveAt(currentDirectoryList.Count - 1);
            currentDirectoryList.RemoveAt(currentDirectoryList.Count - 1);
            var currentDirectory = string.Join("\\", currentDirectoryList);
            return path.Replace($"{currentDirectory}\\", "").Replace("\\", "/");
        }

        public void UploadDirectory()
        {
            using (var client = new SftpClient(host, username, password))
            {
                client.Connect();
                client.ChangeDirectory(".local");
                if (!client.WorkingDirectory.EndsWith("root"))
                {
                    client.ChangeDirectory("/root");
                }
                DirectoryInfo info = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;
                if (!client.Exists(info.Name)) client.CreateDirectory(info.Name);
                client.ChangeDirectory(info.Name);
                foreach (var file in info.GetFiles())
                {
                    using (var fileStream = File.OpenRead(file.FullName))
                    {
                        client.UploadFile(fileStream, file.Name, true);
                    }
                }
                GetSubDirectories(info, client);
            }
        }

        private void GetSubDirectories(DirectoryInfo info, SftpClient client)
        {
            foreach (var subInfo in Directory.GetDirectories(info.FullName)
                .Where(x => x != Directory.GetCurrentDirectory() && !x.EndsWith(".vs") && !x.EndsWith(".git"))
                .Select(x => new DirectoryInfo(x)))
            {
                if (!client.Exists(subInfo.Name)) client.CreateDirectory(subInfo.Name);
                var prevDirectory = client.WorkingDirectory;
                client.ChangeDirectory(subInfo.Name);

                GetSubDirectories(subInfo, client);
                foreach (var file in subInfo.GetFiles())
                {
                    try
                    {
                        using (var fileStream = File.OpenRead(file.FullName))
                        {
                            client.UploadFile(fileStream, file.Name, true);
                        }
                    }
                    catch (IOException e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);
                        Console.ResetColor();
                    }
                }
                client.ChangeDirectory(prevDirectory);
            }
        }
    }
}
