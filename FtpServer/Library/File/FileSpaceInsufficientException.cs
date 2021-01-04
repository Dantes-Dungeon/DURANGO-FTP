// <copyright file="FileSpaceInsufficientException.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// Error when writing to file that will occur when retrying.
    /// </summary>
    public class FileSpaceInsufficientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpaceInsufficientException"/> class.
        /// </summary>
        public FileSpaceInsufficientException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpaceInsufficientException"/> class.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        public FileSpaceInsufficientException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpaceInsufficientException"/> class.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public FileSpaceInsufficientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
