using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models
{
    public record EmailModel([EmailAddress] string Email);
}
