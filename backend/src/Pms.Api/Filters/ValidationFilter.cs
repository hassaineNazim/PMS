using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pms.Api.Filters;

/// <summary>
/// Runs any registered FluentValidation validator matching an action argument and
/// throws a <see cref="ValidationException"/> (mapped to 400 by the exception
/// middleware) before the action body executes.
/// </summary>
public class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is IValidator validator)
            {
                var ctx = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(ctx, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
