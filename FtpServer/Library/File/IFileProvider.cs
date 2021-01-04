// <copyright file="IFileProvider.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// The interface of file provider for single user.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Gets the FTP working directory that starts with "/".
        /// </summary>
        /// <returns>The FTP working directory absolute path.</returns>
        string GetWorkingDirectory();

        /// <summary>
        /// Sets the FTP working directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of working directory.</param>
        /// <returns>Whether the setting succeeded or not.</returns>
        bool SetWorkingDirectory(string path);

        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the directory.</param>
        /// <returns>The task to await.</returns>
        Task CreateDirectoryAsync(string path);

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the directory.</param>
        /// <returns>The task to await.</returns>
        Task DeleteDirectoryAsync(string path);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The task to await.</returns>
        Task DeleteAsync(string path);

        /// <summary>
        /// Renames or moves a file or directory.
        /// </summary>
        /// <param name="fromPath">Absolute or relative FTP path of source file or directory.</param>
        /// <param name="toPath">Absolute or relative FTP path of target file or directory.</param>
        /// <returns>The task to await.</returns>
        Task RenameAsync(string fromPath, string toPath);

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
        /// <exception cref="FileBusyException">The file is temporarily unavailable.</exception>
        /// <exception cref="FileNoAccessException">The file can't be obtained.</exception>
        Task<Stream> OpenFileForReadAsync(string path);

        /// <summary>
        /// Opens a file for writing.
        /// If the file already exists, opens it instead.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
        Task<Stream> OpenFileForWriteAsync(string path);

        /// <summary>
        /// Creates a new file for writing.
        /// If the file already exists, replace it instead.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The file stream.</returns>
        /// <exception cref="FileSpaceInsufficientException">Failed to create the file and a retry will not succeed.</exception>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
        Task<Stream> CreateFileForWriteAsync(string path);

        /// <summary>
        /// Gets the names of files and directories.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The names of items.</returns>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
        /// <exception cref="FileNoAccessException">The listing can't be obtained.</exception>
        Task<IEnumerable<string>> GetNameListingAsync(string path);

        /// <summary>
        /// If the path is a directory, gets the info of its contents.
        /// If the path is a file, gets its info.
        /// </summary>
        /// <param name="path">Absolute or relative FTP path of the file.</param>
        /// <returns>The info of items in <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>.</returns>
        /// <exception cref="FileBusyException">The operation failed but worth a retry.</exception>
        /// <exception cref="FileNoAccessException">The listing can't be obtained.</exception>
        Task<IEnumerable<FileSystemEntry>> GetListingAsync(string path);
    }
}
