using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Models
{
    public class MediaItem
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }
        public string? folderId { get; set; }
        public string? name { get; set; }
        public string? userId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime createdAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime modifiedAt { get; set; }
        public string? size { get; set; }
        public string? type { get; set; }
        public string? mediaType { get; set; }
        public string? description { get; set; }
        public string? azureUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class Thumbnail
    {
        public string? videoAzureUrl { get; set; }
        public string? thumbnailAzureUrl { get; set; }
    }
}
