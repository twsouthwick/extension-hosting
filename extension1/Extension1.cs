using Extension;

namespace Extension1;

public class Extension1Entry : IEntryPoint
{
    public Task RunAsync(Context context, CancellationToken token)
    {
        context.Add("Hello from extension1");

        return Task.CompletedTask;
    }
}
