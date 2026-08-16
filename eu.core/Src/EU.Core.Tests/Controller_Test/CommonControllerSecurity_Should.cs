using System.Reflection;
using EU.Core.AuthHelper;
using EU.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EU.Core.Tests.Controller_Test;

public sealed class CommonControllerSecurity_Should
{
    [Fact]
    public void Protect_agent_database_sync_with_permission_and_super_admin_role()
    {
        AuthorizeAttribute controllerAuthorization = Assert.Single(
            typeof(CommonController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(Permissions.Name, controllerAuthorization.Policy);

        MethodInfo method = typeof(CommonController).GetMethod(
            nameof(CommonController.SyncAgentDatabase))!;
        AuthorizeAttribute methodAuthorization = Assert.Single(
            method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("SuperAdmin", methodAuthorization.Roles);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());

        HttpPostAttribute route = Assert.Single(
            method.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("SyncAgentDatabase", route.Template);
    }
}
