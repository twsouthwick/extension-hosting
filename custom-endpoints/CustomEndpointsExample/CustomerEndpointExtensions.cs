using System.Reflection;
using CustomerEndpointDefinition;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

internal static class MetadataMappers
{
    internal static void AddDefaultMetadata(this EndpointBuilder builder, MethodInfo method)
    {
        builder.Metadata.Add(method);

        var attributes = method.GetCustomAttributes().Concat(method.DeclaringType?.GetCustomAttributes() ?? Enumerable.Empty<Attribute>());

        foreach (var attribute in attributes)
        {
            foreach (var mapped in MapMetadata(attribute))
            {
                builder.Metadata.Add(mapped);
            }
        }
    }

    private static IEnumerable<object> MapMetadata(object attribute)
    {
        yield return attribute;

        if (attribute is HttpGetAttribute)
        {
            yield return new HttpMethodMetadata(new[] { "GET" });
        }

        if(attribute is HttpPostAttribute)
        {
            yield return new HttpMethodMetadata(new[] { "POST" });
        }
    }
}

internal static partial class CustomerEndpointExtensions
{
    public static void UseCustomEndpoints<T>(this T app, Assembly assembly)
        where T : IApplicationBuilder, IEndpointRouteBuilder
    {
        app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Features.Get<CustomEndpointResult>() is { } result)
            {
                await ctx.Response.WriteAsJsonAsync(result.Result, result.Type, ctx.RequestAborted);
            }
        });

        app.MapCustomEndpoints(assembly);
    }

    private static IEndpointConventionBuilder MapCustomEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        var source = new CustomEndpointSource(assembly);

        endpoints.DataSources.Add(source);

        return source;
    }

    private class CustomEndpointSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly IChangeToken _token = new CancellationChangeToken(default);
        private readonly Assembly _assembly;
        private readonly List<Action<EndpointBuilder>> _conventions;

        private List<Endpoint>? _endpoints;

        public CustomEndpointSource(Assembly assembly)
        {
            _assembly = assembly;
            _conventions = new();
        }

        private List<Endpoint> CreateEndpoints(Assembly assembly)
        {
            var endpoints = new List<Endpoint>();

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is { } declaration && type.GetCustomAttribute<RouteAttribute>() is { } serviceRoute)
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (method.GetCustomAttribute<RouteAttribute>() is { } methodRoute)
                        {
                            var requestDelegateFactory = new RequestDelegateFactory(type, method);

                            var pattern = RoutePatternFactory.Parse(CreatePathString(serviceRoute.Path, methodRoute.Path));
                            var builder = new RouteEndpointBuilder(requestDelegateFactory.Create(), pattern, 0)
                            {
                                DisplayName = (string?)$"{declaration.Name}.{method.Name}",
                            };

                            builder.AddDefaultMetadata(method);

                            foreach (var convention in _conventions)
                            {
                                convention(builder);
                            }

                            endpoints.Add(builder.Build());
                        }
                    }
                }
            }

            return endpoints;
        }


        private static PathString CreatePathString(string serviceRoute, string methodRoute)
            => $"/{serviceRoute}/{methodRoute}";

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints is null)
                {
                    _endpoints = CreateEndpoints(_assembly).ToList();
                }

                return _endpoints;
            }
        }

        public override IChangeToken GetChangeToken() => _token;

        public void Add(Action<EndpointBuilder> convention) => _conventions.Add(convention);
    }
}