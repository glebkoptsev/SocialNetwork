using Libraries.Web.Common.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.Tests;

public class SecurityRequirementFilterTests
{
    private static OperationFilterContext CreateContext(bool hasAuthorize, bool hasAllowAnonymous)
    {
        var actionDescriptor = new ActionDescriptor();
        var metadata = new List<object>();
        if (hasAuthorize)
            metadata.Add(new AuthorizeAttribute());
        if (hasAllowAnonymous)
            metadata.Add(new AllowAnonymousAttribute());
        actionDescriptor.EndpointMetadata = metadata;

        var apiDescription = new ApiDescription
        {
            ActionDescriptor = actionDescriptor
        };

        return new OperationFilterContext(
            apiDescription,
            null!,
            null!,
            null!);
    }

    [Fact]
    public void Apply_NoAuthorize_DoesNotAddSecurity()
    {
        var filter = new SecurityRequirementFilter("Bearer");
        var operation = new OpenApiOperation();
        var context = CreateContext(false, false);
        filter.Apply(operation, context);
        Assert.Empty(operation.Security ?? []);
    }

    [Fact]
    public void Apply_WithAllowAnonymous_DoesNotAddSecurity()
    {
        var filter = new SecurityRequirementFilter("Bearer");
        var operation = new OpenApiOperation();
        var context = CreateContext(true, true);
        filter.Apply(operation, context);
        Assert.Empty(operation.Security ?? []);
    }

    [Fact]
    public void Apply_WithAuthorize_AddsSecurity()
    {
        var filter = new SecurityRequirementFilter("Bearer");
        var operation = new OpenApiOperation();
        var context = CreateContext(true, false);
        filter.Apply(operation, context);
        Assert.NotNull(operation.Security);
        Assert.NotEmpty(operation.Security);
    }

    [Fact]
    public void Apply_MultipleRefIds_AddsMultipleRequirements()
    {
        var filter = new SecurityRequirementFilter("Bearer;ApiKey");
        var operation = new OpenApiOperation();
        var context = CreateContext(true, false);
        filter.Apply(operation, context);
        Assert.NotNull(operation.Security);
        Assert.Equal(2, operation.Security.Count);
    }

    [Fact]
    public void Apply_SingleRefId_AddsOneRequirement()
    {
        var filter = new SecurityRequirementFilter("Bearer");
        var operation = new OpenApiOperation();
        var context = CreateContext(true, false);
        filter.Apply(operation, context);
        Assert.NotNull(operation.Security);
        var secReq = Assert.Single(operation.Security);
        var kvp = Assert.Single(secReq);
        Assert.Equal("Bearer", kvp.Key.Reference.Id);
    }


}
