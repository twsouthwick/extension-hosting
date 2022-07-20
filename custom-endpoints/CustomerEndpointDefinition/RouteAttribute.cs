namespace CustomerEndpointDefinition
{
    public class RouteAttribute : Attribute
    {
        public RouteAttribute(string route)
        {
            Path = route;
        }

        public string Path { get; }
    }
}