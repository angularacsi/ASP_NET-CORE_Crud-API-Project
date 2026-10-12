using Microsoft.AspNetCore.Authorization;

namespace Crud.Authorization
{
    public class DeleteProductAuthorizationHandler: AuthorizationHandler<DeleteProductRequirement>
    {

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DeleteProductRequirement requirement)
        {
            // Check if the user has the "CanDeleteProduct" claim
            if (context.User.HasClaim(c => c.Type == "CanDeleteProduct" && c.Value == "true"))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
