using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsAtLeast256BitsLongForTesting!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiresInHours"] = "1"
            })
            .Build();

        _service = new JwtService(config);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = UserRole.Agent,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        Assert.NotEmpty(token);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Username = "claimtester",
            Role = UserRole.Admin,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert — claim types may vary by IdentityModel version; check by value
        Assert.Contains(jwt.Claims, c => c.Value == "claimtester");
        Assert.Contains(jwt.Claims, c => c.Value == "Admin");
    }
}
