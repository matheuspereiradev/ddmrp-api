using Service.Domain.Utils;

namespace Service.Tests.Application;

public class PermissionKeyUtilsTests
{
    [Theory]
    [InlineData("api/CenterProduct", "GET", "centerproduct:GET")]
    [InlineData("api/CenterProduct/{id}", "PUT", "centerproduct:PUT")]
    [InlineData("api/CenterProduct/{id:int}", "put", "centerproduct:PUT")]
    [InlineData("/api/Role/{id}/permissions", "delete", "role/permissions:DELETE")]
    [InlineData("api/CenterProduct/{id}/allocation-group", "PATCH", "centerproduct/allocation-group:PATCH")]
    public void BuildKey_NormalizesTemplateAndMethod(string routeTemplate, string httpMethod, string expected)
    {
        var key = PermissionKeyUtils.BuildKey(routeTemplate, httpMethod);

        Assert.Equal(expected, key);
    }
}
