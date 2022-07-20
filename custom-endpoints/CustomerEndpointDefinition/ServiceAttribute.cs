namespace CustomerEndpointDefinition
{
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}