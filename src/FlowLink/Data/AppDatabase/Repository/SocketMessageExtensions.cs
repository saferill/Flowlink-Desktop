using FlowLink.Data.AppDatabase.Models;
using FlowLink.Data.Models;

namespace FlowLink.Data.AppDatabase.Repository;

public static class SocketMessageExtensions
{
    public static MessageEntity ToEntity(this TextMessage message, string deviceId)
        => MessageEntity.FromMessage(message, deviceId);

    public static AttachmentEntity ToEntity(this SmsAttachment attachment, long messageUniqueId)
        => AttachmentEntity.FromAttachment(attachment, messageUniqueId);

    public static ConversationEntity ToEntity(this ConversationInfo thread, string deviceId)
        => ConversationEntity.FromMessage(thread, deviceId);

    public static ContactEntity ToEntity(this ContactInfo message, string deviceId)
        => ContactEntity.FromMessage(message, deviceId);

    public static NotificationEntity ToEntity(this NotificationInfo message, string deviceId)
        => NotificationEntity.FromMessage(message, deviceId);
}
