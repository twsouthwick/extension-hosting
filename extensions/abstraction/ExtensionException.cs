namespace Extension;

public class ExtensionException : Exception
{
    public ExtensionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
