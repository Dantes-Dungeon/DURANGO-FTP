// <copyright file="ControlConnectionSslFactory.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

#if NETSTANDARD2_1
namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// The implementation of <see cref="IControlConnectionSslFactory"/> using <see cref="SslStream"/>.
    /// </summary>
    public class ControlConnectionSslFactory : IControlConnectionSslFactory
    {
        private readonly X509Certificate certificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlConnectionSslFactory"/> class.
        /// </summary>
        /// <param name="certificate">The certificate for the SSL or TLS authenticate.</param>
        public ControlConnectionSslFactory(X509Certificate certificate)
        {
            this.certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        /// <summary>
        /// Disconnects the given stream.
        /// </summary>
        /// <param name="stream">The plain text or encrypted stream to disconnect.</param>
        /// <returns>The task of the async operation.</returns>
        public async Task DisconnectAsync(Stream stream)
        {
            await stream.DisposeAsync();
        }

        /// <summary>
        /// Upgrades a plain text stream to an encrypted stream.
        /// </summary>
        /// <param name="plainTextStream">The plain text stream to upgrade.</param>
        /// <returns>The task with the upgraded stream.</returns>
        public async Task<Stream> UpgradeAsync(Stream plainTextStream)
        {
            SslStream s = new SslStream(plainTextStream);
            await s.AuthenticateAsServerAsync(certificate);
            return s;
        }
    }
}
#endif
