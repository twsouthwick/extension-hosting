namespace CustomerEndpointDefinition
{
    [Service("test")]
    [Route("here")]
    public class SomeService
    {
        [Route("method")]
        [HttpGet]
        public int Method()
        {
            return 42;
        }

        [Route("method2")]
        [HttpPost]
        public Task<int> Method2()
            => Task.FromResult(43);
    }
}