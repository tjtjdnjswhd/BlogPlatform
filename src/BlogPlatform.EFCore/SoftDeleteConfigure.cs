using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using SoftDeleteServices.Configuration;

namespace BlogPlatform.EFCore
{
    public class SoftDeleteConfigure : CascadeSoftDeleteConfiguration<EntityBase>
    {
        private readonly TimeProvider _timeProvider;

        public SoftDeleteConfigure(DbContext context, TimeProvider timeProvider) : base(context)
        {
            _timeProvider = timeProvider;

            GetSoftDeleteValue = e => e.SoftDeleteLevel;
            SetSoftDeleteValue = (e, value) =>
            {
                if (value == 0)
                {
                    e.SoftDeletedAt = EntityBase.DefaultSoftDeletedAt;
                }
                else if (e.SoftDeleteLevel == 0)
                {
                    e.SoftDeletedAt = _timeProvider.GetUtcNow();
                }
                e.SoftDeleteLevel = value;
            };
        }
    }
}
