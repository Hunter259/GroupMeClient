﻿using System.Linq;
using System.Threading.Tasks;
using GroupMeClientApi.Models;
using GroupMeClientApi.Models.Attachments;
using GroupMeClientApi.Push;
using GroupMeClientApi.Push.Notifications;

namespace GroupMeClient.Notifications.Display
{
    /// <summary>
    /// <see cref="PopupNotificationProvider"/> provides an observer to display notifications from the <see cref="NotificationRouter"/> visually.
    /// </summary>
    public class PopupNotificationProvider : INotificationSink
    {
        private PopupNotificationProvider(IPopupNotificationSink sink)
        {
            this.PopupNotificationSink = sink;
        }

        private IPopupNotificationSink PopupNotificationSink { get; }

        private GroupMeClientApi.GroupMeClient GroupMeClient { get; set; }

        /// <summary>
        /// Creates a <see cref="PopupNotificationProvider"/> to display operating system level notifications.
        /// </summary>
        /// <param name="settingsManager">The settings instance to use.</param>
        /// <returns>A PopupNotificationProvider.</returns>
        public static PopupNotificationProvider CreatePlatformNotificationProvider(Settings.SettingsManager settingsManager)
        {
            var osVersion = System.Environment.OSVersion.Version;

            if (osVersion.Major == 10)
            {
                // UWP-type Toast Notifications are natively supported (modern Windows 10 builds)
                return new PopupNotificationProvider(new Win10.Win10ToastNotificationsProvider(settingsManager));
            }
            else
            {
                // No system-level notification support (pre-Win 10)
                return new PopupNotificationProvider(new Win7.Win7ToastNotificationsProvider(settingsManager));
            }
        }

        /// <summary>
        /// Creates a <see cref="PopupNotificationProvider"/> to display internal (popup) toast notifications.
        /// </summary>
        /// <param name="manager">The manager to use for displaying notifications.</param>
        /// <returns>A PopupNotificationProvider.</returns>
        public static PopupNotificationProvider CreateInternalNotificationProvider(WpfToast.ToastHolderViewModel manager)
        {
            return new PopupNotificationProvider(new WpfToast.WpfToastNotificationProvider(manager));
        }

        /// <inheritdoc/>
        async Task INotificationSink.ChatUpdated(DirectMessageCreateNotification notification, IMessageContainer container)
        {
            if (!string.IsNullOrEmpty(notification.Alert) &&
                !this.DidISendIt(notification.Message) &&
                !this.IsGroupMuted(container))
            {
                var image = notification.Message.Attachments.FirstOrDefault(a => a is ImageAttachment);

                if (image != null)
                {
                    await this.PopupNotificationSink.ShowLikableImageMessage(
                        container.Name,
                        this.RemoveUnprintableCharacters(notification.Alert),
                        notification.Message.AvatarUrl,
                        (notification.Message as IAvatarSource).IsRoundedAvatar,
                        (image as ImageAttachment).Url,
                        container.Id,
                        notification.Message.Id);
                }
                else
                {
                    await this.PopupNotificationSink.ShowLikableMessage(
                       container.Name,
                       this.RemoveUnprintableCharacters(notification.Alert),
                       notification.Message.AvatarUrl,
                       (notification.Message as IAvatarSource).IsRoundedAvatar,
                       container.Id,
                       notification.Message.Id);
                }
            }
        }

        /// <inheritdoc/>
        async Task INotificationSink.GroupUpdated(LineMessageCreateNotification notification, IMessageContainer container)
        {
            if (!string.IsNullOrEmpty(notification.Alert) &&
                !this.DidISendIt(notification.Message) &&
                !this.IsGroupMuted(container))
            {
                var image = notification.Message.Attachments.FirstOrDefault(a => a is ImageAttachment);

                if (image != null)
                {
                    await this.PopupNotificationSink.ShowLikableImageMessage(
                        container.Name,
                        this.RemoveUnprintableCharacters(notification.Alert),
                        container.ImageOrAvatarUrl,
                        container.IsRoundedAvatar,
                        (image as ImageAttachment).Url,
                        container.Id,
                        notification.Message.Id);
                }
                else
                {
                    await this.PopupNotificationSink.ShowLikableMessage(
                        container.Name,
                        this.RemoveUnprintableCharacters(notification.Alert),
                        container.ImageOrAvatarUrl,
                        container.IsRoundedAvatar,
                        container.Id,
                        notification.Message.Id);
                }
            }
        }

        /// <inheritdoc/>
        async Task INotificationSink.MessageUpdated(Message message, string alert, IMessageContainer container)
        {
            if (!string.IsNullOrEmpty(alert) &&
                !this.IsGroupMuted(container))
            {
                await this.PopupNotificationSink.ShowNotification(
                    container.Name,
                    this.RemoveUnprintableCharacters(alert),
                    container.ImageOrAvatarUrl,
                    container.IsRoundedAvatar,
                    container.Id);
            }
        }

        /// <inheritdoc/>
        void INotificationSink.HeartbeatReceived()
        {
        }

        /// <inheritdoc/>
        void INotificationSink.RegisterPushSubscriptions(PushClient pushClient, GroupMeClientApi.GroupMeClient client)
        {
            this.GroupMeClient = client;
            this.PopupNotificationSink.RegisterClient(client);
        }

        private bool DidISendIt(Message message)
        {
            var me = this.GroupMeClient.WhoAmI();
            return message.UserId == me.Id;
        }

        private string RemoveUnprintableCharacters(string message)
        {
            return message.Replace("\uE008 ", string.Empty);
        }

        private bool IsGroupMuted(IMessageContainer messageContainer)
        {
            if (messageContainer is Group group)
            {
                var me = this.GroupMeClient.WhoAmI();
                foreach (var member in group.Members)
                {
                    if (member.UserId == me.UserId)
                    {
                        return member.Muted;
                    }
                }
            }

            return false;
        }
    }
}
