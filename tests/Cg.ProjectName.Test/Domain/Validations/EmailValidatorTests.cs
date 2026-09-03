using Cg.ProjectName.Domain.Validations;

namespace Cg.ProjectName.Test.Domain.Validations;

public class EmailValidatorTests
{
    private readonly EmailValidator _validator = new();

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.com.br")]
    [InlineData("user+tag@example.co")]
    public void Validate_WithValidEmail_ReturnsValid(string email)
    {
        var result = _validator.Validate(email);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("user@no-tld")]
    public void Validate_WithInvalidEmail_ReturnsInvalid(string email)
    {
        var result = _validator.Validate(email);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmailLongerThan100Characters_ReturnsInvalid()
    {
        var longLocalPart = new string('a', 95);
        var email = $"{longLocalPart}@a.com";

        var result = _validator.Validate(email);

        Assert.False(result.IsValid);
    }
}
