// <copyright file="Helpers.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer
{
    /// <summary>
    /// Static class that stores some helpers.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Gets the task to wait for the original task or throws an exception when trying to cancel.
        /// The original task will not and can not be terminated.
        /// </summary>
        /// <typeparam name="T">The typeparam of the source task.</typeparam>
        /// <param name="task">The task to wait.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the action.</param>
        /// <returns>The task to wait until the original task finishes.</returns>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                    {
                        if (task != await Task.WhenAny(task, tcs.Task))
                        {
                            throw new OperationCanceledException(cancellationToken);
                        }
                    }

            return await task;
        }
    }
}
