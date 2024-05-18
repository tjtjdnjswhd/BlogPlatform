using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    [Table("Image")]
    public class Image
    {
        public Image(string name, string contentType, byte[] data)
        {
            Name = name;
            ContentType = contentType;
            Data = data;
        }

        public int Id { get; private set; }

        public string Name { get; set; }

        public string ContentType { get; set; }

        public byte[] Data { get; set; }
    }
}
