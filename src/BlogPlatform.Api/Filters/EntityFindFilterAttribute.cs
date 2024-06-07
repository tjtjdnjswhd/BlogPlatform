using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;

namespace BlogPlatform.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EntityFindFilterAttribute<TEntity> : Attribute, IAsyncActionFilter
        where TEntity : EntityBase
    {
        public string IdParameterName { get; }

        public string? EntityParameterName { get; init; }

        public bool IgnoreSoftDelete { get; init; } = false;

        public EntityFindFilterAttribute(string idParameterName)
        {
            IdParameterName = idParameterName;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.RouteData.Values.TryGetValue(IdParameterName, out object? idValue) || idValue is not int entityId)
            {
                Debug.Assert(false);
                throw new ArgumentException("Id parameter not found or invalid");
            }

            if (EntityParameterName is not null
                &&
                (context.ActionDescriptor.Parameters.FirstOrDefault(p => p.Name == EntityParameterName) is not ParameterDescriptor entityParameter
                || !entityParameter.ParameterType.IsAssignableTo(typeof(TEntity))))
            {
                Debug.Assert(false);
                throw new ArgumentException("Entity parameter not found or invalid");
            }

            using var scope = context.HttpContext.RequestServices.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger<EntityFindFilterAttribute<TEntity>> logger = loggerFactory.CreateLogger<EntityFindFilterAttribute<TEntity>>();

            TEntity? entity = await dbContext.FindAsync<TEntity>([entityId], cancellationToken);
            if (entity == null)
            {
                logger.LogInformation("{entity} with id {id} not found", typeof(TEntity).Name, entityId);
                context.Result = new NotFoundResult();
                return;
            }

            if (EntityParameterName is not null)
            {
                context.ActionArguments[EntityParameterName] = entity;
            }

            await next();
        }
    }
}
