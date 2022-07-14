using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;

namespace Extension.Manager;

public sealed class ExtensionInstance : IDisposable
{
    private readonly AssemblyLoadContext _alc;
    private readonly ILogger<ExtensionInstance> _logger;
    private readonly CompositeEntryPoint _composite;

    public ExtensionInstance(IServiceProvider services, string path)
    {
        _alc = new ExtensionAssemblyLoadContext(path);
        _logger = services.GetRequiredService<ILogger<ExtensionInstance>>();

        var assembly = _alc.LoadFromAssemblyPath(path);
        var entrypoints = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsPublic)
            .Where(t => typeof(IEntryPoint).IsAssignableFrom(t))
            .Select(t => (IEntryPoint)ActivatorUtilities.CreateInstance(services, t))
            .ToArray();

        _composite = new CompositeEntryPoint(this, entrypoints, _logger);

        _alc.Unloading += Unloading;
        Path = path;
    }

    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    public string Path { get; }

    public IEntryPoint EntryPoint => _composite;

    public void Unloading(AssemblyLoadContext context)
    {
        _alc.Unloading -= Unloading;

        _logger.LogTrace("Unloading extension {Path}", Path);
    }

    public void Dispose()
    {
        _logger.LogTrace("Unloading extension {Path}", Path);

        _composite.Clear();
        _alc.Unload();
    }

    private class CompositeEntryPoint : IEntryPoint
    {
        private readonly ExtensionInstance _instance;
        private readonly ILogger _logger;

        private IEntryPoint[] _other;

        public CompositeEntryPoint(ExtensionInstance instance, IEntryPoint[] other, ILogger logger)
        {
            _instance = instance;
            _other = other;
            _logger = logger;
        }

        public void Clear() => Interlocked.Exchange(ref _other, Array.Empty<IEntryPoint>());

        public async Task RunAsync(CancellationToken token)
        {
            if (_other is null)
            {
                _logger.LogInformation("Extension {Path} has been unloaded", _instance.Path);
                return;
            }

            _logger.LogInformation("Running extension {Path}", _instance.Path);

            foreach (var other in _other)
            {
                await other.RunAsync(token);
            }

            _logger.LogInformation("Done running extension {Path}", _instance.Path);
        }
    }
}

public record EntryPointInfo(string AssemblyName, string TypeName);
