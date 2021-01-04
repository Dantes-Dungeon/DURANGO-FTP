// <copyright file="SslLocalDataConnectionFactory.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#if NETSTANDARD2_1
namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// The implementation of SSL or TLS data connection factory using <see cref="System.Net.Security.SslStream"/>.
    /// </summary>
    public class SslLocalDataConnectionFactory : IDataConnectionFactory
    {
        private readonly X509Certificate certificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslLocalDataConnectionFactory"/> class.
        /// </summary>
        /// <param name="certificate">The certificate for the SSL or TLS stream.</param>
        public SslLocalDataConnectionFactory(X509Certificate certificate)
        {
            this.certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        /// <summary>
        /// Gets the data connection instance.
        /// </summary>
        /// <param name="localIP">The local IP to bind the socket.</param>
        /// <returns>The created data connection instance.</returns>
        public IDataConnection GetDataConnection(IPAddress localIP)
        {
            return new SslLocalDataConnection(localIP, certificate);
        }
    }
}
#endif
