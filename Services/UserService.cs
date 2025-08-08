namespace DotNetApiStarterKit.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using BCrypt.Net;
    using DotNetApiStarterKit.Data;
    using DotNetApiStarterKit.Models;
    using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext context;
        private readonly ILogger<UserService> logger;
        private readonly IConfiguration configuration;

        public UserService(AppDbContext context, ILogger<UserService> logger, IConfiguration configuration)
        {
            this.context = context;
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
            try
            {
                return await this.context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                return null;
            }
        }

        public async Task<UserCredential> CreateUserAsync(string username, string password, string email)
        {
            try
            {
                // Check if user already exists
                var existingUser = await this.GetUserByUsernameAsync(username);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"User with username '{username}' already exists");
                }

                // Check if email already exists
                var existingEmail = await this.context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (existingEmail != null)
                {
                    throw new InvalidOperationException($"User with email '{email}' already exists");
                }

                var newUser = new UserCredential
                {
                    Username = username,
                    PasswordHash = BCrypt.HashPassword(password),
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.MinValue,
                    IsActive = true,
                };

                this.context.Users.Add(newUser);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Created new user with username: {Username}", username);
                return newUser;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating user: {Username}", username);
                throw;
            }
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

        private async Task UpdateUserLastLoginAsync(UserCredential user)
        {
            try
            {
                this.context.Users.Update(user);
                await this.context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating last login time for user: {Username}", user.Username);

                // Don't throw here as this is not critical for authentication
            }
        }
    }
}
