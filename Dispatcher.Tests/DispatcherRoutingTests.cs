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
        private const string ValidKey = "KingoSifre123"; // Compass'taki anahtarın

        public DispatcherRoutingTests()
        {
            // Controller'ı hazırlıyoruz
            _controller = new GatewayController();

            // Controller'ın Request ve Header nesnelerine erişebilmesi için sahte Context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnNotFound_WhenPathIsEmpty()
        {
            // Bekçiyi geçmek için anahtarı Header'a ekle
            _controller.Request.Headers["X-Api-Key"] = ValidKey;

            var result = await _controller.ForwardToService("");

            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(404, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnBadRequest_WhenPathIsInvalid()
        {
            _controller.Request.Headers["X-Api-Key"] = ValidKey;

            var result = await _controller.ForwardToService("random-service");

            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(400, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturn503_WhenTargetServiceIsDown()
        {
            // Bu testin geçmesi için Dispatcher kodundaki try-catch bloğunun 503 döndüğünden emin olmalısın
            _controller.Request.Headers["X-Api-Key"] = ValidKey;

            var result = await _controller.ForwardToService("events");

            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(503, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
        {
            // Header'ı boşaltıyoruz
            _controller.Request.Headers.Clear();

            var result = await _controller.ForwardToService("events");

            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(401, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnForbidden_WhenApiKeyIsWrong()
        {
            // Yanlış anahtarı set ediyoruz
            _controller.Request.Headers["X-Api-Key"] = "YanlisSifre";

            var result = await _controller.ForwardToService("events");

            var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }
    }
}