﻿using DesktopNotifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace GroupMeClient.Notifications.Display.Win10
{
    class Win10ToastNotificationsProvider : IPopupNotificationSink
    {
        public Win10ToastNotificationsProvider()
        {
            // Register AUMID and COM server (for Desktop Bridge apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>(ApplicationId);
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
        }

        private string ApplicationId => "MicroCube.GroupMeDesktopClient";

        private bool HasPerformedCleanup { get; set; } = false;

        private string ToastImagePath => Path.GetTempPath() + "WindowsNotifications.GroupMeToasts.Images";

        private GroupMeClientApi.GroupMeClient GroupMeClient { get; set; }

        async Task IPopupNotificationSink.ShowNotification(string title, string body, string avatarUrl, bool roundedAvatar)
        {
            ToastContent toastContent = new ToastContent()
            {
                Launch = "action=viewConversation&conversationId=5",

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },
                            new AdaptiveText()
                            {
                                Text = body
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = await DownloadImageToDiskCached(
                                image: avatarUrl,
                                isAvatar: true,
                                isRounded: roundedAvatar),
                            HintCrop = roundedAvatar ? ToastGenericAppLogoCrop.Circle : ToastGenericAppLogoCrop.Default
                        }
                    }
                }
            };

            this.ShowToast(toastContent);
        }

        async Task IPopupNotificationSink.ShowLikableImageMessage(string title, string body, string avatarUrl, bool roundedAvatar, string imageUrl)
        {
            ToastContent toastContent = new ToastContent()
            {
                Launch = "action=viewConversation&conversationId=5",

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },
                            new AdaptiveText()
                            {
                                Text = body
                            },
                            new AdaptiveImage()
                            {
                                 Source = await DownloadImageToDiskCached(imageUrl)
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = await DownloadImageToDiskCached(
                                image: avatarUrl,
                                isAvatar: true,
                                isRounded: roundedAvatar),
                            HintCrop = roundedAvatar ? ToastGenericAppLogoCrop.Circle : ToastGenericAppLogoCrop.Default
                        },
                        //HeroImage = new ToastGenericHeroImage()
                        //{
                           
                        //}
                    }
                }
            };

            this.ShowToast(toastContent);
        }

        async Task IPopupNotificationSink.ShowLikableMessage(string title, string body, string avatarUrl, bool roundedAvatar)
        {
            ToastContent toastContent = new ToastContent()
            {
                Launch = "action=viewConversation&conversationId=5",

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },
                            new AdaptiveText()
                            {
                                Text = body
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = await DownloadImageToDiskCached(
                                image: avatarUrl,
                                isAvatar: true,
                                isRounded: roundedAvatar),
                            HintCrop = roundedAvatar ? ToastGenericAppLogoCrop.Circle : ToastGenericAppLogoCrop.Default
                        }
                    }
                }
            };

            this.ShowToast(toastContent);
        }

        void IPopupNotificationSink.RegisterClient(GroupMeClientApi.GroupMeClient client)
        {
            this.GroupMeClient = client;
        }

        private void ShowToast(ToastContent toastContent)
        {
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc);

            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        private async Task<string> DownloadImageToDiskCached(string image, bool isAvatar = false, bool isRounded = false)
        {
            // Toasts can live for up to 3 days, so we cache images for up to 3 days.
            // Note that this is a very simple cache that doesn't account for space usage, so
            // this could easily consume a lot of space within the span of 3 days.

            if (image == null)
            {
                image = string.Empty;
            }

            try
            {
                var directory = Directory.CreateDirectory(this.ToastImagePath);

                if (!this.HasPerformedCleanup)
                {
                    // First time we run, we'll perform cleanup of old images
                    this.HasPerformedCleanup = true;

                    foreach (var d in directory.EnumerateDirectories())
                    {
                        if (d.LastAccessTime.Date < DateTime.UtcNow.Date.AddDays(-3))
                        {
                            d.Delete(true);
                        }
                    }
                }

                string hashName = Win10ToastNotificationsProvider.Hash(image) + ".png";
                string imagePath = Path.Combine(directory.FullName, hashName);

                if (File.Exists(imagePath))
                {
                    return imagePath;
                }

                byte[] imageData;

                if (isAvatar)
                {
                    imageData = await this.GroupMeClient.ImageDownloader.DownloadAvatarImage(image, !isRounded);
                }
                else
                {
                    imageData = await this.GroupMeClient.ImageDownloader.DownloadPostImage(image);
                }

                using (var fileStream = File.OpenWrite(imagePath))
                {
                    await fileStream.WriteAsync(imageData, 0, imageData.Length);
                }

                return imagePath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        static string Hash(string input)
        {
            var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
