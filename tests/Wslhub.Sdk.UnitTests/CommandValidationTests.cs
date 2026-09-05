using System;
using System.IO;
using Xunit;

namespace Wslhub.Sdk.UnitTests
{
    public sealed class CommandValidationTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void MissingDistroIsRejectedBeforeNativeCalls(string? distro)
        {
            using var output = new MemoryStream();
            Assert.Throws<ArgumentException>("distroName", () => Wsl.RunWslCommand(distro!, "true", output));
        }

        [Fact]
        public void NullCommandIsRejectedBeforeNativeCalls()
        {
            using var output = new MemoryStream();
            Assert.Throws<ArgumentNullException>("commandLine", () => Wsl.RunWslCommand("test", null!, output));
        }

        [Fact]
        public void NullOutputIsRejectedBeforeNativeCalls()
        {
            Assert.Throws<ArgumentNullException>("outputStream", () => Wsl.RunWslCommand("test", "true", null!));
        }

        [Fact]
        public void ReadOnlyOutputIsRejectedBeforeNativeCalls()
        {
            using var output = new MemoryStream(Array.Empty<byte>(), writable: false);
            Assert.Throws<ArgumentException>("outputStream", () => Wsl.RunWslCommand("test", "true", output));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InvalidBufferIsRejectedBeforeNativeCalls(int bufferLength)
        {
            using var output = new MemoryStream();
            Assert.Throws<ArgumentOutOfRangeException>("bufferLength", () => Wsl.RunWslCommand("test", "true", output, bufferLength));
            Assert.Throws<ArgumentOutOfRangeException>("bufferLength", () => Wsl.RunWslCommand("test", "true", bufferLength));
        }

        [Fact]
        public void NullDistroModelIsRejected()
        {
            Assert.Throws<ArgumentNullException>("distroInfo", () => WslExtension.RunWslCommand(null!, "true"));
        }
    }
}
