using FluentAssertions;
using Moq;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using Xunit;

namespace SimplCommerce.Module.Orders.Tests.Services;

public class OrderEmailServiceTests
{
    [Fact]
    public async Task Sends_html_email_with_order_number_in_subject()
    {
        string? toCapture = null;
        string? subjectCapture = null;
        string? bodyCapture = null;
        bool htmlCapture = false;
        var sender = new Mock<IEmailSender>();
        sender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
              .Callback<string, string, string, bool>((e, s, b, h) =>
              {
                  toCapture = e; subjectCapture = s; bodyCapture = b; htmlCapture = h;
              })
              .Returns(Task.CompletedTask);

        var sut = new OrderEmailService(sender.Object);
        var user = new User { Email = "alice@example.com", FullName = "Alice" };
        var order = new Order { OrderTotal = 123.45m };
        typeof(SimplCommerce.Infrastructure.Models.EntityBaseWithTypedId<long>)
            .GetProperty(nameof(Order.Id))!.SetValue(order, 321L);

        await sut.SendEmailToUser(user, order);

        toCapture.Should().Be("alice@example.com");
        subjectCapture.Should().Be("Order confirmation #321");
        htmlCapture.Should().BeTrue();
        bodyCapture.Should().Contain("#321");
        bodyCapture.Should().Contain("123.45");
        bodyCapture.Should().Contain("Alice");
    }

    [Fact]
    public async Task Skips_send_when_user_has_no_email()
    {
        var sender = new Mock<IEmailSender>();
        var sut = new OrderEmailService(sender.Object);

        await sut.SendEmailToUser(new User { Email = null }, new Order());

        sender.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void Body_html_encodes_customer_name_to_prevent_xss()
    {
        var order = new Order();
        var user = new User { Email = "x@y.z", FullName = "<script>alert(1)</script>" };

        var body = OrderEmailService.BuildOrderConfirmationHtml(order, user);

        body.Should().NotContain("<script>alert(1)</script>");
        body.Should().Contain("&lt;script&gt;");
    }
}
