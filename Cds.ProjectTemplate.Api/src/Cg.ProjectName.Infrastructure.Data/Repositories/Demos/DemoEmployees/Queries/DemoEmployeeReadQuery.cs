using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees.Queries;

public static class DemoEmployeeReadQuery
{
    private static readonly DapperMapping<DemoEmployee, Guid> Mapping = 
        DapperMapping<DemoEmployee, Guid>.Instance;

    private static readonly string employeeTable = Mapping.TableName;
    private static readonly string officeTable = DapperMapping<DemoOffice, Guid>.Instance.TableName;

    public static string GetListQuery(string whereClause, string orderByColumn, string direction)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM {employeeTable} e
            WHERE {whereClause};

            SELECT
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Document))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateHire))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateTermination))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Salary))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.RowVersion))},
                o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Name))} AS OfficeName
            FROM {employeeTable} e
            LEFT JOIN {officeTable} o
                ON o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Id))}
                 = e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))}
            WHERE {whereClause}
            ORDER BY {orderByColumn} {direction}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

        return sql;
    }

    public static string GetByIdQuery()
    {
        var sql = $"""
            SELECT
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Document))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateHire))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateTermination))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Salary))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.RowVersion))},
                o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Name))} AS OfficeName
            FROM {employeeTable} e
            LEFT JOIN {officeTable} o
                ON o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Id))}
                 = e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))}
            WHERE e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))} = @Id
              AND e.{Mapping.GetColumnName(nameof(DemoEmployee.IsDeleted))} = 0
            """;

        return sql;
    }
}
