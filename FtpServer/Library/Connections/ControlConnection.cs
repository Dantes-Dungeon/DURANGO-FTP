// <copyright file="ControlConnection.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zhaobang.FtpServer.Connections;
using Zhaobang.FtpServer.Exceptions;
using Zhaobang.FtpServer.File;

namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// Used to maintain the FTP control connection.
    /// </summary>
    internal class ControlConnection : IDisposable
    {
        private const int ReadByteBufferLength = 12;
        private const int ReadCharBufferLength = 12;

        private readonly FtpServer server;
        private readonly TcpClient tcpClient;

        private readonly IPEndPoint remoteEndPoint;
        private readonly IPEndPoint localEndPoint;

        /// <summary>
        /// This should be available all time, but needs to check
        /// <see cref="LocalDataConnection.IsOpen"/> before usage.
        /// </summary>
        private readonly IDataConnection dataConnection;

        private Stream stream;

        private Encoding encoding = Encoding.UTF8;
        private string userName = string.Empty;
        private bool authenticated;

        private byte[] readByteBuffer = new byte[ReadByteBufferLength];
        private char[] readCharBuffer = new char[ReadCharBufferLength];
        private int readOffset = 0;

        private DataConnectionMode dataConnectionMode = DataConnectionMode.Active;
        private int userActiveProtocal = 1;
        private IPAddress userActiveIP;
        private int userActiveDataPort = 20;

        /// <summary>
        /// This is relevant to user, and should be available if
        /// and only if <see cref="authenticated"/> is true.
        /// </summary>
        private IFileProvider fileProvider;

        /// <summary>
        /// Only stream mode is supported.
        /// </summary>
        private TransmissionMode transmissionMode = TransmissionMode.Stream;

        /// <summary>
        /// This is ignored.
        /// </summary>
        private DataType dataType = DataType.ASCII;

        private ListFormat listFormat = ListFormat.Unix;

        private bool useSecureDataConnection = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlConnection"/> class.
        /// Used by <see cref="FtpServer"/> to create a control connection.
        /// </summary>
        /// <param name="server">The <see cref="FtpServer"/> that creates the connection.</param>
        /// <param name="tcpClient">The TCP client of the connection.</param>
        internal ControlConnection(FtpServer server, TcpClient tcpClient)
        {
            this.server = server;
            this.tcpClient = tcpClient;

            var remoteUri = new Uri("ftp://" + this.tcpClient.Client.RemoteEndPoint.ToString());
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteUri.Host), remoteUri.Port);
            userActiveDataPort = remoteEndPoint.Port;
            userActiveIP = remoteEndPoint.Address;

            var localUri = new Uri("ftp://" + this.tcpClient.Client.LocalEndPoint.ToString());
            localEndPoint = new IPEndPoint(IPAddress.Parse(localUri.Host), localUri.Port);

            dataConnection = server.DataConnector.GetDataConnection(localEndPoint.Address);

            stream = this.tcpClient.GetStream();
        }

        private enum ListFormat
        {
            Unix,
            MsDos,
        }

        private enum TransmissionMode
        {
            Stream,
        }

        private enum DataType
        {
            ASCII,
            IMAGE,
        }

        private enum DataConnectionMode
        {
            Passive,
            Active,
            ExtendedPassive,
            ExtendedActive,
        }

        /// <summary>
        /// Defined in page 40 in RFC 959.
        /// </summary>
        private enum FtpReplyCode
        {
            CommandOkay = 200,
            SystemStatus = 211,
            CommandUnrecognized = 500,
            SyntaxErrorInParametersOrArguments = 501,
            NotImplemented = 502,
            ParameterNotImplemented = 504,
            BadSequence = 503,
            ServiceReady = 220,
            UserLoggedIn = 230,
            NotLoggedIn = 530,
            NeedPassword = 331,
            LocalError = 451,
            PathCreated = 257,
            TransferStarting = 125,
            SuccessClosingDataConnection = 226,
            FileActionOk = 250,
            FileBusy = 450,
            FileNoAccess = 550,
            FileSpaceInsufficient = 452,
            EnteringPassiveMode = 227,
            EnteringEpsvMode = 229,
            AboutToOpenDataConnection = 150,
            NameSystemType = 215,
            FileActionPendingInfo = 350,
            NotSupportedProtocal = 522,
            ProceedWithNegotiation = 234,
        }

        /// <summary>
        /// Dispose of all the connections.
        /// </summary>
        public void Dispose()
        {
            tcpClient.Dispose();
            dataConnection.Close();
        }

        /// <summary>
        /// Starts the control connection.
        /// </summary>
        /// <remarks>Can only be used once.</remarks>
        /// <param name="cancellationToken">Token to terminate the control connection.</param>
        /// <returns>The task that finishes when control connection is closed.</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            server.Tracer.TraceUserConnection(remoteEndPoint);
            try
            {
                await ReplyAsync(FtpReplyCode.ServiceReady, "FtpServer by Taoyou is now ready");

                while (true)
                {
                    var command = await ReadLineAsync();
                    try
                    {
                        await ProcessCommandAsync(command);
                    }
                    catch (QuitRequestedException) { return; }
                    catch (Exception ex)
                    {
                        await ReplyAsync(
                            FtpReplyCode.LocalError,
                            string.Format("Exception thrown, message: {0}", ex.Message).Replace('\r', ' ').Replace('\n', ' '));
                    }
                    if (cancellationToken.IsCancellationRequested)
                        return;
                }
            }
            finally
            {
                Dispose();
                server.Tracer.TraceUserDisconnection(remoteEndPoint);
            }
        }

        private async Task ProcessCommandAsync(string message)
        {
            var messageSegs = message.Split(new[] { ' ' }, 2);
            var command = messageSegs[0];
            var parameter = messageSegs.Length < 2 ? string.Empty : messageSegs[1];

            server.Tracer.TraceCommand(command, remoteEndPoint);
            switch (command.ToUpper())
            {
                case "RNFR":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await CommandRnfrAsync(parameter);
                    return;
                case "RNTO":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await ReplyAsync(FtpReplyCode.BadSequence, "Should use RNFR first");
                    return;
                case "DELE":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await fileProvider.DeleteAsync(parameter);
                    await ReplyAsync(FtpReplyCode.FileActionOk, "Delete succeeded");
                    return;
                case "RMD":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await fileProvider.DeleteDirectoryAsync(parameter);
                    await ReplyAsync(FtpReplyCode.FileActionOk, "Directory deleted");
                    return;
                case "MKD":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await fileProvider.CreateDirectoryAsync(parameter);
                    await ReplyAsync(
                        FtpReplyCode.PathCreated,
                        string.Format(
                            "\"{0}\"",
                            fileProvider.GetWorkingDirectory().Replace("\"", "\"\"")));
                    return;
                case "PWD":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    await ReplyAsync(
                        FtpReplyCode.PathCreated,
                        string.Format(
                            "\"{0}\"",
                            fileProvider.GetWorkingDirectory().Replace("\"", "\"\"")));
                    return;
                case "SYST":
                    await ReplyAsync(FtpReplyCode.NameSystemType, "UNIX simulated by .NET Core");
                    return;
                case "FEAT":
                    await ReplyMultilineAsync(FtpReplyCode.SystemStatus, "Supports:\nUTF8");
                    return;
                case "OPTS":
                    if (parameter.ToUpperInvariant() == "UTF8 ON")
                    {
                        encoding = Encoding.UTF8;
                        await ReplyAsync(FtpReplyCode.CommandOkay, "UTF-8 is on");
                        return;
                    }
                    else if (parameter.ToUpperInvariant() == "UTF8 OFF")
                    {
                        encoding = Encoding.ASCII;
                        await ReplyAsync(FtpReplyCode.CommandOkay, "UTF-8 is off");
                        return;
                    }
                    break;
                case "USER":
                    userName = parameter;
                    authenticated = false;
                    fileProvider = null;
                    await ReplyAsync(FtpReplyCode.NeedPassword, "Please input password");
                    return;
                case "PASS":
                    if (authenticated = server.Authenticator.Authenticate(userName, parameter))
                    {
                        await ReplyAsync(FtpReplyCode.UserLoggedIn, "Logged in");
                        fileProvider = server.FileManager.GetProvider(userName);
                    }
                    else
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "Failed to log in");
                        fileProvider = null;
                    }
                    return;
                case "PORT":
                    await CommandPortAsync(parameter);
                    return;
                case "EPRT":
                    await CommandEprtAsync(parameter);
                    return;
                case "PASV":
                    await CommandPasvAsync();
                    return;
                case "EPSV":
                    await CommandEpsvAsync(parameter);
                    return;
                case "TYPE":
                    switch (parameter)
                    {
                        case "A":
                            dataType = DataType.ASCII;
                            await ReplyAsync(FtpReplyCode.CommandOkay, "In ASCII type");
                            return;
                        case "I":
                            dataType = DataType.IMAGE;
                            await ReplyAsync(FtpReplyCode.CommandOkay, "In IMAGE type");
                            return;
                        default:
                            await ReplyAsync(FtpReplyCode.ParameterNotImplemented, "Unknown type");
                            return;
                    }
                case "MODE":
                    if (parameter == "S")
                    {
                        transmissionMode = TransmissionMode.Stream;
                        await ReplyAsync(FtpReplyCode.CommandOkay, "In stream mode");
                    }
                    else
                    {
                        await ReplyAsync(FtpReplyCode.ParameterNotImplemented, "Unknown mode");
                    }

                    return;
                case "QUIT":
                    if (server.ControlConnectionSslFactory != null)
                    {
                        await server.ControlConnectionSslFactory.DisconnectAsync(stream);
                    }
                    throw new QuitRequestedException();
                case "RETR":
                    await CommandRetrAsync(parameter);
                    return;
                case "STOR":
                    await CommandStorAsync(parameter);
                    return;
                case "CWD":
                    if (!authenticated)
                    {
                        await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                        return;
                    }
                    if (fileProvider.SetWorkingDirectory(parameter))
                    {
                        await ReplyAsync(FtpReplyCode.FileActionOk, fileProvider.GetWorkingDirectory());
                    }
                    else
                    {
                        await ReplyAsync(FtpReplyCode.FileNoAccess, "Path doesn't exist");
                    }
                    return;
                case "NLST":
                    await CommandNlstAsync(parameter);
                    return;
                case "LIST":
                    await CommandListAsync(parameter);
                    return;
                case "NOOP":
                    await ReplyAsync(FtpReplyCode.CommandOkay, "OK");
                    return;
                case "AUTH":
                    await CommandAuthAsync(parameter);
                    return;
                case "PROT":
                    await CommandProtAsync(parameter);
                    return;
            }
            await ReplyAsync(FtpReplyCode.CommandUnrecognized, "Can't recognize this command.");
        }

        private async Task CommandAuthAsync(string parameter)
        {
            if ((parameter != "TLS" && parameter != "SSL") || server.ControlConnectionSslFactory == null)
            {
                await ReplyAsync(FtpReplyCode.NotImplemented, "Not supported");
                return;
            }
            await ReplyAsync(FtpReplyCode.ProceedWithNegotiation, "Authenticating");
            stream = await server.ControlConnectionSslFactory.UpgradeAsync(stream);
        }

        private async Task CommandProtAsync(string parameter)
        {
            switch (parameter)
            {
                case "C":
                    useSecureDataConnection = false;
                    break;
                case "S":
                case "E":
                case "P":
                    if (!(dataConnection is ISslDataConnection))
                    {
                        await ReplyAsync(FtpReplyCode.ParameterNotImplemented, "Parameter not implemented");
                        return;
                    }
                    useSecureDataConnection = true;
                    break;
                default:
                    await ReplyAsync(FtpReplyCode.ParameterNotImplemented, "Parameter not implemented");
                    return;
            }
            await ReplyAsync(FtpReplyCode.CommandOkay, "Secure level set");
        }

        private async Task CommandRnfrAsync(string parameter)
        {
            var fromPath = parameter;
            await ReplyAsync(FtpReplyCode.FileActionPendingInfo, "Waiting for RNTO");
            var nextCommand = await ReadLineAsync();
            if (!nextCommand.ToUpper().StartsWith("RNTO "))
            {
                await ReplyAsync(FtpReplyCode.BadSequence, "Wrong sequence, renaming aborted");
                return;
            }
            var toPath = nextCommand.Substring(5);
            await fileProvider.RenameAsync(fromPath, toPath);
            await ReplyAsync(FtpReplyCode.FileActionOk, "Rename succeeded");
        }

        private async Task CommandListAsync(string parameter)
        {
            if (!authenticated)
            {
                await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                return;
            }
            MemoryStream stream = new MemoryStream();
            var writer = new StreamWriter(stream, encoding);
            writer.NewLine = "\r\n";
            try
            {
                var listing = await fileProvider.GetListingAsync(parameter);
                await writer.WriteLineAsync();
                foreach (var item in listing)
                {
                    if (listFormat == ListFormat.Unix)
                    {
                        await writer.WriteLineAsync(
                            string.Format(
                                "{0}{1}{1}{1}   1 owner   group {2,15} {3} {4}",
                                item.IsDirectory ? 'd' : '-',
                                item.IsReadOnly ? "r-x" : "rwx",
                                item.Length,
                                item.LastWriteTime.ToString(
                                    item.LastWriteTime.Year == DateTime.Now.Year ?
                                    "MMM dd HH:mm" : "MMM dd  yyyy", CultureInfo.InvariantCulture),
                                item.Name));
                    }
                    else if (listFormat == ListFormat.MsDos)
                    {
                        if (item.IsDirectory)
                        {
                            await writer.WriteLineAsync(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0:MM-dd-yy  hh:mmtt} {1,20} {2}",
                                    item.LastWriteTime,
                                    item.Length,
                                    item.Name));
                        }
                        else
                        {
                            await writer.WriteLineAsync(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0:MM-dd-yy  hh:mmtt}       {1,-14} {2}",
                                    item.LastWriteTime,
                                    "<DIR>",
                                    item.Name));
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Can't only use Unix or MS-DOS listing format.");
                    }
                }
            }
            catch (FileBusyException ex)
            {
                await ReplyAsync(FtpReplyCode.FileBusy, string.Format("File temporarily unavailable: {0}", ex.Message));
                return;
            }
            catch (FileNoAccessException ex)
            {
                await ReplyAsync(FtpReplyCode.FileNoAccess, string.Format("File access denied: {0}", ex.Message));
                return;
            }
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            await OpenDataConnectionAsync();
            await dataConnection.SendAsync(stream);
            await dataConnection.DisconnectAsync();
            await ReplyAsync(FtpReplyCode.SuccessClosingDataConnection, "Listing has been sent");
            return;
        }

        private async Task CommandNlstAsync(string parameter)
        {
            if (!authenticated)
            {
                await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                return;
            }
            MemoryStream stream = new MemoryStream();
            var writer = new StreamWriter(stream, encoding);
            writer.NewLine = "\r\n";
            try
            {
                var nameListing = await fileProvider.GetNameListingAsync(parameter);
                foreach (var item in nameListing)
                {
                    await writer.WriteLineAsync(item);
                }
            }
            catch (FileBusyException ex)
            {
                await ReplyAsync(FtpReplyCode.FileBusy, string.Format("File temporarily unavailable: {0}", ex.Message));
                return;
            }
            catch (FileNoAccessException ex)
            {
                await ReplyAsync(FtpReplyCode.FileNoAccess, string.Format("File access denied: {0}", ex.Message));
                return;
            }
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            await OpenDataConnectionAsync();
            await dataConnection.SendAsync(stream);
            await dataConnection.DisconnectAsync();
            await ReplyAsync(FtpReplyCode.SuccessClosingDataConnection, "Listing has been sent");
            return;
        }

        private async Task CommandStorAsync(string parameter)
        {
            if (!authenticated)
            {
                await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                return;
            }
            if (string.IsNullOrEmpty(parameter))
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, path is missing");
                return;
            }
            if (transmissionMode != TransmissionMode.Stream)
            {
                await ReplyAsync(FtpReplyCode.NotImplemented, "Only supports stream mode");
                return;
            }

            try
            {
                using (Stream fileStream = await fileProvider.CreateFileForWriteAsync(parameter))
                {
                    await OpenDataConnectionAsync();
                    await dataConnection.RecieveAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
            catch (FileBusyException ex)
            {
                await ReplyAsync(FtpReplyCode.FileBusy, string.Format("Temporarily unavailable: {0}", ex.Message));
                return;
            }
            catch (FileSpaceInsufficientException ex)
            {
                await ReplyAsync(FtpReplyCode.FileSpaceInsufficient, string.Format("Writing file denied: {0}", ex.Message));
                return;
            }

            await dataConnection.DisconnectAsync();
            await ReplyAsync(FtpReplyCode.SuccessClosingDataConnection, "File has been recieved");
            return;
        }

        private async Task CommandRetrAsync(string parameter)
        {
            if (!authenticated)
            {
                await ReplyAsync(FtpReplyCode.NotLoggedIn, "You need to log in first");
                return;
            }
            if (string.IsNullOrEmpty(parameter))
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, path is missing");
                return;
            }
            if (transmissionMode != TransmissionMode.Stream)
            {
                await ReplyAsync(FtpReplyCode.NotImplemented, "Only supports stream mode");
                return;
            }

            try
            {
                using (Stream fileStream = await fileProvider.OpenFileForReadAsync(parameter))
                {
                    await OpenDataConnectionAsync();
                    await dataConnection.SendAsync(fileStream);
                }
            }
            catch (FileBusyException ex)
            {
                await ReplyAsync(FtpReplyCode.FileBusy, string.Format("File temporarily unavailable: {0}", ex.Message));
                return;
            }
            catch (FileNoAccessException ex)
            {
                await ReplyAsync(FtpReplyCode.FileNoAccess, string.Format("File access denied: {0}", ex.Message));
                return;
            }
            await dataConnection.DisconnectAsync();
            await ReplyAsync(FtpReplyCode.SuccessClosingDataConnection, "File has been sent");
            return;
        }

        private async Task CommandPasvAsync()
        {
            var localEP = dataConnection.Listen();
            var ipBytes = localEP.Address.GetAddressBytes();
            if (ipBytes.Length != 4) throw new Exception();
            var passiveEPString =
                string.Format(
                    "{0},{1},{2},{3},{4},{5}",
                    ipBytes[0],
                    ipBytes[1],
                    ipBytes[2],
                    ipBytes[3],
                    (byte)(localEP.Port / 256),
                    (byte)(localEP.Port % 256));
            dataConnectionMode = DataConnectionMode.Passive;
            await ReplyAsync(FtpReplyCode.EnteringPassiveMode, "Enter Passive Mode (" + passiveEPString + ")");
        }

        private async Task CommandPortAsync(string parameter)
        {
            var paramSegs = parameter.Split(',');
            if (paramSegs.Length != 6)
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, count of comma incorrect");
                return;
            }
            try
            {
                var bytes = paramSegs.Select(x => byte.Parse(x)).ToArray();
                IPAddress remoteIP = new IPAddress(new ArraySegment<byte>(bytes, 0, 4).ToArray());
                int remotePort = (bytes[4] << 8) | bytes[5];
                userActiveDataPort = remotePort;
                userActiveIP = remoteIP;
                userActiveProtocal = 1;
                dataConnectionMode = DataConnectionMode.Active;
                await dataConnection.ConnectActiveAsync(userActiveIP, userActiveDataPort, userActiveProtocal);
                await ReplyAsync(FtpReplyCode.CommandOkay, "Data connection established");
                return;
            }
            catch
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, number format incorrect");
                return;
            }
        }

        private async Task CommandEprtAsync(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, parameter is empty");
                return;
            }

            var seperator = parameter[0];
            var paramSegs = parameter.Split(seperator);

            if (paramSegs.Length != 5)
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Syntax error, count of segments incorrect");
                return;
            }

            int remoteProtocal;
            if (!int.TryParse(paramSegs[1], out remoteProtocal))
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "Protocal ID incorrect");
                return;
            }

            IPAddress remoteIP;
            int remotePort;
            try
            {
                remoteIP = IPAddress.Parse(paramSegs[2]);
                remotePort = int.Parse(paramSegs[3]);
            }
            catch (Exception)
            {
                await ReplyAsync(
                    FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    "IP address or port number incorrect.");
                return;
            }
            userActiveDataPort = remotePort;
            userActiveIP = remoteIP;
            userActiveProtocal = remoteProtocal;

            dataConnectionMode = DataConnectionMode.ExtendedPassive;
            try
            {
                await dataConnection.ConnectActiveAsync(userActiveIP, userActiveDataPort, userActiveProtocal);
            }
            catch (NotSupportedException)
            {
                var supportedProtocalString =
                    string.Join(",", dataConnection.SupportedActiveProtocal.Select(x => x.ToString()));
                await ReplyAsync(FtpReplyCode.NotSupportedProtocal, $"Protocal not supported, use({supportedProtocalString})");
                return;
            }

            await ReplyAsync(FtpReplyCode.CommandOkay, "Data connection established");
        }

        private async Task CommandEpsvAsync(string parameter)
        {
            int port;
            try
            {
                if (string.IsNullOrEmpty(parameter))
                {
                    port = dataConnection.Listen().Port;
                }
                else
                {
                    var protocal = int.Parse(parameter);
                    port = dataConnection.ExtendedListen(protocal);
                }
            }
            catch (FormatException)
            {
                await ReplyAsync(FtpReplyCode.SyntaxErrorInParametersOrArguments, "Protocal ID incorrect.");
                return;
            }
            catch (NotSupportedException)
            {
                var supportedProtocalString =
                    string.Join(",", dataConnection.SupportedPassiveProtocal.Select(x => x.ToString()));
                await ReplyAsync(FtpReplyCode.NotSupportedProtocal, $"Protocal not supported, use({supportedProtocalString})");
                return;
            }
            dataConnectionMode = DataConnectionMode.ExtendedPassive;
            await ReplyAsync(
                FtpReplyCode.EnteringEpsvMode,
                string.Format("Entering extended passive mode (|||{0}|).", port));
        }

        private async Task OpenDataConnectionAsync()
        {
            if (dataConnection != null && dataConnection.IsOpen)
            {
                await ReplyAsync(FtpReplyCode.TransferStarting, "Transfer is starting");
            }
            else
            {
                await ReplyAsync(FtpReplyCode.AboutToOpenDataConnection, "File is Ok, about to open connection.");
                switch (dataConnectionMode)
                {
                    case DataConnectionMode.Active:
                    case DataConnectionMode.ExtendedActive:
                        await dataConnection.ConnectActiveAsync(userActiveIP, userActiveDataPort, userActiveProtocal);
                        break;
                    case DataConnectionMode.Passive:
                    case DataConnectionMode.ExtendedPassive:
                        await dataConnection.AcceptAsync();
                        break;
                }
            }
            if (useSecureDataConnection)
            {
                await (dataConnection as ISslDataConnection).UpgradeToSslAsync();
            }
        }

        /// <summary>
        /// Reads a line from network stream partitioned by CRLF.
        /// </summary>
        /// <returns>The line read with CRLF trimmed.</returns>
        private async Task<string> ReadLineAsync()
        {
            var decoder = encoding.GetDecoder();
            StringBuilder messageBuilder = new StringBuilder();

            bool lastByteIsCr = false;
            while (true)
            {
                var byteCount = await stream.ReadAsync(readByteBuffer, readOffset, readByteBuffer.Length - readOffset);
                if (byteCount == 0)
                {
                    throw new EndOfStreamException();
                }
                for (int i = readOffset; i < readOffset + byteCount; i++)
                {
                    // If meets CRLF, stop
                    if (lastByteIsCr && readByteBuffer[i] == '\n')
                    {
                        var byteCountToRead = i + 1 - readOffset;
                        while (byteCountToRead > 0)
                        {
                            decoder.Convert(
                                readByteBuffer,
                                readOffset,
                                byteCountToRead,
                                readCharBuffer,
                                0,
                                readCharBuffer.Length,
                                true,
                                out int bytesUsed,
                                out int charsUsed,
                                out bool completed);
                            messageBuilder.Append(readCharBuffer, 0, charsUsed);
                            byteCountToRead -= bytesUsed;
                        }

                        messageBuilder.Remove(messageBuilder.Length - 2, 2);
                        return messageBuilder.ToString();
                    }
                    else
                    {
                        lastByteIsCr = readByteBuffer[i] == '\r';
                    }
                }

                while (byteCount > 0)
                {
                    decoder.Convert(
                        readByteBuffer,
                        readOffset,
                        byteCount,
                        readCharBuffer,
                        0,
                        readCharBuffer.Length,
                        false,
                        out int bytesUsed,
                        out int charsUsed,
                        out bool completed);
                    byteCount -= bytesUsed;
                    messageBuilder.Append(readCharBuffer, 0, charsUsed);
                }

                readOffset = 0;
            }
        }

        private async Task ReplyAsync(FtpReplyCode code, string message)
        {
            server.Tracer.TraceReply(((int)code).ToString(), remoteEndPoint);
            StringBuilder stringBuilder = new StringBuilder(6 + message.Length);
            stringBuilder.Append((int)code);
            stringBuilder.Append(' ');
            stringBuilder.Append(message);
            stringBuilder.Append("\r\n");
            System.Diagnostics.Debug.WriteLine(stringBuilder.ToString());
            var bytesToSend = encoding.GetBytes(stringBuilder.ToString());
            await stream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
        }

        private async Task ReplyMultilineAsync(FtpReplyCode code, string message)
        {
            server.Tracer.TraceReply(((int)code).ToString(), remoteEndPoint);
            message = message.Replace("\r", string.Empty);
            message = message.Replace("\n", "\r\n ");
            var stringToSend = string.Format("{0}-{1}\r\n{2} End\r\n", (int)code, message, (int)code);
            var bytesToSend = encoding.GetBytes(stringToSend);
            await stream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
        }

        private string EncodePathName(string path)
        {
            return path.Replace("\r", "\r\0");
        }

        private string DecodePathName(string path)
        {
            return path.Replace("\r\0", "\r\0");
        }
    }
}
