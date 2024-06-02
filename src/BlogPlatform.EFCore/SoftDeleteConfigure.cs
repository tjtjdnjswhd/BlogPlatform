using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using SoftDeleteServices.Configuration;

namespace BlogPlatform.EFCore
{
    public class SoftDeleteConfigure : CascadeSoftDeleteConfiguration<EntityBase>
    {
        public SoftDeleteConfigure(DbContext context) : base(context)
        {
            GetSoftDeleteValue = e => e.SoftDeleteLevel;
            SetSoftDeleteValue = (e, value) =>
            {
                if (value == 0)
                {
                    e.SoftDeletedAt = null;
                }
                else if (e.SoftDeleteLevel == 0)
                {
                    e.SoftDeletedAt = DateTimeOffset.UtcNow;
                }
                e.SoftDeleteLevel = value;
            };
        }
    }
}
