using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(ILogger<AuthController> logger, AppDbContext context, ITokenService tokenService)
    {
        _logger = logger;
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Username and password are required" });
            }

            // Check for demo user (hardcoded for demo purposes)
            if (request.Username == "demo-user" && request.Password == "password")
            {
                var demoToken = _tokenService.GenerateToken("demo-123", "demo-user");
                _logger.LogInformation("Demo user logged in");
                
                return Ok(new AuthResponse
                {
                    Token = demoToken,
                    UserId = "demo-123",
                    Username = "demo-user",
                    Email = "demo@shopassistant.com",
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });
            }

            // Find user in database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {Username}", request.Username);
                await Task.Delay(Random.Shared.Next(100, 500)); // Prevent timing attacks
                return Unauthorized(new { error = "Invalid username or password" });
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                await Task.Delay(Random.Shared.Next(100, 500)); // Prevent timing attacks
                return Unauthorized(new { error = "Invalid username or password" });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate token
            var token = _tokenService.GenerateToken(user.Id.ToString(), user.Username);

            _logger.LogInformation("User logged in successfully: {Username}", request.Username);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { error = "Login failed", message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Username, email, and password are required" });
            }

            // Validate username format
            if (request.Username.Length < 3 || request.Username.Length > 50)
            {
                return BadRequest(new { error = "Username must be between 3 and 50 characters" });
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { error = "Invalid email format" });
            }

            // Validate password strength
            if (request.Password.Length < 6)
            {
                return BadRequest(new { error = "Password must be at least 6 characters long" });
            }

            // Check if username already exists
            var existingUsername = await _context.Users
                .AnyAsync(u => u.Username == request.Username);

            if (existingUsername)
            {
                return BadRequest(new { error = "Username already exists" });
            }

            // Check if email already exists
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email == request.Email);

            if (existingEmail)
            {
                return BadRequest(new { error = "Email already registered" });
            }

            // Hash password
            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token
            var token = _tokenService.GenerateToken(user.Id.ToString(), user.Username);

            _logger.LogInformation("User registered successfully: {Username}", request.Username);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return StatusCode(500, new { error = "Registration failed", message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            // In a real system, you'd invalidate the token here
            // For now, just log the logout
            _logger.LogInformation("User logged out: {UserId}", request.UserId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { error = "Logout failed", message = ex.Message });
        }
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Invalid token", isValid = false });
            }

            // Validate token using TokenService
            if (_tokenService.ValidateToken(token, out var userId, out var username))
            {
                _logger.LogInformation("Token validated successfully for user: {Username}", username);
                return Ok(new { isValid = true, message = "Token is valid", userId, username });
            }
            else
            {
                _logger.LogWarning("Token validation failed");
                return Unauthorized(new { error = "Invalid or expired token", isValid = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation error");
            return Unauthorized(new { error = "Token validation failed", isValid = false });
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// DTOs
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}


