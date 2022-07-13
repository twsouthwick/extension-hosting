using System.Reflection;

namespace Extension.Manager;

internal class ExtensionInfoExtractor
{
    public ExtensionInfo GetExtensionInfo(string name, string directory)
    {
        var assemblies = Directory.EnumerateFiles(directory, "*.dll").ToList();
        var extensions = new List<EntryPointInfo>();
        using var loadContext = new MetadataLoadContext(new PathAssemblyResolver(assemblies));

        loadContext.LoadFromAssemblyPath(typeof(IEntryPoint).Assembly.Location);

        foreach (var assemblyName in assemblies)
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyName);

            extensions.AddRange(FilterTypes(assembly));
        }

        return new()
        {
            Name = name,
            Path = directory,
            Entrypoints = extensions,
        };
    }

    private static IEnumerable<EntryPointInfo> FilterTypes(Assembly assembly)
        => assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsPublic)
            .Where(t => typeof(IEntryPoint).IsAssignableFrom(t))
            .Where(t => !string.IsNullOrEmpty(t.Assembly.FullName))
            .Where(t => !string.IsNullOrEmpty(t.FullName))
            .Select(t => new EntryPointInfo(t.Assembly.FullName!, t.FullName!));
}
