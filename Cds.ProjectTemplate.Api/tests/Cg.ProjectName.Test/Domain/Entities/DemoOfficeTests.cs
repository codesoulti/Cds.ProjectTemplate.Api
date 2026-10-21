using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Test.Domain.Entities;

public class DemoOfficeTests
{
    [Fact]
    public void Create_SetsExpectedFieldsAndDefaultsToActive()
    {
        var office = DemoOffice.Create("Matriz");

        Assert.NotEqual(Guid.Empty, office.Id);
        Assert.Equal("Matriz", office.Name);
        Assert.Equal(EStatus.Active, office.Status);
    }

    [Fact]
    public void Constructor_SetsCreatedAtInUtc()
    {
        var before = DateTime.UtcNow;

        var office = DemoOffice.Create("Filial");

        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, office.CreatedAt.Kind);
        Assert.InRange(office.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Inactivate_SetsStatusInactiveAndUpdatesTimestamp()
    {
        var office = DemoOffice.Create("Filial");

        office.Inactivate();

        Assert.Equal(EStatus.Inactive, office.Status);
        Assert.NotNull(office.UpdatedAt);
    }

    [Fact]
    public void Activate_SetsStatusActiveAndUpdatesTimestamp()
    {
        var office = DemoOffice.Create("Filial");
        office.Inactivate();

        office.Activate();

        Assert.Equal(EStatus.Active, office.Status);
        Assert.NotNull(office.UpdatedAt);
    }
}
