using Extension;

namespace Extension1;

public class Extension1Entry : IEntryPoint
{
    public Task RunAsync(CancellationToken token)
        => Task.Delay(TimeSpan.FromSeconds(2), token);
}
