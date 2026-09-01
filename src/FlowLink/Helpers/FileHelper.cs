using FlowLink.Data.Models;

namespace FlowLink.Helpers;

public static class FileHelper
{
    public static async Task<FileMetadata> ToFileMetadata(this StorageFile file)
    {
        return new FileMetadata(file.Name, file.ContentType, (long)(await file.GetBasicPropertiesAsync()).Size);
    }
}