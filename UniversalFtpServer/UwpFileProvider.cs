using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Composition;
using Zhaobang.FtpServer.File;

namespace UniversalFtpServer
{
    class UwpFileProvider : IFileProvider
    {

        string workFolder = string.Empty;

        Dictionary<string, List<string>> appPaths = new Dictionary<string, List<string>>();

        public UwpFileProvider()
        {
            this.appPaths = GenerateAppPaths();
        }

        public Dictionary<string, List<string>> GenerateAppPaths()
        {
            Dictionary<string, List<string>> paths = new Dictionary<string, List<string>>();
            Dictionary<string,string> filePatterns = new Dictionary<string,string>()
            {
                {"Retroarch","retroarch.cfg"},
                {"DuckStation","settings.ini"},
                {"Flycast", "emu.cfg"}
            };
            Dictionary<string, string> directoryPatterns = new Dictionary<string, string>()
            {
                {"PPSSPP", "PSP"},
                {"PCSX2", "cheats_ws"},
                {"XBSX2", "widescreenpatches"},
                {"Minecraft", "games"}
            };

            foreach (string name in filePatterns.Keys)
                paths.Add(name, new List<string>());
            foreach (string name in directoryPatterns.Keys)
                paths.Add(name, new List<string>());

            List<MonitoredFolderItem> monfiles = PinvokeFilesystem.GetItems(Path.Combine(UserDataPaths.GetForUser(App.user).LocalAppData, "Packages"));
            List<MonitoredFolderItem> totalMonitoredFolderItems = new List<MonitoredFolderItem>();
            foreach (MonitoredFolderItem monfile in monfiles)
            {
                if (monfile.IsDir)
                {
                    List<MonitoredFolderItem> appmonfiles = PinvokeFilesystem.GetItems(Path.Combine(monfile.ParentFolderPath, monfile.Name, "LocalState"));
                    totalMonitoredFolderItems.AddRange(appmonfiles);
                }
            }
            foreach (KeyValuePair<string, string> filePattern in filePatterns)
            {
                foreach (MonitoredFolderItem item in totalMonitoredFolderItems)
                {
                    if (!item.IsDir)
                    {
                        if (item.Name == filePattern.Value)
                            paths[filePattern.Key].Add(Path.GetFileName(Path.GetDirectoryName(item.ParentFolderPath)));

                    }
                }
            }

            foreach (KeyValuePair<string, string> directoryPattern in directoryPatterns)
            {
                foreach (MonitoredFolderItem item in totalMonitoredFolderItems)
                {
                    if (item.IsDir)
                    {
                        if (item.Name == directoryPattern.Value)
                            paths[directoryPattern.Key].Add(Path.GetFileName(Path.GetDirectoryName(item.ParentFolderPath)));

                    }
                }
            }
            return paths;
        }

        public async Task CreateDirectoryAsync(string path)
        {
            string fullPath = GetLocalVfsPath(path);
            await RecursivelyCreateDirectoryAsync(fullPath);
        }

        public async Task<bool> SetFileModificationTimeAsync(string path, DateTime newTime)
        {
            path = GetLocalVfsPath(path);
            var pathexists = ItemExists(path);
            if (pathexists)
            {
                await Task.Run(() => { PinvokeFilesystem.SetFileModificationTime(path, newTime); });
                return true;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public async Task<DateTime> GetFileModificationTimeAsync(string path)
        {
            path = GetLocalVfsPath(path);
            var pathexists = ItemExists(path);
            if (pathexists)
            {
                return await Task.Run(() => {return PinvokeFilesystem.GetFileModificationTime(path); });
            } 
            else
            {
                throw new FileNotFoundException();
            }
        }

        public async Task<Stream> CreateFileForWriteAsync(string path)
        {
            path = GetLocalVfsPath(path);
            string parentPath = Path.GetDirectoryName(path);
            var parentexists = ItemExists(parentPath);
            if (!parentexists)
            {
                await RecursivelyCreateDirectoryAsync(parentPath);
            }
            IntPtr hStream = PinvokeFilesystem.CreateFileFromApp(path, PinvokeFilesystem.GENERIC_WRITE, 0, IntPtr.Zero, PinvokeFilesystem.CREATE_ALWAYS, (uint)PinvokeFilesystem.File_Attributes.BackupSemantics, IntPtr.Zero);
            return new FileStream(hStream, FileAccess.Write);
        }

        public async Task RecursivelyCreateDirectoryAsync(string path)
        {
            string parentPath = System.IO.Directory.GetParent(path).ToString();
            var itemexists = ItemExists(parentPath);
            if (!itemexists) 
            { 
                await RecursivelyCreateDirectoryAsync(parentPath);
            }
            await Task.Run(() => {PinvokeFilesystem.CreateDirectoryFromApp((@"\\?\" + path), IntPtr.Zero);});
        }


        public bool ItemExists(string path) 
        {
            PinvokeFilesystem.GetFileAttributesExFromApp((@"\\?\" + path), PinvokeFilesystem.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo);
            if (lpFileInfo.dwFileAttributes != 0)
            {
                return true;
            } else {
                return false;
            }
        }

        public async Task DeleteAsync(string path)
        {
            path = GetLocalVfsPath(path);
            PinvokeFilesystem.GetFileAttributesExFromApp((@"\\?\" + path), PinvokeFilesystem.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo);
            //this handling is only neccessary as winscp for some reason sends directory delete commands as file delete commands
            if (lpFileInfo.dwFileAttributes.HasFlag(System.IO.FileAttributes.Directory))
            {
                await RecursivelyDeleteDirectoryAsync(path);
            }
            else if (lpFileInfo.dwFileAttributes != 0)
            {
                await Task.Run(() => { PinvokeFilesystem.DeleteFileFromApp(@"\\?\" + path); });
            } else {
                throw new FileBusyException("Items of unknown type can't be deleted");
            }
            MainPage.Current.RefreshStorage();
        }

        public async Task DeleteDirectoryAsync(string path)
        {
            path = GetLocalVfsPath(path);
            await RecursivelyDeleteDirectoryAsync(path);
            MainPage.Current.RefreshStorage();
        }

        public async Task RecursivelyDeleteDirectoryAsync(string path) 
        {
            List<MonitoredFolderItem> mininfo = PinvokeFilesystem.GetMinInfo(path);
            foreach (MonitoredFolderItem item in mininfo) 
            {
                string itempath = System.IO.Path.Combine(item.ParentFolderPath, item.Name);
                if (item.attributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    await RecursivelyDeleteDirectoryAsync(itempath);
                } else {
                    await Task.Run(()=> { PinvokeFilesystem.DeleteFileFromApp(@"\\?\" + itempath); });
                }
            }
            await Task.Run(() => { PinvokeFilesystem.RemoveDirectoryFromApp(@"\\?\" + path); });
        }

        public Task<IEnumerable<FileSystemEntry>> GetListingAsync(string path)
        {
            if (path == "-a" || path == "-al") 
            {
                path = "";
            }
            string fullPath = GetLocalPath(path);

            //create empty list of filesystem entries
            List<FileSystemEntry> result = new List<FileSystemEntry>();

            //check if the current path is the root of the virtual filesystem
            // the \\- check is to make sure that it is not a sub command being entered
            if (fullPath == "" || fullPath.StartsWith("\\-"))
            {
                //if in this if statement then the check returned true

                //add entry for the so called Local folder - used to access apps local data
                FileSystemEntry localentry = new FileSystemEntry()
                {
                    IsDirectory = true,
                    IsReadOnly = true,
                    LastWriteTime = DateTime.Now,
                    Length = 0,
                    Name = "LOCALFOLDER"
                };
                //add the entry that was just created

                result.Add(localentry);

                //add entry for the DRIVES
                localentry = new FileSystemEntry()
                {
                    IsDirectory = true,
                    IsReadOnly = true,
                    LastWriteTime = DateTime.Now,
                    Length = 0,
                    Name = "DRIVES"
                };

                result.Add(localentry);

                //add entry for the APPS
                localentry = new FileSystemEntry()
                {
                    IsDirectory = true,
                    IsReadOnly = true,
                    LastWriteTime = DateTime.Now,
                    Length = 0,
                    Name = "APPS"
                };

                result.Add(localentry);
            } else if (fullPath == "DRIVES") {
                //get all drives
                List<string> drives = PinvokeFilesystem.GetDrives();
                //loop through all the drives
                foreach (string drive in drives)
                {
                    //create an entry for the current drive
                    FileSystemEntry entry = new FileSystemEntry()
                    {
                        IsDirectory = true,
                        IsReadOnly = true,
                        LastWriteTime = DateTime.Now,
                        Length = 0,
                        Name = drive
                    };
                    //add the entry
                    result.Add(entry);
                }
            } else if (fullPath == "APPS") {
                foreach (string name in appPaths.Keys)
                {
                    FileSystemEntry localentry = new FileSystemEntry()
                    {
                        IsDirectory = true,
                        IsReadOnly = true,
                        LastWriteTime = DateTime.Now,
                        Length = 0,
                        Name = name
                    };

                    result.Add(localentry);
                }
            } else if (fullPath.StartsWith("APPS") && fullPath.Split('\\','/').Length == 2) {
                string[] folders = fullPath.Split('\\', '/');
                foreach (string name in appPaths[folders[1]])
                {
                    FileSystemEntry localentry = new FileSystemEntry()
                    {
                        IsDirectory = true,
                        IsReadOnly = true,
                        LastWriteTime = DateTime.Now,
                        Length = 0,
                        Name = name
                    };

                    result.Add(localentry);
                }
            } else {
                string reformatedpath = GetLocalVfsPath(path);
                List<MonitoredFolderItem> monfiles = PinvokeFilesystem.GetItems(reformatedpath);
                foreach (var item in monfiles)
                {
                    switch (item.Name) 
                    {
                        case ".":
                            break;
                        case "..":
                            break;
                        case "﻿":
                            break;
                        default:
                            FileSystemEntry entry = new FileSystemEntry()
                            {
                                IsDirectory = item.IsDir,
                                IsReadOnly = item.attributes.HasFlag(System.IO.FileAttributes.ReadOnly),
                                LastWriteTime = item.DateModified.ToUniversalTime().DateTime,
                                Length = (long)item.Size,
                                Name = item.Name
                            };
                            result.Add(entry);
                            break;
                    }
                    
                }   
            }

            //return list of entries
            MainPage.Current.RefreshStorage();
            return Task.FromResult(result.AsEnumerable());
        }

        public Task<IEnumerable<string>> GetNameListingAsync(string path)
        {
            path = GetLocalVfsPath(path);
            List<string> result = PinvokeFilesystem.GetNames(path);
            return Task.FromResult(result.AsEnumerable());
        }

        public string GetWorkingDirectory()
        {
            return "/" + workFolder;
        }

        public async Task<Stream> OpenFileForReadAsync(string path) 
        {
            path = GetLocalVfsPath(path);
            try
            {
                IntPtr hStream = PinvokeFilesystem.CreateFileFromApp((@"\\?\" + path), PinvokeFilesystem.GENERIC_READ, 0, IntPtr.Zero, PinvokeFilesystem.OPEN_ALWAYS, 4, IntPtr.Zero);
                return new FileStream(hStream, FileAccess.Read);
            }
            catch 
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                return await file.OpenStreamForReadAsync();
            }
        }

        public async Task<Stream> OpenFileForWriteAsync(string path)
        {
            path = GetLocalVfsPath(path);
            try
            {
                IntPtr hStream = PinvokeFilesystem.CreateFileFromApp((@"\\?\" + path), PinvokeFilesystem.GENERIC_WRITE | PinvokeFilesystem.GENERIC_READ, 0, IntPtr.Zero, PinvokeFilesystem.OPEN_ALWAYS, (uint)PinvokeFilesystem.File_Attributes.BackupSemantics, IntPtr.Zero);
                return new FileStream(hStream, FileAccess.ReadWrite);
            } catch {
                var file = await StorageFile.GetFileFromPathAsync(path);
                return await file.OpenStreamForWriteAsync();
            }
        }

        public async Task RenameAsync(string fromPath, string toPath)
        {
            //get full path from parameter "fromPath"
            fromPath = GetLocalVfsPath(fromPath);
            toPath = GetLocalVfsPath(toPath);

            PinvokeFilesystem.GetFileAttributesExFromApp((@"\\?\" + fromPath), PinvokeFilesystem.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo);
            if (lpFileInfo.dwFileAttributes == 0)
            {
                throw new FileNoAccessException("Can't find the item to rename");
            } else {
                if (lpFileInfo.dwFileAttributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    PinvokeFilesystem.CreateDirectoryFromApp(@"\\?\" + toPath, IntPtr.Zero);
                    if (!(await MoveFolderAsync(fromPath, toPath)))
                        throw new FileBusyException("Some items can't be moved");
                } else {
                    await Task.Run(() =>
                    {
                        PinvokeFilesystem.CopyFileFromApp(fromPath, toPath, false);
                        PinvokeFilesystem.DeleteFileFromApp(fromPath);
                    });
                }
            }
        }

        private async Task<bool> MoveFolderAsync(string folder, string destination, bool firstcall = true)
        {

            List<MonitoredFolderItem> mininfo = PinvokeFilesystem.GetMinInfo(folder);
            foreach (MonitoredFolderItem item in mininfo)
            {
                string itempath = System.IO.Path.Combine(item.ParentFolderPath, item.Name);
                string targetpath = System.IO.Path.Combine(destination, item.Name);
                if (item.attributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    await Task.Run(() =>
                    {
                        PinvokeFilesystem.CreateDirectoryFromApp(@"\\?\" + targetpath, IntPtr.Zero);
                    });
                    await MoveFolderAsync(itempath, targetpath, false);
                }
                else
                {
                    //move file
                    await Task.Run(() => 
                    {
                        PinvokeFilesystem.CopyFileFromApp(@"\\?\" + itempath, @"\\?\" + targetpath, false);
                        PinvokeFilesystem.DeleteFileFromApp(@"\\?\" + itempath); 
                    });
                }
            }
            mininfo = PinvokeFilesystem.GetMinInfo(folder);
            if (mininfo.Count() == 0)
            {
                await Task.Run(() => { PinvokeFilesystem.RemoveDirectoryFromApp(@"\\?\" + folder); });
                if (firstcall)
                    MainPage.Current.RefreshStorage();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetWorkingDirectory(string path)
        {
            try
            {
                string localPath = GetLocalPath(path);
                workFolder = GetFtpPath(localPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetLocalVfsPath(string path)
        {
            //get full path from parameter "toPath"
            string toFullPath = GetLocalPath(path);
            //split the path into each folder
            var localpath = toFullPath.Split("\\").ToList();
            if (toFullPath.StartsWith("DRIVES"))
            {
                //add the colon for file access
                localpath.RemoveAt(0);
                toFullPath = String.Join("\\", localpath);
                toFullPath = toFullPath.Insert(1, ":");
            }
            else
            {
                if(toFullPath.StartsWith("APPS"))
                    localpath.RemoveAt(0);
                //get local state folder
                localpath[0] = UserDataPaths.GetForUser(App.user).LocalAppData + "\\Packages";
                //rejoin the parts of the path together
                toFullPath = String.Join("\\", localpath);
            }
            //retrim end of string, just as a precautionary measure
            //I tried implementing this as a trim end but it did not seem to work
            if (toFullPath.EndsWith("\\"))
            {
                toFullPath = toFullPath.Substring(0, toFullPath.Length - "\\".Length);
            }

            return toFullPath;
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="SecurityException"/>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="PathTooLongException"/>
        private string GetLocalPath(string path)
        {
            return Path.Combine(workFolder, path).TrimStart('/', '\\').TrimEnd('/', '\\').Replace("/", "\\");
        }

        private string GetFtpPath(string localPath)
        {
            return localPath.TrimEnd('/', '\\').TrimStart('/', '\\').Replace('\\', '/');
        }
    }
}
