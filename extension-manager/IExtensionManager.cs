namespace Extension.Manager;

public interface IExtensionManager
{
    IEnumerable<ExtensionInfo> Extensions { get; }

    Task Add(string name, string path);
}

