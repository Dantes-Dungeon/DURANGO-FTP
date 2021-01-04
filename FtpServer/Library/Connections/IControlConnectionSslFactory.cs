// <copyright file="IControlConnectionSslFactory.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// The factory to upgrade a stream to an encrypted stream.
    /// </summary>
    public interface IControlConnectionSslFactory
    {
        /// <summary>
        /// Upgrades a plain text stream to an encrypted stream.
        /// </summary>
        /// <param name="plainTextStream">The plain text stream to upgrade.</param>
        /// <returns>The task with the upgraded stream.</returns>
        Task<Stream> UpgradeAsync(Stream plainTextStream);

        /// <summary>
        /// Disconnects the given stream.
        /// </summary>
        /// <param name="sslStream">The plain text or encrypted stream to disconnect.</param>
        /// <returns>The task of the async operation.</returns>
        Task DisconnectAsync(Stream sslStream);
    }
}
