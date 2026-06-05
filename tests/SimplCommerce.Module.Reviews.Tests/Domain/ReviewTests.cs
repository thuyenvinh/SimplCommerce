using FluentAssertions;
using SimplCommerce.Module.Reviews.Models;
using Xunit;

namespace SimplCommerce.Module.Reviews.Tests.Domain;

public class ReviewTests
{
    [Fact]
    public void New_review_defaults_to_pending()
    {
        new Review().Status.Should().Be(ReviewStatus.Pending);
    }

    [Fact]
    public void New_review_stamps_created_on()
    {
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var r = new Review();
        var after = DateTimeOffset.Now.AddSeconds(1);
        r.CreatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void New_review_has_empty_replies()
    {
        var r = new Review();
        r.Replies.Should().NotBeNull();
        r.Replies.Should().BeEmpty();
    }

    [Fact]
    public void Reply_can_be_appended()
    {
        var r = new Review();
        r.Replies.Add(new Reply { Comment = "thanks" });
        r.Replies.Should().HaveCount(1);
        r.Replies.Single().Comment.Should().Be("thanks");
    }
}

public class ReplyTests
{
    [Fact]
    public void New_reply_defaults_to_pending()
    {
        new Reply().Status.Should().Be(ReplyStatus.Pending);
    }

    [Fact]
    public void New_reply_stamps_created_on()
    {
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var r = new Reply();
        var after = DateTimeOffset.Now.AddSeconds(1);
        r.CreatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}

public class ReviewStatusTests
{
    [Theory]
    [InlineData(ReviewStatus.Pending, 1)]
    [InlineData(ReviewStatus.Approved, 5)]
    [InlineData(ReviewStatus.NotApproved, 8)]
    public void Enum_values_are_stable(ReviewStatus s, int expected) =>
        ((int)s).Should().Be(expected);
}

public class ReplyStatusTests
{
    [Theory]
    [InlineData(ReplyStatus.Pending, 1)]
    [InlineData(ReplyStatus.Approved, 5)]
    [InlineData(ReplyStatus.NotApproved, 8)]
    public void Enum_values_are_stable(ReplyStatus s, int expected) =>
        ((int)s).Should().Be(expected);
}

public class ReviewListItemDtoTests
{
    [Fact]
    public void Round_trips_all_fields()
    {
        var dto = new ReviewListItemDto
        {
            Id = 1,
            UserId = 2,
            Title = "Nice",
            Comment = "Works great",
            Rating = 5,
            ReviewerName = "Alice",
            Status = ReviewStatus.Approved,
            CreatedOn = DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
            EntityTypeId = "Product",
            EntityId = 99,
            EntityName = "Blue Widget",
            EntitySlug = "blue-widget",
        };

        dto.Id.Should().Be(1);
        dto.UserId.Should().Be(2);
        dto.Title.Should().Be("Nice");
        dto.Comment.Should().Be("Works great");
        dto.Rating.Should().Be(5);
        dto.ReviewerName.Should().Be("Alice");
        dto.Status.Should().Be(ReviewStatus.Approved);
        dto.EntityTypeId.Should().Be("Product");
        dto.EntityId.Should().Be(99);
        dto.EntityName.Should().Be("Blue Widget");
        dto.EntitySlug.Should().Be("blue-widget");
    }
}
