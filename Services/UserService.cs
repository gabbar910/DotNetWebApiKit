namespace DotNetApiStarterKit.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Json;
    using BCrypt.Net;
    using DotNetApiStarterKit.Models;
    using Microsoft.IdentityModel.Tokens;

    public interface IUserService
    {
        Task<LoginResponse?> AuthenticateAsync(LoginRequest loginRequest);

        Task<UserCredential?> GetUserByUsernameAsync(string username);

        Task<UserCredential> CreateUserAsync(string username, string password, string email);

        Task<bool> ValidateTokenAsync(string token);
    }

    public class UserService : IUserService
    {
        private readonly string dataFilePath;
        private readonly ILogger<UserService> logger;
        private readonly IConfiguration configuration;

        public UserService(IWebHostEnvironment environment, ILogger<UserService> logger, IConfiguration configuration)
        {
            this.dataFilePath = Path.Combine(environment.ContentRootPath, "data", "usercreds.json");
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<LoginResponse?> AuthenticateAsync(LoginRequest loginRequest)
        {
            try
            {
                var user = await this.GetUserByUsernameAsync(loginRequest.Username);

                if (user == null || !user.IsActive)
                {
                    this.logger.LogWarning("Authentication failed for username: {Username} - User not found or inactive", loginRequest.Username);
                    return null;
                }

                if (!BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    this.logger.LogWarning("Authentication failed for username: {Username} - Invalid password", loginRequest.Username);
                    return null;
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await this.UpdateUserLastLoginAsync(user);

                // Generate JWT token
                var token = this.GenerateJwtToken(user);
                var expiryMinutes = this.configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
                var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

                this.logger.LogInformation("User {Username} authenticated successfully", loginRequest.Username);

                return new LoginResponse
                {
                    Token = token,
                    Expires = expires,
                    Username = user.Username,
                };
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during authentication for username: {Username}", loginRequest.Username);
                return null;
            }
        }

        public async Task<UserCredential?> GetUserByUsernameAsync(string username)
        {
            var users = await this.LoadUsersAsync();
            return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<UserCredential> CreateUserAsync(string username, string password, string email)
        {
            var users = await this.LoadUsersAsync();

            // Check if user already exists
            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"User with username '{username}' already exists");
            }

            // Generate new ID
            var maxId = users.Any() ? users.Max(u => u.UserId) : 0;
            var newUser = new UserCredential
            {
                UserId = maxId + 1,
                Username = username,
                PasswordHash = BCrypt.HashPassword(password),
                Email = email,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.MinValue,
                IsActive = true,
            };

            users.Add(newUser);
            await this.SaveUsersAsync(users);

            this.logger.LogInformation("Created new user with username: {Username}", username);
            return newUser;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(this.configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

                    tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = this.configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = this.configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                    },
                    out SecurityToken validatedToken);

                    return true;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Token validation failed");
                    return false;
                }
            });
        }

        private string GenerateJwtToken(UserCredential user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(this.configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
            var expiryMinutes = this.configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("username", user.Username),
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = this.configuration["Jwt:Issuer"],
                Audience = this.configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<List<UserCredential>> LoadUsersAsync()
        {
            try
            {
                if (!File.Exists(this.dataFilePath))
                {
                    this.logger.LogInformation("User credentials file not found, creating empty list");
                    return new List<UserCredential>();
                }

                var jsonContent = await File.ReadAllTextAsync(this.dataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var users = JsonSerializer.Deserialize<List<UserCredential>>(jsonContent, options) ?? new List<UserCredential>();
                this.logger.LogInformation("Loaded {Count} users from credentials file", users.Count);

                return users;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error loading user credentials from file: {FilePath}", this.dataFilePath);
                return new List<UserCredential>();
            }
        }

        private async Task SaveUsersAsync(List<UserCredential> users)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true,
                };

                var jsonContent = JsonSerializer.Serialize(users, options);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(this.dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(this.dataFilePath, jsonContent);
                this.logger.LogInformation("Saved {Count} users to credentials file", users.Count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error saving user credentials to file: {FilePath}", this.dataFilePath);
                throw;
            }
        }

        private async Task UpdateUserLastLoginAsync(UserCredential user)
        {
            try
            {
                var users = await this.LoadUsersAsync();
                var existingUser = users.FirstOrDefault(u => u.UserId == user.UserId);

                if (existingUser != null)
                {
                    existingUser.LastLoginAt = user.LastLoginAt;
                    await this.SaveUsersAsync(users);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating last login time for user: {Username}", user.Username);

                // Don't throw here as this is not critical for authentication
            }
        }
    }
}
