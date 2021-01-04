// <copyright file="FileNoAccessException.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// Exception indicating that file action failed and will not
    /// success when retrying (e.g., file not found or unauthorized access).
    /// Causing FTP reply code 550.
    /// </summary>
    public class FileNoAccessException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileNoAccessException"/> class.
        /// </summary>
        public FileNoAccessException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileNoAccessException"/> class with given message.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        public FileNoAccessException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileNoAccessException"/> class with given message and inner exception.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public FileNoAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
