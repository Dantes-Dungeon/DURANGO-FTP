// <copyright file="IAuthenticator.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.Authenticate
{
    /// <summary>
    /// Used to authenticate FTP login.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Verifies if the username-password pair is correct.
        /// </summary>
        /// <param name="userName">The user name user inputted.</param>
        /// <param name="password">The password user inputted.</param>
        /// <returns>Whether the pair is correct.</returns>
        bool Authenticate(string userName, string password);
    }
}
