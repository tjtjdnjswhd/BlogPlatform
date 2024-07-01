using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models
{
    /// <summary>
    /// 응답할 오류를 나타냅니다
    /// </summary>
    /// <param name="Message"></param>
    public record Error([Required] string Message);
}
