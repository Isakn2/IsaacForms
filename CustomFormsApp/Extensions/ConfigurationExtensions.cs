// ConfigurationExtensions.cs
using Microsoft.Extensions.Configuration;

namespace CustomFormsApp.Extensions
{
    public static class ConfigurationExtensions
    {
        public static void ProcessEnvironmentVariables(this IConfiguration configuration)
        {
            foreach (var (key, value) in configuration.AsEnumerable())
            {
                if (value != null && value.StartsWith("#{") && value.EndsWith("}#"))
                {
                    var envVarName = value[2..^2];
                    var envValue = Environment.GetEnvironmentVariable(envVarName);
                    if (!string.IsNullOrEmpty(envValue))
                    {
                        configuration[key] = envValue;
                    }
                }
            }
        }
    }
}