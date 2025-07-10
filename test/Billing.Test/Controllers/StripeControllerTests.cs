using Bit.Billing.Controllers;
using Bit.Billing.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using System.Text;

namespace Bit.Billing.Test.Controllers;

[ControllerCustomize(typeof(StripeController))]
[SutProviderCustomize]
public class StripeControllerTests
{
    [Theory]
    [BitAutoData]
    public async Task PostWebhook_InvalidWebhookKey_BadRequest(SutProvider<StripeController> sutProvider)
    {
        var billingSettings = new BillingSettings { StripeWebhookKey = "correct-key" };
        sutProvider.GetDependency<IOptions<BillingSettings>>().Value.Returns(billingSettings);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        
        sutProvider.Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var response = await sutProvider.Sut.PostWebhook("definitely-wrong-key");

        Assert.IsType<BadRequestResult>(response);
    }

    [Fact]
    public async Task PostWebhook_ValidKey_ProcessesRequest()
    {
        var billingSettings = new BillingSettings { StripeWebhookKey = "test-key" };
        var options = Substitute.For<IOptions<BillingSettings>>();
        options.Value.Returns(billingSettings);
        
        var logger = Substitute.For<ILogger<StripeController>>();
        var hostingEnvironment = Substitute.For<IWebHostEnvironment>();
        var stripeEventService = Substitute.For<IStripeEventService>();
        var stripeEventProcessor = Substitute.For<IStripeEventProcessor>();
        
        var controller = new StripeController(
            options, 
            hostingEnvironment, 
            logger, 
            stripeEventService, 
            stripeEventProcessor);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var response = await controller.PostWebhook("test-key");

        Assert.IsNotType<BadRequestResult>(response);
    }
}