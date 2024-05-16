using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    [ModelBinder<OAuthSignUpInfoModelBinder>]
    public record OAuthSignUpInfo([property: Required] string Provider, [property: Required] string NameIdentifier, [property: Required, UserNameValidate] string Name, [property: Required] string Email);
}
