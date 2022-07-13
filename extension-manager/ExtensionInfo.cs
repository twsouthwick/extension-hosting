namespace Extension.Manager;

public record ExtensionInfo
{
    public string Name { get; init; } = null!;

    public string Path { get; init; } = null!;

    public IReadOnlyCollection<EntryPointInfo> Entrypoints { get; init; }
}

public record EntryPointInfo(string AssemblyName, string TypeName);
