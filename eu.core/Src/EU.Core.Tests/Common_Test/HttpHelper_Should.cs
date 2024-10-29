using EU.Core.Common.Helper;
using Xunit;

namespace EU.Core.Tests.Common_Test
{
    public class HttpHelper_Should
    {

        [Fact]
        public async void Get_Async_Test()
        {
            var responseString = (await HttpHelper.GetAsync("http://apk.neters.club/api/EU"));

            Assert.NotNull(responseString);
        }

        [Fact]
        public void Post_Async_Test()
        {
            var responseString = HttpHelper.PostAsync("http://apk.neters.club/api/Login/swgLogin", "{\"name\":\"admin\",\"pwd\":\"admin\"}");

            Assert.NotNull(responseString);
        }

    }
}
