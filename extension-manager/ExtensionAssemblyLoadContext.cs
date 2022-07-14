using System.Reflection;
using System.Runtime.Loader;

namespace Extension.Manager;

internal class ExtensionAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ExtensionAssemblyLoadContext(string path)
        : base(Path.GetFileNameWithoutExtension(path), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(path);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_resolver.ResolveAssemblyToPath(assemblyName) is { } path)
        {
            return LoadFromAssemblyPath(path);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path)
        {
            return LoadUnmanagedDllFromPath(path);
        }

        return IntPtr.Zero;
    }
}
