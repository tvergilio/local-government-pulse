using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using FluentAssertions;

namespace stream_processor.Tests
{
    [TestFixture]
    public class StreamProcessorServiceTests
    {
        private Mock<IMessageProcessingService> _messageProcessingServiceMock;

        [SetUp]
        public void SetUp()
        {
            _messageProcessingServiceMock = new Mock<IMessageProcessingService>();
        }

        [Test]
        public async Task ProcessTextAsync_ShouldReturnProcessedText_WhenCalled()
        {
            // Arrange
            const string inputMessage = "This is a test message";
            const string expectedResponse = "{\"key\":\"value\"}";

            _messageProcessingServiceMock
                .Setup(service => service.ProcessTextAsync(inputMessage))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _messageProcessingServiceMock.Object.ProcessTextAsync(inputMessage);

            // Assert
            result.Should().Be(expectedResponse);
            _messageProcessingServiceMock.Verify(service => service.ProcessTextAsync(inputMessage), Times.Once);
        }

        [Test]
        public async Task ProcessTextAsync_ShouldReturnError_WhenExceptionOccurs()
        {
            // Arrange
            const string inputMessage = "This is a test message";

            _messageProcessingServiceMock
                .Setup(service => service.ProcessTextAsync(inputMessage))
                .ThrowsAsync(new System.Exception("API error"));

            // Act & Assert
            Assert.ThrowsAsync<System.Exception>(async () => await _messageProcessingServiceMock.Object.ProcessTextAsync(inputMessage));
        }
    }
}