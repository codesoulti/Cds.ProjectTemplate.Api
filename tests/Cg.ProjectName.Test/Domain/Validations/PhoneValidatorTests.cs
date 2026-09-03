using Cg.ProjectName.Domain.Validations;

namespace Cg.ProjectName.Test.Domain.Validations;

public class PhoneValidatorTests
{
    private readonly PhoneValidator _validator = new();

    [Theory]
    [InlineData("+5511987654321")]
    [InlineData("5511987654321")]
    [InlineData("+12025550123")]
    public void Validate_WithValidE164Phone_ReturnsValid(string phone)
    {
        var result = _validator.Validate(phone);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789")]
    [InlineData("abc12345678")]
    [InlineData("+")]
    [InlineData("123456789012345678")]
    public void Validate_WithInvalidPhone_ReturnsInvalid(string phone)
    {
        var result = _validator.Validate(phone);

        Assert.False(result.IsValid);
    }
}
