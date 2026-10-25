
using Crud.Interfaces;
using Crud.Models;
using Crud.Services;
using Microsoft.AspNetCore.Authorization;

namespace Crud.Authorization
{
    public class DeleteProductOwnerHandler
        : AuthorizationHandler<DeleteProductOwnerRequirement, Product>
    {
        private readonly ICurrentUserService _currentUserService;

        public DeleteProductOwnerHandler(
            ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DeleteProductOwnerRequirement requirement,
            Product resource)
        {
            // Allow Admin users to delete any product
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Allow the product owner to delete their own product
            if (_currentUserService.GetCurrentUserId() == resource.CreatedBy)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
