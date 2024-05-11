using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    [BasicSignUpValidate]
    public record BasicSignUpInfo([property: Required] string Id, [property: Required] string Password, [property: Required] string Email, [property: Required] string Name);
}
