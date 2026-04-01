using Ccx.Api;
using FluentAssertions;

namespace Ccx.Api.Tests;

public class OAuthResolverTests
{
    [Fact]
    public void Resolve_ExplicitKey_ReturnsApiKey()
    {
        var result = OAuthResolver.Resolve("sk-ant-test-key");

        result.Token.Should().Be("sk-ant-test-key");
        result.IsOAuth.Should().BeFalse();
        result.DisplayName.Should().Be("API Key");
    }

    [Fact]
    public void ParseOAuthToken_NestedFormat_ReturnsOAuthResult()
    {
        var json = """{"claudeAiOAuth":{"accessToken":"oauth-token-123","refreshToken":"rt","expiresAt":"2099-01-01"}}""";

        var result = OAuthResolver.ParseOAuthToken(json, "test");

        result.Should().NotBeNull();
        result!.Token.Should().Be("oauth-token-123");
        result.IsOAuth.Should().BeTrue();
        result.DisplayName.Should().Be("test");
    }

    [Fact]
    public void ParseOAuthToken_FlatFormat_ReturnsOAuthResult()
    {
        var json = """{"accessToken":"flat-token-456"}""";

        var result = OAuthResolver.ParseOAuthToken(json, "flat source");

        result.Should().NotBeNull();
        result!.Token.Should().Be("flat-token-456");
        result.IsOAuth.Should().BeTrue();
        result.DisplayName.Should().Be("flat source");
    }

    [Fact]
    public void ParseOAuthToken_EmptyToken_ReturnsNull()
    {
        var json = """{"claudeAiOAuth":{"accessToken":""}}""";

        var result = OAuthResolver.ParseOAuthToken(json, "test");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseOAuthToken_NoToken_ReturnsNull()
    {
        var json = """{"someOtherField":"value"}""";

        var result = OAuthResolver.ParseOAuthToken(json, "test");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseOAuthToken_InvalidJson_ReturnsNull()
    {
        var result = OAuthResolver.ParseOAuthToken("not json", "test");

        result.Should().BeNull();
    }
}
