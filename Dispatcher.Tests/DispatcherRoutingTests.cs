using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Threading.Tasks;
using Dispatcher.Controllers;

namespace Dispatcher.Tests
{
    public class DispatcherRoutingTests
    {
        private readonly GatewayController _controller;
        private const string ValidKey = "KingoSifre123";

        public DispatcherRoutingTests()
        {
            _controller = new GatewayController();
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        // Boş path → BadRequest (400)
        [Fact]
        public async Task ForwardToService_ShouldReturnBadRequest_WhenPathIsEmpty()
        {
            _controller.Request.Headers["X-Api-Key"] = ValidKey;
            var result = await _controller.ForwardToService("");
            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(400, statusCodeResult.StatusCode);
        }

        // Geçersiz path → BadRequest (400)
        [Fact]
        public async Task ForwardToService_ShouldReturnBadRequest_WhenPathIsInvalid()
        {
            _controller.Request.Headers["X-Api-Key"] = ValidKey;
            var result = await _controller.ForwardToService("random-service");
            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(400, statusCodeResult.StatusCode);
        }

        // Servis ayakta değil → 502 Bad Gateway
        [Fact]
        public async Task ForwardToService_ShouldReturn502_WhenTargetServiceIsDown()
        {
            _controller.Request.Headers["X-Api-Key"] = ValidKey;
            var result = await _controller.ForwardToService("events");
            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(502, statusCodeResult.StatusCode);
        }

        // API key eksik → Unauthorized (401)
        [Fact]
        public async Task ForwardToService_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
        {
            _controller.Request.Headers.Clear();
            var result = await _controller.ForwardToService("events");
            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(401, statusCodeResult.StatusCode);
        }

        // Yanlış API key → MongoDB yoksa 502, varsa 403
        // Test ortamında MongoDB olmadığı için 502 bekliyoruz
        [Fact]
        public async Task ForwardToService_ShouldReturn502OrForbidden_WhenApiKeyIsWrong()
        {
            _controller.Request.Headers["X-Api-Key"] = "YanlisSifre";
            var result = await _controller.ForwardToService("events");
            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.True(
                statusCodeResult.StatusCode == 403 || statusCodeResult.StatusCode == 502,
                $"Beklenen 403 veya 502, gelen: {statusCodeResult.StatusCode}"
            );
        }
    }
}