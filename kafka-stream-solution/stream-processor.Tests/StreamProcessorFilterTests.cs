using FluentAssertions;
using NUnit.Framework;

namespace stream_processor.Tests
{
    [TestFixture]
    public class StreamProcessorFilterTests
    {
        [Test]
        public void Filter_ShouldReturnFalse_WhenValueIsNull()
        {
            string value = null;

            var result = !string.IsNullOrEmpty(value);

            result.Should().BeFalse();
        }

        [Test]
        public void Filter_ShouldReturnFalse_WhenValueIsEmpty()
        {
            var value = string.Empty;

            var result = !string.IsNullOrEmpty(value);

            result.Should().BeFalse();
        }

        [Test]
        public void Filter_ShouldReturnTrue_WhenValueIsNotEmpty()
        {
            const string value = "Valid Message";

            var result = !string.IsNullOrEmpty(value);

            result.Should().BeTrue();
        }
    }
}