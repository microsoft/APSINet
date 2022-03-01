using Microsoft.Research.APSI.Server;
using Xunit;

namespace APSILibraryTests
{
    public class OPRFKeyTests
    {
        [Fact]
        public void ToFromStringTest()
        {
            OPRFKey oprfKey = new();
            string str = oprfKey.ToString();

            OPRFKey oprfKey2 = new(str);
            string str2 = oprfKey2.ToString();

            OPRFKey oprfKey3 = new();
            string str3 = oprfKey3.ToString();

            Assert.Equal(str, str2);
            Assert.NotEqual(str, str3);
            Assert.NotEqual(str2, str3);
        }
    }
}
