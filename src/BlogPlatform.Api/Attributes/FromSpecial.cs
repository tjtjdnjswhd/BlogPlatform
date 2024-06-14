using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromSpecial : Attribute, IBindingSourceMetadata
    {
        public BindingSource? BindingSource => BindingSource.Special;
    }
}
