using Microsoft.Extensions.DependencyInjection;

namespace Shared.Observability.Core;

public interface IObservabilityBuilder
{
    IServiceCollection Services { get; }
}

internal class ObservabilityBuilder(IServiceCollection services) : IObservabilityBuilder
{
    public IServiceCollection Services { get; } = services;
}