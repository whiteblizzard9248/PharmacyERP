using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shsmg.Pharma.Application.Behaviors;

namespace Shsmg.Pharma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, Action<MediatRServiceConfiguration>? configure = null)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configure?.Invoke(cfg);
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));

        return services;
    }
}