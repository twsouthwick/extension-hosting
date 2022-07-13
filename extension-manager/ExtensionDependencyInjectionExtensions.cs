using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.Manager;

public static class ExtensionDependencyInjectionExtensions
{
    public static void AddExtensionManagement(this IServiceCollection services)
    {
        services.AddSingleton<IExtensionManager, InMemoryExtensionManager>();
        services.AddTransient<ExtensionInfoExtractor>();
    }
}
