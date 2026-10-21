using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

namespace Cg.ProjectName.Test.Infrastructure.Dapper;

public class DapperMappingTests
{
    [Fact]
    public void TableName_IsSchemaQualified()
    {
        Assert.Equal("[Demo].[DemoEmployees]", DapperMapping<DemoEmployee, Guid>.Instance.TableName);
        Assert.Equal("[Demo].[DemoOffices]", DapperMapping<DemoOffice, Guid>.Instance.TableName);
    }

    [Fact]
    public void GetColumn_ForScalarProperty_IsMapped()
    {
        var column = DapperMapping<DemoEmployee, Guid>.Instance.GetColumn(nameof(DemoEmployee.OfficeId));

        Assert.NotNull(column);
        Assert.Equal("[OfficeId]", column!.QuotedColumnName);
    }

    [Fact]
    public void GetColumn_ForNavigationProperty_IsNotMapped()
    {
        // DemoEmployee.Office (referência a outra entidade) e
        // DemoOffice.Employees (coleção) não são colunas físicas — antes
        // do fix, essas propriedades entravam no mapeamento como se
        // fossem, gerando SQL inválido no dia em que alguém tentasse
        // projetar ou filtrar por elas.
        var officeColumn = DapperMapping<DemoEmployee, Guid>.Instance.GetColumn(nameof(DemoEmployee.Office));
        var employeesColumn = DapperMapping<DemoOffice, Guid>.Instance.GetColumn(nameof(DemoOffice.Employees));

        Assert.Null(officeColumn);
        Assert.Null(employeesColumn);
    }

    [Fact]
    public void Key_ResolvesToId()
    {
        Assert.Equal("[Id]", DapperMapping<DemoEmployee, Guid>.Instance.Key.QuotedColumnName);
        Assert.Equal("[Id]", DapperMapping<DemoOffice, Guid>.Instance.Key.QuotedColumnName);
    }
}
