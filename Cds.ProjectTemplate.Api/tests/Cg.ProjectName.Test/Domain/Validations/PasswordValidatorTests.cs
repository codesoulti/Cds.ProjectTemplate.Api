using Cg.ProjectName.Domain.Validations;

namespace Cg.ProjectName.Test.Domain.Validations;

public class PasswordValidatorTests
{
    private readonly PasswordValidator _validator = new();

    [Theory]
    [InlineData("Abcdef1!")]
    [InlineData("C0mplex@Pass")]
    [InlineData("Str0ng#Password123")]
    public void Validate_WithStrongPassword_ReturnsValid(string password)
    {
        var result = _validator.Validate(password);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPassword_ReturnsInvalid()
    {
        var result = _validator.Validate(string.Empty);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShorterThanEightCharacters_ReturnsInvalid()
    {
        var result = _validator.Validate("Ab1!");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_LongerThan128Characters_ReturnsInvalid()
    {
        var tooLong = "Ab1!" + new string('a', 128);

        var result = _validator.Validate(tooLong);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutUppercaseLetter_ReturnsInvalid()
    {
        var result = _validator.Validate("abcdefg1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public void Validate_WithoutLowercaseLetter_ReturnsInvalid()
    {
        var result = _validator.Validate("ABCDEFG1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public void Validate_WithoutDigit_ReturnsInvalid()
    {
        var result = _validator.Validate("Abcdefgh!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("number"));
    }

    [Fact]
    public void Validate_WithoutSpecialCharacter_ReturnsInvalid()
    {
        var result = _validator.Validate("Abcdefg1");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("special character"));
    }
}
