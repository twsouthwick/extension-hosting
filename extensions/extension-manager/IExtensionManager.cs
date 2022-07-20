namespace Extension.Manager;

public interface IExtensionManager
{
    IEnumerable<ExtensionInstance> Extensions { get; }

    Task<ExtensionInstance> AddAsync(string path);

    Task<ExtensionInstance?> DeleteAsync(string path);

    Task<IEnumerable<string>> RunAsync(CancellationToken token);
}

