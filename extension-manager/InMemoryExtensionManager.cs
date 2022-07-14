using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Extension.Manager;

internal class InMemoryExtensionManager : IExtensionManager
{
    private ImmutableDictionary<string, ExtensionInstance> _extensions = ImmutableDictionary<string, ExtensionInstance>.Empty;

    private readonly IServiceProvider _services;
    private readonly ILogger<InMemoryExtensionManager> _logger;

    public InMemoryExtensionManager(IServiceProvider services, ILogger<InMemoryExtensionManager> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task<ExtensionInstance> AddAsync(string path)
    {
        try
        {
            var info = ImmutableInterlocked.GetOrAdd(ref _extensions, path, static (path, services) => new(services, path), _services);

            return Task.FromResult(info);
        }
        catch (Exception e)
        {
            throw new ExtensionException("Error adding exception", e);
        }
    }

    public Task<ExtensionInstance?> DeleteAsync(string path)
    {
        if (ImmutableInterlocked.TryRemove(ref _extensions, path, out var result))
        {
            result.Dispose();
            return Task.FromResult(result)!;
        }

        return Task.FromResult<ExtensionInstance?>(null);
    }

    public async Task<IEnumerable<string>> RunAsync(CancellationToken token)
    {
        var context = new Context();

        foreach (var extension in _extensions.Values)
        {
            await extension.EntryPoint.RunAsync(context, token);
        }

        return context.Messages;
    }

    public IEnumerable<ExtensionInstance> Extensions => _extensions.Values;
}
