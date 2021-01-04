// <copyright file="IFileProviderFactory.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Zhaobang.FtpServer.File
{
     /// <summary>
     /// Manager that creates <see cref="IFileProvider"/> for each user.
     /// </summary>
    public interface IFileProviderFactory
    {
        /// <summary>
        /// Creates <see cref="IFileProvider"/> for specified user.
        /// </summary>
        /// <param name="user">The name of the user.</param>
        /// <returns>The <see cref="IFileProvider"/>.</returns>
        IFileProvider GetProvider(string user);
    }
}
