// <copyright file="IDataConnection.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// The interface of a manager of FTP data connection.
    /// Each user has one instance.
    /// </summary>
    public interface IDataConnection
    {
        /// <summary>
        /// Gets a value indicating whether a data connection is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the supported protocal IDs in active mode (defined in RFC 2824).
        /// </summary>
        IEnumerable<int> SupportedActiveProtocal { get; }

        /// <summary>
        /// Gets the supported protocal IDs in passive mode (defined in RFC 2824).
        /// </summary>
        IEnumerable<int> SupportedPassiveProtocal { get; }

        /// <summary>
        /// Initiates a data connection in FTP active mode.
        /// </summary>
        /// <param name="remoteIP">The IP to connect to.</param>
        /// <param name="remotePort">The port to connect to.</param>
        /// <param name="protocal">Protocal ID defined in RFC 2428.</param>
        /// <returns>The task to await.</returns>
        Task ConnectActiveAsync(IPAddress remoteIP, int remotePort, int protocal);

        /// <summary>
        /// Listens for FTP passive connection and returns the listening end point.
        /// </summary>
        /// <returns>The end point listening at.</returns>
        IPEndPoint Listen();

        /// <summary>
        /// Listens for FTP EPSV connection and returns the listening port.
        /// </summary>
        /// <param name="protocal">The protocal ID to use. Defined in RFC 2824.</param>
        /// <returns>The port listening at.</returns>
        int ExtendedListen(int protocal);

        /// <summary>
        /// Accepts a FTP passive mode connection.
        /// </summary>
        /// <returns>The task to await.</returns>
        Task AcceptAsync();

        /// <summary>
        /// Disconnects any open connection.
        /// </summary>
        /// <returns>The task to await.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Copies content to data connection.
        /// </summary>
        /// <param name="streamToRead">The stream to copy from.</param>
        /// <returns>The task to await.</returns>
        Task SendAsync(Stream streamToRead);

        /// <summary>
        /// Copies content from data connection.
        /// </summary>
        /// <param name="streamToWrite">The stream to copy to.</param>
        /// <returns>The task to await.</returns>
        Task RecieveAsync(Stream streamToWrite);

        /// <summary>
        /// Close the connection and listener.
        /// </summary>
        void Close();
    }
}
