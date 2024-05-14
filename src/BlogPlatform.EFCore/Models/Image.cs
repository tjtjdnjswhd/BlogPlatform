using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    [Table("Image")]
    [Index(nameof(Uri), IsUnique = true)]
    public class Image
    {
        public Image(Uri uri, string contentType, byte[] data)
        {
            Uri = uri;
            ContentType = contentType;
            Data = data;
        }

        public int Id { get; private set; }

        public Uri Uri { get; set; }

        public string ContentType { get; set; }

        public byte[] Data { get; set; }
    }
}
