using Xunit;
using Microsoft.AspNetCore.Mvc;
using Products.Api.Controllers;

namespace Products.Api.Tests;

public class HealthControllerTests
{
    [Fact]
    public void Get_devuelve_200_OK()
    {
        var result = new HealthController().Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public void Get_devuelve_status_ok()
    {
        var result = (OkObjectResult)new HealthController().Get();

        Assert.Equal("{ status = ok }", result.Value!.ToString());
    }
}
