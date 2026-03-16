using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks; // async Task için gerekli
using Dispatcher.Controllers;

namespace Dispatcher.Tests
{
    public class DispatcherRoutingTests
    {
        // 1. Önceki testin
        [Fact]
        public void Dispatcher_Should_Return_NotFound_For_Invalid_Route()
        {
            // Arrange
            var controller = new GatewayController();

            // Act
            var result = controller.Get("/invalid-path");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // 2. Yeni eklediğin testin (Burası sınıfın içi)
        [Fact]
        public async Task Dispatcher_Should_Forward_Events_Request_To_EventService()
        {
            // Arrange
            var controller = new GatewayController();
            string path = "/events";

            // Act
            var result = await controller.ForwardToService(path);

            // Assert
            Assert.IsType<ContentResult>(result);
        }
    }
}