using System.Reflection;
using CustomerEndpointDefinition;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

internal static partial class CustomerEndpointExtensions
{
    public static IEndpointConventionBuilder MapCustomEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        var source = new CustomEndpointSource(assembly);

        endpoints.DataSources.Add(source);

        return source;
    }

    private static void AddDefaultMetadata(this EndpointBuilder builder, MethodInfo method)
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

        if (attribute is HttpPostAttribute)
        {
            yield return new HttpMethodMetadata(new[] { "POST" });
        }
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
                            var requestInvoke = requestDelegateFactory.Create();
                            var builder = new RouteEndpointBuilder(async (HttpContext context) =>
                            {
                                await requestInvoke(context);

                                if (context.Features.Get<CustomEndpointResult>() is { } result)
                                {
                                    await context.Response.WriteAsJsonAsync(result.Result, result.Type, context.RequestAborted);
                                }
                            }, pattern, 0)
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

        private record CustomEndpointResult(Type Type)
        {
            public object? Result { get; init; }
        }

        private class RequestDelegateFactory
        {
            private readonly ObjectFactory _factory;
            private readonly MethodInfo _method;

            public RequestDelegateFactory(Type container, MethodInfo method)
            {
                _factory = ActivatorUtilities.CreateFactory(container, Array.Empty<Type>());
                _method = method;
            }

            public RequestDelegate Create()
            {
                if (_method.ReturnType is { IsGenericType: true } r && r.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var type = r.GetGenericArguments()[0];
                    var getMethod = r.GetProperty("Result")!.GetGetMethod()!;

                    return async context =>
                    {
                        var instance = _factory(context.RequestServices, null);
                        var task = _method.Invoke(instance, null);

                        await (task as Task)!;

                        var result = getMethod.Invoke(task, null);

                        context.Features.Set(new CustomEndpointResult(type) { Result = result });
                    };
                }
                else if (_method.ReturnType == typeof(Task))
                {
                    return context =>
                    {
                        var instance = _factory(context.RequestServices, null);
                        var task = _method.Invoke(instance, null);

                        return (Task)task!;
                    };
                }
                else
                {
                    return context =>
                    {
                        var instance = _factory(context.RequestServices, null);
                        var result = _method.Invoke(instance, null);

                        context.Features.Set(new CustomEndpointResult(_method.ReturnType) { Result = result });

                        return Task.CompletedTask;
                    };
                }
            }
        }
    }
}