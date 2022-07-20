using System.Reflection;

internal static partial class CustomerEndpointExtensions
{
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