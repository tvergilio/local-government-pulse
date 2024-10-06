using DotnetGeminiSDK.Client.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace stream_processor.Tests
{
    [TestFixture]
    public class GeminiServiceTests
    {
        private Mock<IGeminiClient> _mockGeminiClient;
        private Mock<ILogger<GeminiService>> _mockLogger;
        private GeminiService _geminiService;

        [SetUp]
        public void Setup()
        {
            _mockGeminiClient = new Mock<IGeminiClient>();
            _mockLogger = new Mock<ILogger<GeminiService>>();
            _geminiService = new GeminiService(_mockGeminiClient.Object, _mockLogger.Object);
        }

        [Test]
        public void SanitiseInput_WhenInputExceeds280Characters_ShouldTrimInput()
        {
            // Arrange
            var input = new string('a', 300); // Input with 300 'a' characters
            var expectedSanitisedInput = new string('a', 280); // Expected sanitised input with 280 'a' characters

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputIsValid_ShouldReturnSameInput()
        {
            // Arrange
            const string input = "This is a clean message without special characters.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(input);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsBackticks_ShouldReplaceWithSingleQuotes()
        {
            // Arrange
            const string input = "This `is` a test.";
            const string expectedSanitisedInput = "This 'is' a test.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsBraces_ShouldRemoveBraces()
        {
            // Arrange
            const string input = "This {is} a [test] message.";
            const string expectedSanitisedInput = "This is a test message.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsDashes_ShouldRemoveDashes()
        {
            // Arrange
            const string input = "This -- is a test -- message.";
            const string expectedSanitisedInput = "This  is a test  message.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsColons_ShouldRemoveColons()
        {
            // Arrange
            const string input = "This: is a test: message.";
            const string expectedSanitisedInput = "This is a test message.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsBackslashes_ShouldRemoveBackslashes()
        {
            // Arrange
            const string input = "This \\ is a \\ test message.";
            const string expectedSanitisedInput = "This  is a  test message.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsQuotes_ShouldRemoveQuotes()
        {
            // Arrange
            const string input = "This \"is\" a test message.";
            const string expectedSanitisedInput = "This is a test message.";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputContainsAllSpecialCharacters_ShouldSanitiseCorrectly()
        {
            // Arrange
            const string input = "`{[This -- is : a \\ \"test\"]}`";
            const string expectedSanitisedInput = "'This  is  a  test'";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputIsEmpty_ShouldReturnEmptyString()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void SanitiseInput_WhenInputContainsOnlySpecialCharacters_ShouldReturnEmptyString()
        {
            // Arrange
            const string input = "{}[]--:\"\\";
            var expectedSanitisedInput = string.Empty;

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(expectedSanitisedInput);
        }

        [Test]
        public void SanitiseInput_WhenInputHasLeadingAndTrailingWhitespace_ShouldPreserveWhitespace()
        {
            // Arrange
            const string input = "   This is a test message.   ";

            // Act
            var result = _geminiService.SanitiseInput(input);

            // Assert
            result.Should().Be(input);
        }
    }
}
