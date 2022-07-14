namespace Extension;

public interface IEntryPoint
{
    Task RunAsync(CancellationToken token);
}
