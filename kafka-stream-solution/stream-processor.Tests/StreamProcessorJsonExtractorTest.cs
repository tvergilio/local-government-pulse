using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace stream_processor.Tests
{
    [TestFixture]
    public class StreamProcessorJsonExtractorTests
    {
        [Test]
        public void ExtractJsonFromResponse_ShouldReturnNull_WhenInputIsNull()
        {
            string input = null;

            var result = StreamProcessor.ExtractJsonFromResponse(input);

            result.Should().BeNull();
        }

        [Test]
        public void ExtractJsonFromResponse_ShouldReturnNull_WhenInputIsEmpty()
        {
            var input = string.Empty;

            var result = StreamProcessor.ExtractJsonFromResponse(input);

            result.Should().BeNull();
        }

        [Test]
        public void ExtractJsonFromResponse_ShouldReturnNull_WhenJsonMarkersAreMissing()
        {
            const string input = "This is not valid JSON content.";

            var result = StreamProcessor.ExtractJsonFromResponse(input);

            result.Should().BeNull();
        }

        [Test]
        public void ExtractJsonFromResponse_ShouldReturnJson_WhenValidJsonMarkersArePresent()
        {
            const string input = "`json{\"key\":\"value\"}```";
            const string expectedJson = "{\"key\":\"value\"}";

            var result = StreamProcessor.ExtractJsonFromResponse(input);

            result.Should().Be(expectedJson);
        }

        [Test]
        public void ExtractJsonFromResponse_ShouldReturnNull_WhenMarkersAreImproperlyPlaced()
        {
            const string input = "`json This is not properly closed";

            var result = StreamProcessor.ExtractJsonFromResponse(input);

            result.Should().BeNull();
        }

        [Test]
        public void ExtractFullJsonFromResponse_ShouldReturnValidJson_WhenBothParametersAreValid()
        {
            // Arrange
            var originalMessage = "This is the original message";
            var processedData = "```json{\"key\":\"value\"}```";

            // Act
            var result = StreamProcessor.ExtractFullJsonFromResponse(originalMessage, processedData);

            // Assert
            var expectedJson = JsonSerializer.Serialize(new
            {
                originalMessage = originalMessage,
                extractedJson = "{\"key\":\"value\"}"
            });
            result.Should().Be(expectedJson);
        }

        [Test]
        public void
            ExtractFullJsonFromResponse_ShouldReturnNullForExtractedJson_WhenProcessedDataDoesNotContainValidJson()
        {
            // Arrange
            var originalMessage = "This is the original message";
            var processedData = "No valid json data here";

            // Act
            var result = StreamProcessor.ExtractFullJsonFromResponse(originalMessage, processedData);

            // Assert
            var expectedJson = JsonSerializer.Serialize(new
            {
                originalMessage = originalMessage,
                extractedJson = (string)null
            });
            result.Should().Be(expectedJson);
        }

        [Test]
        public void ExtractFullJsonFromResponse_ShouldHandleEmptyOriginalMessage()
        {
            // Arrange
            var originalMessage = "";
            var processedData = "```json{\"key\":\"value\"}```";

            // Act
            var result = StreamProcessor.ExtractFullJsonFromResponse(originalMessage, processedData);

            // Assert
            var expectedJson = JsonSerializer.Serialize(new
            {
                originalMessage = originalMessage,
                extractedJson = "{\"key\":\"value\"}"
            });
            result.Should().Be(expectedJson);
        }

        [Test]
        public void ExtractFullJsonFromResponse_ShouldReturnNullForExtractedJson_WhenProcessedDataIsEmpty()
        {
            // Arrange
            var originalMessage = "This is the original message";
            var processedData = "";

            // Act
            var result = StreamProcessor.ExtractFullJsonFromResponse(originalMessage, processedData);

            // Assert
            var expectedJson = JsonSerializer.Serialize(new
            {
                originalMessage = originalMessage,
                extractedJson = (string)null
            });
            result.Should().Be(expectedJson);
        }

        [Test]
        public void ExtractFullJsonFromResponse_ShouldReturnNullForExtractedJson_WhenMarkersAreImproperlyPlaced()
        {
            // Arrange
            var originalMessage = "This is the original message";
            var processedData = "`json This is not properly closed";

            // Act
            var result = StreamProcessor.ExtractFullJsonFromResponse(originalMessage, processedData);

            // Assert
            var expectedJson = JsonSerializer.Serialize(new
            {
                originalMessage = originalMessage,
                extractedJson = (string)null
            });
            result.Should().Be(expectedJson);
        }
    }
}