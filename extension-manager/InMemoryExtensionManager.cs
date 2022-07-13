using System.Collections.Immutable;
using System.Threading;

namespace Extension.Manager;

internal class InMemoryExtensionManager : IExtensionManager
{
    private ImmutableList<ExtensionInfo> _extensions = ImmutableList<ExtensionInfo>.Empty;
    private readonly ExtensionInfoExtractor _extractor;

    public InMemoryExtensionManager(ExtensionInfoExtractor extractor)
    {
        _extractor = extractor;
    }

    public Task Add(string name, string directory)
    {
        Interlocked.Exchange(ref _extensions, _extensions.Add(_extractor.GetExtensionInfo(name, directory)));

        return Task.CompletedTask;
    }

    public IEnumerable<ExtensionInfo> Extensions => _extensions;
}
