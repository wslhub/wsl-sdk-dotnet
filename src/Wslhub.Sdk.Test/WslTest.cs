#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Wslhub.Sdk.Test
{
    static class WslTest
    {
        static void Test_SystemAssertion()
        {
            Wsl.AssertWslSupported();
        }

        static void Test_GetDistroListFromRegistryTest()
        {
            var distros = Wsl.GetDistroListFromRegistry();

            if (distros == null)
                throw new Exception("Cannot query registry keys.");
        }

        static void Test_QueryDistroInfo()
        {
            var distros = Wsl.GetDistroQueryResult();

            if (distros == null)
                throw new ArgumentNullException(nameof(distros));

            if (distros.Count(x => x.IsRegistered == false) > 0)
                throw new Exception("Cannot query distro properties.");
        }

        static void Test_GetDefaultDistro()
        {
            var defaultDistro = Wsl.GetDefaultDistro();

            if (defaultDistro == null)
                throw new Exception("Cannot find the default distro.");
        }

        static void Test_ExecTest()
        {
            var outputContent = Wsl.GetDefaultDistro().RunWslCommand("cat /etc/passwd");

            if (outputContent == null)
                throw new ArgumentNullException(nameof(outputContent));

            if (outputContent.Length == 0)
                throw new Exception("No output.");
        }

        static void Test_ExecTest_OutputStream()
        {
            using var outputStream = new MemoryStream();
            var outputLength = Wsl.GetDefaultDistro().RunWslCommand("ls /dev | gzip -", outputStream);

            if (outputLength == 0)
                throw new Exception("No output.");

            outputStream.Seek(0L, SeekOrigin.Begin);
            using var gzStream = new GZipStream(outputStream, CompressionMode.Decompress, true);
            using var streamReader = new StreamReader(gzStream, new UTF8Encoding(false), false);
            var content = streamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(content))
                throw new Exception("No output.");

            if (!content.Contains("null"))
                throw new Exception("Invalid output.");
        }
    }
}

#pragma warning restore IDE0051 // Remove unused private members
