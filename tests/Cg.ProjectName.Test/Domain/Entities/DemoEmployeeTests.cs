using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Test.Domain.Entities;

public class DemoEmployeeTests
{
    [Fact]
    public void Create_SetsExpectedFieldsAndDefaultsToActive()
    {
        var officeId = Guid.NewGuid();
        var dateHire = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var employee = DemoEmployee.Create("Fernando Jose", "12345678900", dateHire, 5000m, officeId);

        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.Equal("Fernando Jose", employee.Name);
        Assert.Equal("12345678900", employee.Document);
        Assert.Equal(dateHire, employee.DateHire);
        Assert.Equal(5000m, employee.Salary);
        Assert.Equal(officeId, employee.OfficeId);
        Assert.Equal(EStatus.Active, employee.Status);
    }

    [Fact]
    public void Constructor_SetsCreatedAtInUtc()
    {
        var before = DateTime.UtcNow;

        var employee = DemoEmployee.Create("Name", "Doc", DateTime.UtcNow, 1000m, Guid.NewGuid());

        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, employee.CreatedAt.Kind);
        Assert.InRange(employee.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Change_UpdatesFieldsAndUpdatedAt_ButNotStatus()
    {
        var employee = DemoEmployee.Create("Old Name", "Old Doc", DateTime.UtcNow, 1000m, Guid.NewGuid());
        employee.Activate();
        var newOfficeId = Guid.NewGuid();
        var newDateHire = DateTime.UtcNow.AddDays(-30);
        var newDateTermination = DateTime.UtcNow;

        employee.Change("New Name", "New Doc", newDateHire, newDateTermination, 2000m, newOfficeId);

        Assert.Equal("New Name", employee.Name);
        Assert.Equal("New Doc", employee.Document);
        Assert.Equal(newDateHire, employee.DateHire);
        Assert.Equal(newDateTermination, employee.DateTermination);
        Assert.Equal(2000m, employee.Salary);
        Assert.Equal(newOfficeId, employee.OfficeId);
        Assert.NotNull(employee.UpdatedAt);
        // Change() não decide o Status — isso é responsabilidade de Activate/Inactivate.
        Assert.Equal(EStatus.Active, employee.Status);
    }

    [Fact]
    public void Activate_SetsStatusActiveAndUpdatesTimestamp()
    {
        var employee = DemoEmployee.Create("Name", "Doc", DateTime.UtcNow, 1000m, Guid.NewGuid());
        employee.Inactivate();

        employee.Activate();

        Assert.Equal(EStatus.Active, employee.Status);
        Assert.NotNull(employee.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, employee.UpdatedAt!.Value.Kind);
    }

    [Fact]
    public void Inactivate_SetsStatusInactiveAndUpdatesTimestamp()
    {
        var employee = DemoEmployee.Create("Name", "Doc", DateTime.UtcNow, 1000m, Guid.NewGuid());

        employee.Inactivate();

        Assert.Equal(EStatus.Inactive, employee.Status);
        Assert.NotNull(employee.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, employee.UpdatedAt!.Value.Kind);
    }
}
