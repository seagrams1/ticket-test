using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TicketSystem.API.Controllers;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// Instantiates the controller directly with an in-memory EF Core context
/// and a real JwtService, giving full logic coverage without requiring a running server.
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Use a real JwtService backed by in-memory configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsAtLeast256BitsLongForTesting!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiresInHours"] = "1"
            })
            .Build();

        _jwtService = new JwtService(config);
        _controller = new AuthController(_context, _jwtService);
    }

    // ─── Helper: set controller's HttpContext to simulate an authenticated user ──

    private void SetAuthenticatedUser(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ─── Register tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_ShouldReturn201WithToken()
    {
        var request = new RegisterRequest("newuser", "Password1!");

        var result = await _controller.Register(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.NotEmpty(response.Token);
        Assert.Equal("newuser", response.Username);
        Assert.Equal("Submitter", response.Role); // default role
    }

    [Fact]
    public async Task Register_DuplicateUsername_ShouldReturn409Conflict()
    {
        var request = new RegisterRequest("dupuser", "Password1!");
        await _controller.Register(request);

        var result = await _controller.Register(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WithAgentRole_ShouldAssignAgentRole()
    {
        var request = new RegisterRequest("agentuser", "Password1!", "Agent");

        var result = await _controller.Register(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Agent", response.Role);
    }

    [Fact]
    public async Task Register_WithAdminRole_ShouldAssignAdminRole()
    {
        var request = new RegisterRequest("adminuser", "Password1!", "Admin");

        var result = await _controller.Register(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public async Task Register_InvalidRole_ShouldDefaultToSubmitter()
    {
        var request = new RegisterRequest("roleuser", "Password1!", "InvalidRole");

        var result = await _controller.Register(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Submitter", response.Role);
    }

    [Fact]
    public async Task Register_ShouldReturnTokenThatIsValidJwt()
    {
        var result = await _controller.Register(new RegisterRequest("jwtuser", "Password1!"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(created.Value);

        // JWT has 3 parts separated by 2 dots
        Assert.Equal(2, response.Token.Count(c => c == '.'));
    }

    [Fact]
    public async Task Register_ShouldHashPassword()
    {
        await _controller.Register(new RegisterRequest("hashcheck", "PlainPassword1!"));

        var user = await _context.Users.FirstAsync(u => u.Username == "hashcheck");
        Assert.NotEqual("PlainPassword1!", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("PlainPassword1!", user.PasswordHash));
    }

    // ─── Login tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturn200WithToken()
    {
        await _controller.Register(new RegisterRequest("loginuser", "Password1!"));

        var result = await _controller.Login(new LoginRequest("loginuser", "Password1!"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.NotEmpty(response.Token);
        Assert.Equal("loginuser", response.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_ShouldReturn401Unauthorized()
    {
        await _controller.Register(new RegisterRequest("badpwuser", "Password1!"));

        var result = await _controller.Login(new LoginRequest("badpwuser", "WrongPassword!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_NonExistentUser_ShouldReturn401Unauthorized()
    {
        var result = await _controller.Login(new LoginRequest("doesnotexist", "Password1!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ShouldReturnUserIdInResponse()
    {
        await _controller.Register(new RegisterRequest("idcheckuser", "Password1!"));

        var result = await _controller.Login(new LoginRequest("idcheckuser", "Password1!"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.True(response.UserId > 0);
    }

    // ─── Change password tests ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_ShouldReturn200()
    {
        var registerResult = await _controller.Register(new RegisterRequest("chgpwuser", "OldPass1!"));
        var created = Assert.IsType<CreatedAtActionResult>(registerResult.Result);
        var auth = Assert.IsType<AuthResponse>(created.Value);
        SetAuthenticatedUser(auth.UserId);

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("OldPass1!", "NewPass1!"));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ShouldReturn400()
    {
        var registerResult = await _controller.Register(new RegisterRequest("chgpwfail", "OldPass1!"));
        var created = Assert.IsType<CreatedAtActionResult>(registerResult.Result);
        var auth = Assert.IsType<AuthResponse>(created.Value);
        SetAuthenticatedUser(auth.UserId);

        var result = await _controller.ChangePassword(
            new ChangePasswordRequest("WrongPassword!", "NewPass1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ShouldActuallyUpdatePasswordHash()
    {
        var registerResult = await _controller.Register(new RegisterRequest("chgpwhash", "OldPass1!"));
        var created = Assert.IsType<CreatedAtActionResult>(registerResult.Result);
        var auth = Assert.IsType<AuthResponse>(created.Value);
        SetAuthenticatedUser(auth.UserId);

        await _controller.ChangePassword(new ChangePasswordRequest("OldPass1!", "NewPass2!"));

        var user = await _context.Users.FindAsync(auth.UserId);
        Assert.NotNull(user);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass2!", user.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("OldPass1!", user.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_NewPasswordWorksForSubsequentLogin()
    {
        var registerResult = await _controller.Register(new RegisterRequest("chgpwlogin", "OldPass1!"));
        var created = Assert.IsType<CreatedAtActionResult>(registerResult.Result);
        var auth = Assert.IsType<AuthResponse>(created.Value);
        SetAuthenticatedUser(auth.UserId);

        await _controller.ChangePassword(new ChangePasswordRequest("OldPass1!", "NewPass2!"));

        // Old password should fail
        var oldLoginResult = await _controller.Login(new LoginRequest("chgpwlogin", "OldPass1!"));
        Assert.IsType<UnauthorizedObjectResult>(oldLoginResult.Result);

        // New password should succeed
        var newLoginResult = await _controller.Login(new LoginRequest("chgpwlogin", "NewPass2!"));
        var ok = Assert.IsType<OkObjectResult>(newLoginResult.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.NotEmpty(response.Token);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
