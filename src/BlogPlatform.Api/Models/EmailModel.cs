using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record EmailModel([EmailAddress] string Email);
}
