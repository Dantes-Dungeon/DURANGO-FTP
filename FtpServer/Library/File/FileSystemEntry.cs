// <copyright file="FileSystemEntry.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// A class that holds information of a file or directory.
    /// </summary>
    public class FileSystemEntry
    {
        /// <summary>
        /// Gets or sets the name of the file or directory.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the last write time of the file or directory.
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets or sets the length of the file, or any value for directory.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entry is a directory.
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entry is read only.
        /// </summary>
        public bool IsReadOnly { get; set; }
    }
}
