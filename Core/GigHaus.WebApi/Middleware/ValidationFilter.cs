using FluentValidation;

namespace GigHaus.WebApi.Middleware;

/// <summary>
/// Minimal API endpoint filter that runs FluentValidation on request bodies.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var validator = ctx.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(ctx);

        // Find the argument matching the type
        var arg = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null)
            return await next(ctx);

        var result = await validator.ValidateAsync(arg);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }

        return await next(ctx);
    }
}

