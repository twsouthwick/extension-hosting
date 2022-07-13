namespace Extension.Manager;

public interface IExtensionManager
{
    IAsyncEnumerable<ExtensionInfo> GetExtensionsAsync(CancellationToken token);

    Task Add(string name, string path);
}

