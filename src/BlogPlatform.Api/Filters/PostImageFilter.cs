using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;

namespace BlogPlatform.Api.Filters
{
    public class PostImageFilter : ActionFilterAttribute
    {
        private static readonly Dictionary<string, List<byte[]>> imageTypeSignatures = new()
        {
            { "image/jpeg", [[0xFF, 0xD8]] },
            { "image/png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]] },
            { "image/gif", [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]] },
            { "image/bmp", [[0x42, 0x4D]] },
        };

        public string ImagesParameter { get; }

        public PostImageFilter(string imagesParameter)
        {
            ImagesParameter = imagesParameter;
        }

        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue(ImagesParameter, out object? images) || images is not IEnumerable<IFormFile> imageFiles)
            {
                Debug.Assert(false);
                return base.OnActionExecutionAsync(context, next);
            }

            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    context.ModelState.AddModelError(ImagesParameter, "이미지 최대 크기는 5MB입니다");
                    context.Result = new BadRequestObjectResult(context.ModelState);
                    return Task.CompletedTask;
                }

                if (!imageFile.ContentType.StartsWith("image/"))
                {
                    context.ModelState.AddModelError(ImagesParameter, "Content type은 image만 가능합니다");
                    context.Result = new BadRequestObjectResult(context.ModelState);
                    return Task.CompletedTask;
                }

                if (!IsImageFileValid(imageFile))
                {
                    context.ModelState.AddModelError(ImagesParameter, "올바르지 않은 이미지 파일입니다");
                    context.Result = new BadRequestObjectResult(context.ModelState);
                    return Task.CompletedTask;
                }
            }
            return base.OnActionExecutionAsync(context, next);
        }

        private static bool IsImageFileValid(IFormFile imageFile)
        {
            byte[] fileSignature = GetImageFileSignature(imageFile);
            if (imageFile.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                return fileSignature.Take(4).SequenceEqual("RIFF"u8.ToArray()) && fileSignature.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray());
            }

            return imageTypeSignatures.TryGetValue(imageFile.ContentType, out List<byte[]>? signatures) && signatures.Any(signature => StartWith(fileSignature, signature));
        }

        private static byte[] GetImageFileSignature(IFormFile imageFile)
        {
            using var memoryStream = new MemoryStream();
            imageFile.CopyTo(memoryStream);
            return memoryStream.ToArray().Take(16).ToArray();
        }

        private static bool StartWith(byte[] array, byte[] prefix)
        {
            if (array.Length < prefix.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (array[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
