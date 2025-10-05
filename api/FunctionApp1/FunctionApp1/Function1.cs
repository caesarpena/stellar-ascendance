// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.IO;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;

namespace FunctionApp1
{
    public class EventGridEventData
    {
        public string url { get; set; }
        public string api { get; set; }

        // Add other properties from your event data as needed
    }
    public class BlobUrlComponents
    {
        public string AzureUrl { get; set; }
        public string Container { get; set; }
        public string InputName { get; set; }
    }

    public class Function1
    {
        [FunctionName("Function1")]
        public async Task RunAsync([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());

            try
            {
                // Parse the Event Grid event data into your custom class
                var eventData = JsonConvert.DeserializeObject<EventGridEventData>(eventGridEvent.Data.ToString());

                if (eventData?.url != null)
                {
                    string videoBlobUrl = eventData.url;

                    BlobUrlComponents components = DeserializeBlobUrl(videoBlobUrl);

                    if (components != null)
                    {
                        string azureUrl = components.AzureUrl; // https://azurefitstorage.blob.core.windows.net
                        string containerName = components.Container; // input-8feeed44-228c-4498-af80-5e9222470462

                        // Replace invalid characters in directory and name with underscores
                        string inputName = CleanPathSegment(components.InputName); // mixkit-man-exercising-with-a-kettlebell-4506-mediumttt.mp4

                        // Generate thumbnail
                        // Define a fixed destination directory for your thumbnails
                        string thumbnailDirectory = "thumbnails"; // Change this to your desired directory name
                        string outputFileName = $"{Path.GetFileNameWithoutExtension(inputName)}_thumbnail.png";
                        string outputFilePath = Path.Combine(thumbnailDirectory, outputFileName).Replace('\\', '/');
                        var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                        if (eventData.api == "DeleteBlob")
                        {

                        }
                        else if (eventData.api == "PutBlob")
                        {
                            if (IsVideoFile(inputName))
                            {
                                string ffmpegPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "site", "wwwroot", "ffmpeg\\ffmpeg.exe");

                                if (await blobContainerClient.ExistsAsync())
                                {
                                    var blobClient = blobContainerClient.GetBlobClient(inputName); // Assuming inputName is the blob name
                                    BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

                                    if (blobDownloadInfo.ContentLength > 0)
                                    {
                                        // Create a temporary local directory if it doesn't exist
                                        string tempVideoDirectory = Path.Combine(Path.GetTempPath(), containerName);
                                        Directory.CreateDirectory(tempVideoDirectory);
                                        // Create a temporary local file to store the video content
                                        string tempVideoFilePath = Path.Combine(tempVideoDirectory, inputName);

                                        using (var tempVideoStream = File.OpenWrite(tempVideoFilePath))
                                        {
                                            await blobDownloadInfo.Content.CopyToAsync(tempVideoStream);
                                        }

                                        ProcessStartInfo psi = new ProcessStartInfo(ffmpegPath);
                                        // Create a temporary local directory if it doesn't exist
                                        string tempThumbnailDirectory = Path.Combine(Path.GetTempPath(), $"{containerName}/thumbnails");
                                        Directory.CreateDirectory(tempThumbnailDirectory);
                                        // Create a temporary local file to store the thumbnail image.
                                        string tempThumbnailFilePath = Path.Combine(tempThumbnailDirectory, outputFileName);

                                        psi.Arguments = $"-i {tempVideoFilePath} -ss 00:00:02 -vframes 1 -s 120x80 {tempThumbnailFilePath}";

                                        psi.RedirectStandardOutput = true;
                                        psi.RedirectStandardError = true;
                                        psi.UseShellExecute = false;
                                        psi.CreateNoWindow = true;

                                        using (Process process = new Process())
                                        {
                                            process.StartInfo = psi;
                                            process.Start();
                                            string ffmpegErrorOutput = await process.StandardError.ReadToEndAsync();
                                            log.LogInformation($"FFmpeg Error Output: {ffmpegErrorOutput}");

                                            // Read the generated thumbnail image from the standard output
                                            using (var thumbnailStream = new MemoryStream())
                                            {
                                                await process.StandardOutput.BaseStream.CopyToAsync(thumbnailStream);
                                                thumbnailStream.Seek(0, SeekOrigin.Begin);
                                                // Check if the thumbnailStream contains data
                                                if (tempThumbnailFilePath.Length > 0)
                                                {
                                                    // Upload the thumbnail to Azure Blob Storage
                                                    BlobClient blobClientThumbnail = blobContainerClient.GetBlobClient(outputFilePath);
                                                    BlobContentInfo response = await blobClientThumbnail.UploadAsync(tempThumbnailFilePath, true);

                                                    var thumbnailBlobUrl = blobClientThumbnail.Uri.AbsoluteUri;

                                                    //API POST call

                                                    using (var client = new HttpClient())
                                                    {
                                                        var postUrl = "https://ec61-209-122-222-148.ngrok-free.app/api/MediaItems/add-media-item-thumbnail";
                                                        var apiKey = Environment.GetEnvironmentVariable("WebApiKey");
                                                        
                                                        var postData = new
                                                        {
                                                            videoAzureUrl = videoBlobUrl,
                                                            thumbnailAzureUrl = thumbnailBlobUrl
                                                        };

                                                        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                                                        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
                                                        var webApiResponse = await client.PostAsync(postUrl, content);
                                                        
                                                        if (webApiResponse.IsSuccessStatusCode)
                                                        {
                                                            log.LogInformation("API POST request successful.");
                                                        }
                                                        else
                                                        {
                                                            log.LogError($"API POST request failed with status code: {webApiResponse.StatusCode}");
                                                        }
                                                    }
                                                }
                                                else {
                                                    log.LogError("Thumbnail stream is empty or corrupted.");
                                                }
                                            }
                                            process.WaitForExit();
                                        }

                                        // Check if the output file exists
                                        if (File.Exists(tempVideoDirectory))
                                        {
                                            // Delete the existing file
                                            File.Delete(tempVideoDirectory);
                                        }
                                        // Check if the output file exists
                                        if (File.Exists(tempThumbnailFilePath))
                                        {
                                            // Delete the existing file
                                            File.Delete(tempThumbnailFilePath);
                                        }

                                        // log.LogInformation($"Thumbnail generated and stored as {name}_thumbnail.png");
                                    }
                                }
                            }
                            else
                            {
                                // Handle the case where the URL is invalid
                                log.LogError("input format is not valid. valid formats: .mp4", ".avi", ".mkv", ".mov");
                            }
                        }
                    }
                    else
                    {
                        log.LogError("Blob URL not found in event data.");
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError($"Error: {ex.Message}");
            }
        }

        private BlobUrlComponents DeserializeBlobUrl(string url)
        {
            Uri uri = new Uri(url);
            string[] segments = uri.Segments;
            if (segments.Length >= 3)
            {
                // The first segment is usually empty, the second is the container, and the rest is the input name
                return new BlobUrlComponents
                {
                    AzureUrl = uri.GetLeftPart(UriPartial.Authority),
                    Container = segments[1].Trim('/'),
                    InputName = string.Join(string.Empty, segments.Skip(2))
                };
            }
            else
            {
                // Handle invalid URLs as needed
                return null;
            }
        }

        private bool IsVideoFile(string fileName)
        {
            string[] videoExtensions = { ".mp4", ".avi", ".mkv", ".mov" }; // Add more video extensions as needed
            string extension = Path.GetExtension(fileName);
            return Array.Exists(videoExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private string CleanPathSegment(string pathSegment)
        {
            // Replace invalid characters with underscores
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                pathSegment = pathSegment.Replace(invalidChar, '_');
            }
            return pathSegment;
        }
    }
}
