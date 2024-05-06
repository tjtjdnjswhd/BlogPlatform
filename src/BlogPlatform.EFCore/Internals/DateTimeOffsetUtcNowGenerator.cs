using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace BlogPlatform.EFCore.Internals
{
    public class DateTimeOffsetUtcNowGenerator : ValueGenerator<DateTimeOffset>
    {
        public override bool GeneratesTemporaryValues => false;

        public override DateTimeOffset Next(EntityEntry entry)
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
