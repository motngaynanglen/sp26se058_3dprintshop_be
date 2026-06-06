using FluentAssertions;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using NUnit.Framework;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Mainflow2;

public class Mainflow2TimelineBuilderTests
{
    [Test]
    public void Build_ShouldMarkCancelledAsSingleStep()
    {
        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            SourceType = SourceTypes.CustomQuoteMainflow2,
            CustomerId = Guid.NewGuid(),
            Status = Mainflow2DesignWorkStatuses.Cancelled,
            Created = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedBy = "test",
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test"
        };

        var steps = Mainflow2TimelineBuilder.Build(dw);

        steps.Should().HaveCount(1);
        steps[0].Code.Should().Be("CANCELLED");
        steps[0].IsCurrent.Should().BeTrue();
    }

    [Test]
    public void Build_ShouldSetCurrentToQuoted_WhenNegotiating()
    {
        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            SourceType = SourceTypes.CustomQuoteMainflow2,
            CustomerId = Guid.NewGuid(),
            Status = Mainflow2DesignWorkStatuses.Negotiating,
            Created = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = "test",
            MainAssignedStaffId = Guid.NewGuid(),
            StaffAssignedAt = DateTimeOffset.UtcNow.AddHours(-3),
            LastQuotedAt = DateTimeOffset.UtcNow.AddHours(-1),
            LatestQuotedPrice = 100000,
            QuoteRevision = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test"
        };

        var steps = Mainflow2TimelineBuilder.Build(dw);

        steps.Should().Contain(s => s.Code == "QUOTED" && s.IsCurrent);
    }
}
