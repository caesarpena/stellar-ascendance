using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using System;

namespace webapi.Services
{
    public interface IBlobService
    {
        Task<Uri> UploadFileBlobAsync(string blobContainerName, Stream content, string contentType, string fileName);

        Task<bool> DeleteBlobIfExistsAsync(string blobContainerName, string blobName);
    }
}