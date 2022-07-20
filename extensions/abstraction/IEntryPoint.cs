namespace Extension;

public interface IEntryPoint
{
    Task RunAsync(Context context, CancellationToken token);
}
