using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

namespace Cg.ProjectName.Test.Application.Validators;

public class CreateDemoEmployeeValidatorTests
{
    private readonly CreateDemoEmployeeValidator _validator = new();

    // Id e Status não pertencem mais a CreateDemoEmployeeCommand: criação
    // sempre nasce Active e o Id é gerado internamente por DemoEmployee.Create
    // (ver comentário em DemoEmployeeCommandBase).
    private static CreateDemoEmployeeCommand ValidCommand() => new()
    {
        Name = "Fernando Jose",
        Document = "12345678900",
        DateHire = DateTime.UtcNow,
        DateTermination = null,
        Salary = 5000m,
        OfficeName = "Matriz"
    };

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsInvalid()
    {
        var command = ValidCommand();
        command.Name = string.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThan200Characters_ReturnsInvalid()
    {
        var command = ValidCommand();
        command.Name = new string('a', 201);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validate_WithEmptyDocument_ReturnsInvalid()
    {
        var command = ValidCommand();
        command.Document = string.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Document));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_WithSalaryNotGreaterThanZero_ReturnsInvalid(decimal salary)
    {
        var command = ValidCommand();
        command.Salary = salary;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Salary));
    }

    [Fact]
    public void Validate_WithEmptyOfficeName_ReturnsInvalid()
    {
        var command = ValidCommand();
        command.OfficeName = string.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.OfficeName));
    }
}
