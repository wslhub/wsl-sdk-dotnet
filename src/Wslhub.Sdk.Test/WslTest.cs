using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Wslhub.Sdk.Test
{
    [TestClass]
    public class WslTest
    {
        [TestMethod]
        public void SystemAssertion()
        {
            Wsl.AssertWslSupported();
        }

        [TestMethod]
        public void GetDistroListFromRegistryTest()
        {
            var distros = Wsl.GetDistroListFromRegistry();
            Assert.IsNotNull(distros);
        }

        [TestMethod]
        public void QueryDistroInfo()
        {
            var distros = Wsl.GetDistroQueryResult();
            Assert.IsNotNull(distros);
        }
    }
}
