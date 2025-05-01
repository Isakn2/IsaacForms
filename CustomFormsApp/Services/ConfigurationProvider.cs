namespace CustomFormsApp.Services
{
    public class ConfigurationProvider
    {
        // Change from private/protected to public
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        
        // Add other necessary methods here
    }
}
