// <copyright file="SimpleFileProvider.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// Provides the same root directory to all users.
    /// </summary>
    public class SimpleFileProvider : IFileProvider
    {
        /// <summary>
        /// The root directory for ftp.
        /// </summary>
        private readonly string baseDirectory;

        /// <summary>
        /// The remote working directory path, without '/' at start.
        /// Use <see cref="GetLocalPath(string)"/> to get local
        /// working directory.
        /// </summary>
        private string workingDirectory = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleFileProvider"/> class.
        /// </summary>
        /// <param name="baseDirectory">The root directory of the user.</param>
        public SimpleFileProvider(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                throw new IOException("Base directory doesn't exist");
            }
            this.baseDirectory = baseDirectory;
        }

        /// <summary>
        /// Gets the FTP working directory.
        /// </summary>
        /// <returns>The FTP working directory absolute path.</returns>
        public string GetWorkingDirectory()
        {
            return "/" + workingDirectory;
        }

        /// <summary>
        /// Sets the FTP working directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of working directory.</param>
        /// <returns>Whether the setting succeeded or not.</returns>
        public bool SetWorkingDirectory(string path)
        {
            try
            {
                var localPath = GetLocalPath(path);
                if (Directory.Exists(localPath))
                {
                    workingDirectory = GetFtpPath(localPath);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the directory.</param>
        /// <returns>The task to await.</returns>
#pragma warning disable CS1998
        public async Task CreateDirectoryAsync(string path)
#pragma warning restore CS1998
        {
            Directory.CreateDirectory(GetLocalPath(path));
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the directory.</param>
        /// <returns>The task to await.</returns>
#pragma warning disable CS1998
        public async Task DeleteDirectoryAsync(string path)
#pragma warning restore CS1998
        {
            var localPath = GetLocalPath(path);
            if (localPath == GetLocalPath(baseDirectory))
            {
                throw new UnauthorizedAccessException("User tried to delete base directory");
            }
            Directory.Delete(localPath, true);
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The task to await.</returns>
#pragma warning disable CS1998
        public async Task DeleteAsync(string path)
#pragma warning restore CS1998
        {
            System.IO.File.Delete(GetLocalPath(path));
        }

        /// <summary>
        /// Renames or moves a file or directory.
        /// </summary>
        /// <param name="fromPath">Absolute or relative FTP path of source file or directory.</param>
        /// <param name="toPath">Absolute or relative FTP path of target file or directory.</param>
        /// <returns>The task to await.</returns>
#pragma warning disable CS1998
        public async Task RenameAsync(string fromPath, string toPath)
#pragma warning restore CS1998
        {
            var fromLocalPath = GetLocalPath(fromPath);
            var toLocalPath = GetLocalPath(toPath);
            Directory.Move(fromLocalPath, toLocalPath);
        }

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
        /// <exception cref="FileBusyException">The file is temporarily unavailable.</exception>
        /// <exception cref="FileNoAccessException">The file can't be obtained.</exception>
#pragma warning disable CS1998
        public async Task<Stream> OpenFileForReadAsync(string path)
#pragma warning restore CS1998
        {
            try
            {
                string localPath = GetLocalPath(path);
                return System.IO.File.OpenRead(localPath);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is UnauthorizedAccessException ||
                    ex is PathTooLongException ||
                    ex is DirectoryNotFoundException ||
                    ex is FileNotFoundException ||
                    ex is NotSupportedException)
                    throw new FileNoAccessException(ex.Message, ex);
                else
                    throw new FileBusyException(ex.Message);
            }
        }

        /// <summary>
        /// Opens a file for writing.
        /// If the file already exists, opens it instead.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
#pragma warning disable CS1998
        public async Task<Stream> OpenFileForWriteAsync(string path)
#pragma warning restore CS1998
        {
            string localPath = GetLocalPath(path);
            return System.IO.File.OpenWrite(localPath);
        }

        /// <summary>
        /// Creates a new file for writing.
        /// If the file already exists, replace it instead.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
        /// <exception cref="FileSpaceInsufficientException">Failed to create the file and a retry will not succeed.</exception>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
#pragma warning disable CS1998
        public async Task<Stream> CreateFileForWriteAsync(string path)
#pragma warning restore CS1998
        {
            try
            {
                string localPath = GetLocalPath(path);
                return System.IO.File.Create(localPath);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is UnauthorizedAccessException ||
                    ex is PathTooLongException ||
                    ex is DirectoryNotFoundException ||
                    ex is NotSupportedException)
                    throw new FileSpaceInsufficientException(ex.Message, ex);
                else
                    throw new FileBusyException(ex.Message);
            }
        }

        /// <summary>
        /// Gets the names of files and directories.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The names of items.</returns>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
        /// <exception cref="FileNoAccessException">The listing can't be obtained.</exception>
#pragma warning disable CS1998
        public async Task<IEnumerable<string>> GetNameListingAsync(string path)
#pragma warning restore CS1998
        {
            try
            {
                string localPath = GetLocalPath(path);
                return Directory.EnumerateFileSystemEntries(localPath);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is PathTooLongException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException ||
                    ex is DirectoryNotFoundException ||
                    ex is IOException)
                    throw new FileNoAccessException(ex.Message, ex);
                else
                    throw new FileBusyException(ex.Message, ex);
            }
        }

        /// <summary>
        /// If the path is a directory, gets the info of its contents.
        /// If the path is a file, gets its info.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The info of items in <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>.</returns>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
        /// <exception cref="FileNoAccessException">The listing can't be obtained.</exception>
#pragma warning disable CS1998
        public async Task<IEnumerable<FileSystemEntry>> GetListingAsync(string path)
#pragma warning restore CS1998
        {
            try
            {
                string localPath = GetLocalPath(path);
                try
                {
                    FileInfo fileInfo = new FileInfo(localPath);
                    return new[]
                    {
                        new FileSystemEntry
                        {
                            Name = fileInfo.Name,
                            IsDirectory = false,
                            Length = fileInfo.Length,
                            IsReadOnly = fileInfo.IsReadOnly,
                            LastWriteTime = fileInfo.LastWriteTime,
                        },
                    };
                }
                catch (FileNotFoundException)
                {
                    // If the path doesn't refer to a file, try if it refers to a directory.
                }

                var directories = Directory.GetDirectories(localPath)
                    .Select(x => new DirectoryInfo(x))
                    .Select(x => new FileSystemEntry
                    {
                        Name = x.Name,
                        IsDirectory = true,
                        Length = 0,
                        IsReadOnly = false,
                        LastWriteTime = x.LastWriteTime,
                    });
                var files = Directory.GetFiles(localPath)
                    .Select(x => new FileInfo(x))
                    .Select(x => new FileSystemEntry
                    {
                        Name = x.Name,
                        IsDirectory = false,
                        Length = x.Length,
                        IsReadOnly = x.IsReadOnly,
                        LastWriteTime = x.LastWriteTime,
                    });
                return directories.Concat<FileSystemEntry>(files);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is PathTooLongException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException ||
                    ex is DirectoryNotFoundException)
                    throw new FileNoAccessException(ex.Message, ex);
                else
                    throw new FileBusyException(ex.Message, ex);
            }
        }

        /// <exception cref="ArgumentException">
        /// By <see cref="Path.Combine(string, string)"/>: path1 or path2 contains one or more of the invalid characters defined in System.IO.Path.GetInvalidPathChars.
        /// By <see cref="Path.GetFullPath(string)"/>: path is a zero-length string, contains only white space, or contains one or more
        /// of the invalid characters defined in System.IO.Path.GetInvalidPathChars. -or-
        /// The system could not retrieve the absolute path.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="path"/> tries to access parent folders.
        /// </exception>
        /// <exception cref="SecurityException">
        /// By <see cref="Path.GetFullPath(string)"/>: The caller does not have the required permissions.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// By <see cref="Path.GetFullPath(string)"/>: path contains a colon (":") that is not part of a volume identifier (for example,
        /// "c:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// By <see cref="Path.GetFullPath(string)"/>: The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        private string GetLocalPath(string path)
        {
            string fullPath = Path.Combine(workingDirectory, path).TrimStart('/', '\\');
            string localPath = Path.GetFullPath(Path.Combine(baseDirectory, fullPath)).TrimEnd('/', '\\');
            string baseLocalPath = Path.GetFullPath(baseDirectory).TrimEnd('/', '\\');
            if (!Path.GetFullPath(localPath).Contains(baseLocalPath))
                throw new UnauthorizedAccessException("User tried to access out of base directory");
            return localPath;
        }

        private string GetFtpPath(string localPath)
        {
            string localFullPath = Path.GetFullPath(localPath).TrimEnd('/', '\\');
            string baseFullPath = Path.GetFullPath(baseDirectory).TrimEnd('/', '\\');
            if (!localFullPath.Contains(baseFullPath))
            {
                throw new UnauthorizedAccessException("User tried to access out of base directory");
            }
            return localFullPath.Replace(baseFullPath, string.Empty).TrimStart('/', '\\').Replace('\\', '/');
        }
    }
}
