namespace CustomFormsApp.Services
{
    public class ClerkOptions
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string JwtKey { get; set; } = string.Empty; // Added missing property
        public string ApiUrl { get; set; } = "https://api.clerk.com";
        public string ApplicationId { get; set; } = "app_2vYy9SnEeXVg2cmESjWm538hpQR";
        public string InstanceId { get; set; } = "ins_2vYy9Yyfablcap2xDWVGfkQg9cH";
        public string FrontendApi { get; set; } = "https://meet-phoenix-78.clerk.accounts.dev";
        public string JwksUrl { get; set; } = "https://meet-phoenix-78.clerk.accounts.dev/.well-known/jwks.json";
    }
}