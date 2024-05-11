using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    [ModelBinder<OAuthLoginInfoModelBinder>]
    public record OAuthLoginInfo([property: Required] string Provider, [property: Required] string NameIdentifier);
}
