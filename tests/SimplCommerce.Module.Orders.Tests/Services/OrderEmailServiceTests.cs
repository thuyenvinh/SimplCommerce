using FluentAssertions;
using Moq;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using Xunit;

namespace SimplCommerce.Module.Orders.Tests.Services;

public class OrderEmailServiceTests
{
    [Fact]
    public async Task Renders_template_with_order_and_sends_html_email()
    {
        var renderer = new Mock<IRazorViewRenderer>();
        renderer.Setup(x => x.RenderViewToStringAsync(
                "/Areas/Orders/Views/EmailTemplates/OrderEmailToCustomer.cshtml",
                It.IsAny<Order>()))
            .ReturnsAsync("<html>hi</html>");

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

        var sut = new OrderEmailService(sender.Object, renderer.Object);
        var user = new User { Email = "alice@example.com" };
        var order = new Order();
        typeof(SimplCommerce.Infrastructure.Models.EntityBaseWithTypedId<long>)
            .GetProperty(nameof(Order.Id))!.SetValue(order, 321L);

        await sut.SendEmailToUser(user, order);

        toCapture.Should().Be("alice@example.com");
        subjectCapture.Should().Be("Order information #321");
        bodyCapture.Should().Be("<html>hi</html>");
        htmlCapture.Should().BeTrue();
        renderer.VerifyAll();
    }

    [Fact]
    public async Task Passes_order_as_model_to_renderer()
    {
        Order? seen = null;
        var renderer = new Mock<IRazorViewRenderer>();
        renderer.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<Order>()))
                .Callback<string, Order>((_, o) => seen = o)
                .ReturnsAsync(string.Empty);
        var sender = new Mock<IEmailSender>();
        sender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
              .Returns(Task.CompletedTask);

        var order = new Order();
        await new OrderEmailService(sender.Object, renderer.Object).SendEmailToUser(new User { Email = "x@y.z" }, order);

        seen.Should().BeSameAs(order);
    }
}
