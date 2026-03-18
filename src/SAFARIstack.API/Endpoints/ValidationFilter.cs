using FluentValidation;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Auto-detecting Minimal API endpoint filter that validates request bodies using FluentValidation.
/// Inspects all endpoint arguments, resolves matching validators from DI, and returns 400 with
/// structured validation errors when validation fails. No need to specify types — fully automatic.
/// </summary>
public class AutoValidationFilter : IEndpointFilter
{
    private static readonly HashSet<Type> _skipTypes = new()
    {
        typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset),
        typeof(int), typeof(long), typeof(decimal), typeof(bool), typeof(double), typeof(float)
    };

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null || arg is CancellationToken || arg is HttpContext) continue;

            var argType = arg.GetType();
            if (_skipTypes.Contains(argType) || argType.IsEnum
                || argType.IsPrimitive || Nullable.GetUnderlyingType(argType) is not null)
                continue;

            // Resolve IValidator<T> for the argument type
            var validatorType = typeof(IValidator<>).MakeGenericType(argType);
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;
            if (validator is null) continue;

            // Create ValidationContext<T> and validate
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argType);
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, arg)!;
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(
                    result.ToDictionary(),
                    title: "One or more validation errors occurred.",
                    statusCode: 400);
            }
        }

        return await next(context);
    }
}

/// <summary>
/// Extension methods for adding auto-validation filters to endpoint groups and individual endpoints.
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>
    /// Adds auto-validation filter to a route group — validates all request bodies automatically.
    /// </summary>
    public static RouteGroupBuilder WithAutoValidation(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<AutoValidationFilter>();
        return group;
    }

    /// <summary>
    /// Adds auto-validation filter to a single endpoint.
    /// </summary>
    public static RouteHandlerBuilder WithAutoValidation(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<AutoValidationFilter>();
        return builder;
    }
}
