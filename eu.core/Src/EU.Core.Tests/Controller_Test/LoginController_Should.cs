using Autofac;
using EU.Core.AuthHelper;
using EU.Core.Controllers;
using EU.Core.IServices;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EU.Core.Tests
{
    public class LoginController_Should
    {
        LoginController loginController;
         
        private readonly ISmUsersServices _smUsersServices;
        private readonly PermissionRequirement _requirement;
        private readonly ILogger<LoginController> _logger;

        DI_Test dI_Test = new DI_Test();


        public LoginController_Should()
        {
            var container = dI_Test.DICollections(); 
            _smUsersServices = container.Resolve<ISmUsersServices>();
            _requirement = container.Resolve<PermissionRequirement>();
            _logger = container.Resolve<ILogger<LoginController>>();
            loginController = new LoginController( _smUsersServices, _requirement,
                _logger);
        }

        [Fact]
        public void GetJwtStrTest()
        {
            var data = loginController.GetJwtStr("test", "test");

            Assert.NotNull(data);
        }

        [Fact]
        public void GetJwtStrForNuxtTest()
        {
            object Tiobons = loginController.GetJwtStrForNuxt("test", "test");

            Assert.NotNull(Tiobons);
        }

        [Fact]
        public async void GetJwtToken3Test()
        {
            var res = await loginController.GetJwtToken3("test", "test");

            Assert.NotNull(res);
        }

        [Fact]
        public async void RefreshTokenTest()
        {
            var res = await loginController.RefreshToken(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidGVzdCIsImp0aSI6IjgiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL2V4cGlyYXRpb24iOiIyMDE5LzEwLzE4IDIzOjI2OjQ5IiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW5UZXN0IiwibmJmIjoxNTcxNDA4ODA5LCJleHAiOjE1NzE0MTI0MDksImlzcyI6IkJsb2cuQ29yZSIsImF1ZCI6IndyIn0.oz-SPz6UCL78fM09bUecw5rmjcNYEY9dWGtuPs2gdBg");

            Assert.NotNull(res);
        }

        [Fact]
        public void Md5PasswordTest()
        {
            var res = loginController.Md5Password("test");

            Assert.NotNull(res);
        }
    }
}