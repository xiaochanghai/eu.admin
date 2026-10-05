using EU.Core.Model.Base;
using EU.Core.Model.Edit;
using EU.Core.Model.Entity;
using EU.Core.Model.Insert;
using Xunit;

namespace EU.Core.Tests.Common_Test;

public class SmModulesSearchVisibility_Should
{
    [Fact]
    public void Expose_nullable_search_visibility_in_module_entity_and_input_contract()
    {
        Assert.Equal(typeof(bool?), typeof(SmModules).GetProperty("IsShowSearch")?.PropertyType);
        Assert.Equal(typeof(bool?), typeof(SmModulesBase).GetProperty("IsShowSearch")?.PropertyType);
        Assert.Equal(typeof(bool?), typeof(InsertSmModulesInput).GetProperty("IsShowSearch")?.PropertyType);
        Assert.Equal(typeof(bool?), typeof(EditSmModulesInput).GetProperty("IsShowSearch")?.PropertyType);
    }
}
