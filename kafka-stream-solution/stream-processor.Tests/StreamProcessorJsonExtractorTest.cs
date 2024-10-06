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
    }
}
