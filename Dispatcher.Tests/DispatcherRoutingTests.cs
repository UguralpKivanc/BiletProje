using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Dispatcher.Controllers;

namespace Dispatcher.Tests
{
    public class DispatcherRoutingTests
    {
        private readonly GatewayController _controller;

        public DispatcherRoutingTests()
        {
            // Controller'ı her test için hazırla
            _controller = new GatewayController();
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnNotFound_WhenPathIsEmpty()
        {
            // Act: Boş bir yol gönderiyoruz
            var result = await _controller.ForwardToService("");

            // Assert: NotFound dönmesini bekliyoruz (İster 3.1)
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturnBadRequest_WhenPathIsInvalid()
        {
            // Act: Tanımsız bir servis ismi gönderiyoruz
            var result = await _controller.ForwardToService("random-service");

            // Assert: BadRequest dönmesini bekliyoruz
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ForwardToService_ShouldReturn503_WhenTargetServiceIsDown()
        {
            // Act: Geçerli bir yol ("events") ama servis kapalıyken deniyoruz
            // (Şu an servislerin kapalı olduğunu varsayıyoruz)
            var result = await _controller.ForwardToService("events");

            // Assert: Servis kapalı olduğu için 503 Service Unavailable bekliyoruz
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusCodeResult.StatusCode);
        }
    }
}