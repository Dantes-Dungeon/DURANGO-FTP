// <copyright file="FileBusyException.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// Exception indicating that file action failed and may work
    /// when retrying. Causing FTP reply code 450.
    /// </summary>
    public class FileBusyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileBusyException"/> class.
        /// </summary>
        public FileBusyException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBusyException"/> class with given message.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        public FileBusyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBusyException"/> class with given message and inner exception.
        /// </summary>
        /// <param name="message">Description of exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public FileBusyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
