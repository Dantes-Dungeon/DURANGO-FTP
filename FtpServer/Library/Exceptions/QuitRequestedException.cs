// <copyright file="QuitRequestedException.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.Exceptions
{
    /// <summary>
    /// The user requested to quit.
    /// </summary>
    internal class QuitRequestedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuitRequestedException"/> class.
        /// </summary>
        public QuitRequestedException()
            : base("The user has requested to quit")
        {
        }
    }
}
