using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Wslhub.Sdk.UnitTests
{
    public sealed class PipeTests
    {
        [WindowsFact]
        public async Task LargeOutputAndShortReadsAreDrainedUntilTheWriterCloses()
        {
            var data = Enumerable.Range(0, 2 * 1024 * 1024).Select(index => (byte)(index % 251)).ToArray();
            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),
            };
            Assert.True(NativeMethods.CreatePipe(out var read, out var write, ref attributes, 0));
            using (read)
            using (write)
            using (var output = new MemoryStream())
            {
                var reader = Task.Run(() => WslPipe.CopyTo(read, output, 4096));
                var writer = Task.Run(async () =>
                {
                    using var stream = new FileStream(write, FileAccess.Write, bufferSize: 1);
                    stream.Write(data, 0, 3);
                    await Task.Delay(100);
                    stream.Write(data, 3, data.Length - 3);
                });
                await Task.WhenAll(reader, writer).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(data.Length, await reader);
                Assert.Equal(data, output.ToArray());
                Assert.True(output.CanWrite);
            }
        }

        [WindowsFact]
        public void ClosedWriterProducesEmptyOutput()
        {
            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),
            };
            Assert.True(NativeMethods.CreatePipe(out var read, out var write, ref attributes, 0));
            using (read)
            using (write)
            using (var output = new MemoryStream())
            {
                write.Dispose();
                Assert.Equal(0, WslPipe.CopyTo(read, output, 1));
                Assert.Empty(output.ToArray());
            }
        }

        [WindowsFact]
        public void NativeErrorsAreNotMistakenForEndOfOutput()
        {
            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),
            };
            Assert.True(NativeMethods.CreatePipe(out var read, out var write, ref attributes, 0));
            using (read)
            using (write)
            using (var output = new MemoryStream())
                Assert.Throws<Win32Exception>(() => WslPipe.CopyTo(write, output, 1));
        }
    }
}
