namespace Extension.Manager;

public class ExtensionInfo
{
    public string Name { get; init; } = null!;

    public string Path { get; init; } = null!;

    public string Entrypoint { get; init; } = null!;
}
