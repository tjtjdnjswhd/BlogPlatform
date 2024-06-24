using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace BlogPlatform.EFCore.Internals
{
    public class DateTimeOffsetUtcNowGenerator : ValueGenerator<DateTimeOffset>
    {
        public override bool GeneratesTemporaryValues => false;

        private readonly TimeProvider _timeProvider;

        public DateTimeOffsetUtcNowGenerator(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public override DateTimeOffset Next(EntityEntry entry)
        {
            return _timeProvider.GetUtcNow();
        }
    }
}
