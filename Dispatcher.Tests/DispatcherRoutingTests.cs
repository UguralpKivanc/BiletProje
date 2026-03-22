using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Dispatcher.Controllers;

namespace Dispatcher.Tests
{
    public class DispatcherRoutingTests
    {
        [Fact]
        public async Task Dispatcher_Should_Return_NotFound_For_Invalid_Route()
        {
            // Arrange
            var controller = new GatewayController();

            // Act
            var result = await controller.ForwardToService("/invalid-path");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Dispatcher_Should_Forward_Events_Request_To_EventService()
        {
            // Arrange
            var controller = new GatewayController();
            string path = "events";

            // Act
            var result = await controller.ForwardToService(path);

            // Assert
            Assert.IsType<ContentResult>(result);
        }
    }
}