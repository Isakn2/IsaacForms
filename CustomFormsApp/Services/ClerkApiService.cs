using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CustomFormsApp.Services;

public interface IClerkApiService
{
    Task<List<ClerkUserDto>> GetAllUsersAsync();
    Task<ClerkUserDto?> GetUserAsync(string userId);
}

public class ClerkApiService : IClerkApiService
{
    private readonly HttpClient _httpClient;
    private readonly ClerkOptions _options;
    private readonly ILogger<ClerkApiService> _logger;
    
    public ClerkApiService(
        HttpClient httpClient,
        IOptions<ClerkOptions> options,
        ILogger<ClerkApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Configure the HTTP client for Clerk API with proper URI validation
        try
        {
            // Default to Clerk's standard API URL if nothing is configured
            string baseUrl = "https://api.clerk.com/v1/";
            
            // If ApiUrl is specified in options, use it with validation
            if (!string.IsNullOrWhiteSpace(_options.ApiUrl))
            {
                // Ensure the URL has a scheme
                string apiUrl = _options.ApiUrl.Trim();
                if (!apiUrl.StartsWith("http://") && !apiUrl.StartsWith("https://"))
                {
                    apiUrl = "https://" + apiUrl;
                }
                
                // Ensure the URL ends with a trailing slash
                apiUrl = apiUrl.TrimEnd('/') + "/";
                
                // Add v1 if it doesn't already include it
                if (!apiUrl.EndsWith("v1/"))
                {
                    apiUrl += "v1/";
                }
                
                baseUrl = apiUrl;
            }
            
            _httpClient.BaseAddress = new Uri(baseUrl);
            _logger.LogInformation("Initialized Clerk API client with base address: {BaseAddress}", _httpClient.BaseAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Clerk API base URL. Using default API behavior.");
            // Allow the service to be created without a base address
            // API calls will need to use absolute URLs
        }
        
        // Set authorization header if we have a secret key
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        }
        else
        {
            _logger.LogWarning("Clerk SecretKey is not configured. API calls requiring authentication will fail.");
        }
    }
    
    // Verify a session token (JWT)
    public async Task<ClerkTokenVerificationResult?> VerifyToken(string token)
    {
        try
        {
            var response = await _httpClient.GetAsync($"tokens/verify?token={token}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ClerkTokenVerificationResult>(content);
            }
            
            _logger.LogWarning("Failed to verify token. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Clerk token");
            return null;
        }
    }
    
    // Get user details
    public async Task<ClerkUserDetails?> GetUser(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"users/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ClerkUserDetails>(content);
            }
            
            _logger.LogWarning("Failed to get user {UserId}. Status: {StatusCode}", 
                userId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Clerk user {UserId}", userId);
            return null;
        }
    }

    // Get all users from Clerk
    public async Task<List<ClerkUserDto>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("users");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Log the first part of the response for debugging
                _logger.LogDebug("Clerk API response first 500 chars: {Response}", 
                    content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                
                // Configure serializer to be more tolerant
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                try
                {
                    var clerkUsers = JsonSerializer.Deserialize<ClerkUsersResponse>(content, options);
                    
                    if (clerkUsers?.Data == null)
                    {
                        _logger.LogWarning("Clerk API returned null or empty users list");
                        return new List<ClerkUserDto>();
                    }
                    
                    // Map to our DTO format
                    return clerkUsers.Data.Select(u => new ClerkUserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.PrimaryEmail ?? "No Email",
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        ImageUrl = u.ProfileImageUrl
                    }).ToList();
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization error. Response structure may not match expected format");
                    
                    // Fallback: Try to deserialize as a direct array of users
                    try {
                        var userArray = JsonSerializer.Deserialize<List<ClerkUserDetails>>(content, options);
                        
                        if (userArray == null || !userArray.Any())
                        {
                            _logger.LogWarning("Fallback deserialization returned null or empty users list");
                            return new List<ClerkUserDto>();
                        }
                        
                        // Map to our DTO format
                        return userArray.Select(u => new ClerkUserDto
                        {
                            Id = u.Id,
                            Username = u.Username,
                            Email = u.PrimaryEmail ?? "No Email",
                            FirstName = u.FirstName ?? "",
                            LastName = u.LastName ?? "",
                            ImageUrl = u.ProfileImageUrl
                        }).ToList();
                    }
                    catch (Exception fallbackEx) {
                        _logger.LogError(fallbackEx, "Fallback deserialization also failed");
                        throw;
                    }
                }
            }
            
            _logger.LogWarning("Failed to get users from Clerk. Status: {StatusCode}", response.StatusCode);
            return new List<ClerkUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users from Clerk API");
            return new List<ClerkUserDto>();
        }
    }
    
    // Get a specific user from Clerk
    public async Task<ClerkUserDto?> GetUserAsync(string userId)
    {
        try
        {
            var clerkUser = await GetUser(userId);
            
            if (clerkUser == null)
            {
                return null;
            }
            
            return new ClerkUserDto
            {
                Id = clerkUser.Id,
                Username = clerkUser.Username,
                Email = clerkUser.PrimaryEmail ?? "No Email",
                FirstName = clerkUser.FirstName ?? "",
                LastName = clerkUser.LastName ?? "",
                ImageUrl = clerkUser.ProfileImageUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} from Clerk API", userId);
            return null;
        }
    }
}

// Classes to deserialize Clerk API responses
public class ClerkTokenVerificationResult
{
    [JsonPropertyName("jwt")]
    public ClerkJwt? Jwt { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class ClerkJwt
{
    [JsonPropertyName("claims")]
    public ClerkClaims? Claims { get; set; }
}

public class ClerkClaims
{
    [JsonPropertyName("sub")]
    public string? Subject { get; set; }
    
    [JsonPropertyName("aud")]
    public string? Audience { get; set; }
    
    [JsonPropertyName("exp")]
    public long Expiration { get; set; }
}

public class ClerkUserDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    
    [JsonPropertyName("email_addresses")]
    public List<ClerkEmailAddress>? EmailAddresses { get; set; }
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
    
    // Helper property to get primary email
    public string? PrimaryEmail => 
        EmailAddresses?.FirstOrDefault(e => e.Primary)?.EmailAddress;
}

public class ClerkEmailAddress
{
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }
    
    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
    
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}

public class ClerkUsersResponse
{
    [JsonPropertyName("data")]
    public List<ClerkUserDetails>? Data { get; set; }
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}
