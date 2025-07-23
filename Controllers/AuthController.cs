namespace DotNetApiStarterKit.Controllers
{
    using DotNetApiStarterKit.Models;
    using DotNetApiStarterKit.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly ILogger<AuthController> logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            this.userService = userService;
            this.logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="loginRequest">User credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginRequest.Username))
                {
                    return this.BadRequest("Username is required");
                }

                if (string.IsNullOrWhiteSpace(loginRequest.Password))
                {
                    return this.BadRequest("Password is required");
                }

                var loginResponse = await this.userService.AuthenticateAsync(loginRequest);

                if (loginResponse == null)
                {
                    return this.Unauthorized("Invalid username or password");
                }

                this.logger.LogInformation("User {Username} logged in successfully", loginRequest.Username);
                return this.Ok(loginResponse);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during login for username: {Username}", loginRequest.Username);
                return this.StatusCode(500, "An error occurred during authentication");
            }
        }

        /// <summary>
        /// Register a new user (for testing purposes)
        /// </summary>
        /// <param name="registerRequest">User registration data</param>
        /// <returns>Success message</returns>
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(registerRequest.Username))
                {
                    return this.BadRequest("Username is required");
                }

                if (string.IsNullOrWhiteSpace(registerRequest.Password))
                {
                    return this.BadRequest("Password is required");
                }

                if (string.IsNullOrWhiteSpace(registerRequest.Email))
                {
                    return this.BadRequest("Email is required");
                }

                if (registerRequest.Password.Length < 6)
                {
                    return this.BadRequest("Password must be at least 6 characters long");
                }

                // Check if user already exists
                var existingUser = await this.userService.GetUserByUsernameAsync(registerRequest.Username);
                if (existingUser != null)
                {
                    return this.Conflict("Username already exists");
                }

                await this.userService.CreateUserAsync(registerRequest.Username, registerRequest.Password, registerRequest.Email);

                this.logger.LogInformation("New user registered: {Username}", registerRequest.Username);
                return this.Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during user registration for username: {Username}", registerRequest.Username);
                return this.StatusCode(500, "An error occurred during registration");
            }
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="tokenRequest">Token to validate</param>
        /// <returns>Token validation result</returns>
        [HttpPost("validate")]
        public async Task<ActionResult> ValidateToken([FromBody] TokenValidationRequest tokenRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenRequest.Token))
                {
                    return this.BadRequest("Token is required");
                }

                var isValid = await this.userService.ValidateTokenAsync(tokenRequest.Token);

                if (isValid)
                {
                    return this.Ok(new { valid = true, message = "Token is valid" });
                }
                else
                {
                    return this.Unauthorized(new { valid = false, message = "Token is invalid or expired" });
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during token validation");
                return this.StatusCode(500, "An error occurred during token validation");
            }
        }

        /// <summary>
        /// Get current user information (requires authentication)
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult> GetCurrentUser()
        {
            try
            {
                var username = this.User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return this.Unauthorized("Invalid token");
                }

                var user = await this.userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return this.NotFound("User not found");
                }

                // Return user info without sensitive data
                var userInfo = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.CreatedAt,
                    user.LastLoginAt,
                    user.IsActive,
                };

                return this.Ok(userInfo);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving current user information");
                return this.StatusCode(500, "An error occurred while retrieving user information");
            }
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
