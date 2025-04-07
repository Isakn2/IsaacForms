using CustomFormsApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CustomFormsApp.Services.Authorization;

public class TemplateOwnerAuthorizationHandler : AuthorizationHandler<TemplateOwnerRequirement, Template>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TemplateOwnerRequirement requirement,
        Template resource)
    {
        if (context.User.IsInRole("Admin") || 
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) == resource.CreatorId.ToString())
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}