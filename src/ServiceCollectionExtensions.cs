using System;
using EventAggregator.Blazor;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds IEventAggregator as a singleton
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddEventAggregator(this IServiceCollection services, Action<EventAggregatorOptions> configure = null)
        {
            services.AddSingleton<IEventAggregator, EventAggregator.Blazor.EventAggregator>();

            if (configure != null)
            {
                services.Configure(configure);
            }
            
            return services;
        }
    }
}