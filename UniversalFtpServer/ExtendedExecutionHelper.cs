using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Notifications;

namespace UniversalFtpServer
{
    static class ExtendedExecutionHelper
    {
        const string SuspendedNotificationGroup = "SuspendedGroup";
        static ExtendedExecutionSession extendedExeSession = null;
        static int count;
        static readonly object countSyncRoot = new object();
        static readonly object sessionSyncRoot = new object();

        public static Deferral GetDeferral()
        {
            bool requestSession = false;
            lock (countSyncRoot)
            {
                ++count;
                requestSession = count == 1;
            }
            if (requestSession)
                RequestExtendedExecutionAsync();
            return new Deferral(TaskFinished);
        }

        private static void TaskFinished()
        {
            bool clearSession = false;
            lock (countSyncRoot)
            {
                --count;
                clearSession = count == 0;
            }
            if (clearSession)
                ClearExtendedExeSession();
        }

        private static Task RequestExtendedExecutionAsync()
        {
            return Task.Run(() =>
            {
                lock (sessionSyncRoot)
                {
                    if (extendedExeSession != null)
                    {
                        extendedExeSession.Dispose();
                        extendedExeSession = null;
                    }

                    var newSession = new ExtendedExecutionSession();
                    newSession.Reason = ExtendedExecutionReason.Unspecified;
                    newSession.Revoked += ExtendedExecutionRevoked;

                    var asyncTask = newSession.RequestExtensionAsync().AsTask();
                    asyncTask.Wait();
                    ExtendedExecutionResult result = asyncTask.Result;

                    switch (result)
                    {
                        case ExtendedExecutionResult.Allowed:
                            extendedExeSession = newSession;
                            break;
                        default:
                        case ExtendedExecutionResult.Denied:
                            newSession.Dispose();
                            break;
                    }
                }
            });
        }

        private static void ClearExtendedExeSession()
        {
            lock (sessionSyncRoot)
            {
                if (extendedExeSession != null)
                {
                    extendedExeSession.Dispose();
                    extendedExeSession = null;
                }
            }
        }

        private static void ExtendedExecutionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            lock (sessionSyncRoot)
            {
                if (extendedExeSession != null)
                {
                    extendedExeSession.Dispose();
                    extendedExeSession = null;
                }
            }
        }

        public static async Task OnSuspendingAsync()
        {
            if (count > 0)
            {
                var loader = new ResourceLoader();
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(loader.GetString("ExtendedExecution_HelpFileUri")));
                ToastContent toastContent = new ToastContent()
                {
                    Visual = new ToastVisual
                    {
                        BindingGeneric = new ToastBindingGeneric
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = loader.GetString("ExtendedExecution_Stopped")
                                }
                            }
                        }
                    },
                    Actions = new ToastActionsCustom
                    {
                        Buttons =
                        {
                            new ToastButton(loader.GetString("ExtendedExecution_LearnMore"), "file:\\\\\\" + file.Path)
                            {
                                ActivationType = ToastActivationType.Protocol
                            }
                        }
                    }
                };
                var notification = new ToastNotification(toastContent.GetXml())
                {
                    Group = SuspendedNotificationGroup
                };
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }

        public static void OnResuming()
        {
            ToastNotificationManager.History.RemoveGroup(SuspendedNotificationGroup);
        }
    }
}
