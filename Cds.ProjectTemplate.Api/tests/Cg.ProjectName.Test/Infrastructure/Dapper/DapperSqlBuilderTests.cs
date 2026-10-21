using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper.Queries;

namespace Cg.ProjectName.Test.Infrastructure.Dapper;

/// <summary>
/// Testa apenas a GERAÇÃO do SQL (texto + parâmetros) de
/// <see cref="DapperSqlBuilder"/> — nunca abre uma conexão real, por isso
/// não precisa de banco de dados. É exatamente essa camada que ficou sem
/// nenhum teste antes e escondeu, por meses, o bug de GetByIdAsync sempre
/// lançar NotSupportedException.
/// </summary>
public class DapperSqlBuilderTests
{
    [Fact]
    public void BuildById_IncludesKeyAndSoftDeleteFilter()
    {
        var id = Guid.NewGuid();

        var result = DapperSqlBuilder.BuildById<DemoEmployee, Guid>(id);

        Assert.Contains("[Id] = @Id", result.Sql);
        Assert.Contains("IsDeleted = 0", result.Sql);
        Assert.Equal(id, result.Parameters.Get<Guid>("Id"));
    }

    [Fact]
    public void BuildById_WithoutColumns_SelectsAllColumns()
    {
        var result = DapperSqlBuilder.BuildById<DemoEmployee, Guid>(Guid.NewGuid());

        Assert.Contains("SELECT *", result.Sql);
    }

    [Fact]
    public void BuildById_WithColumns_SelectsOnlyRequestedColumns()
    {
        var result = DapperSqlBuilder.BuildById<DemoEmployee, Guid>(
            Guid.NewGuid(),
            x => new { x.Name, x.Document });

        Assert.Contains("[Name]", result.Sql);
        Assert.Contains("[Document]", result.Sql);
        Assert.DoesNotContain("SELECT *", result.Sql);
    }

    [Fact]
    public void Build_WithClosureCapturedVariable_ResolvesValueWithoutError()
    {
        // Este é o caso comum que antes exigia Expression.Compile() a cada
        // chamada: "officeId" é uma variável capturada por closure, não uma
        // ConstantExpression pura. O teste garante que o valor continua
        // sendo corretamente extraído e bindado como parâmetro.
        var officeId = Guid.NewGuid();

        var result = DapperSqlBuilder.Build<DemoEmployee, Guid>(
            new DapperQueryOptions<DemoEmployee>
            {
                Where = x => x.OfficeId == officeId
            });

        Assert.Contains("[OfficeId] =", result.Sql);

        var parameterName = result.Parameters.ParameterNames.Single();

        Assert.Equal(officeId, result.Parameters.Get<Guid>(parameterName));
    }

    [Fact]
    public void Build_WithMultipleClosureVariablesAndAndAlso_ResolvesAllValues()
    {
        var officeId = Guid.NewGuid();
        var status = EStatus.Active;

        var result = DapperSqlBuilder.Build<DemoEmployee, Guid>(
            new DapperQueryOptions<DemoEmployee>
            {
                Where = x => x.OfficeId == officeId && x.Status == status
            });

        Assert.Contains("AND", result.Sql);
        Assert.Equal(2, result.Parameters.ParameterNames.Count());
    }

    [Fact]
    public void Build_WithContains_EscapesLikeWildcardsInValue()
    {
        // "50%" contém um caractere curinga do LIKE. Sem escapar, isso
        // alteraria o significado da busca (viraria "começa com 50,
        // qualquer coisa" em vez de "contém literalmente 50%").
        const string searchValue = "50%_off";

        var result = DapperSqlBuilder.Build<DemoEmployee, Guid>(
            new DapperQueryOptions<DemoEmployee>
            {
                Where = x => x.Name.Contains(searchValue)
            });

        Assert.Contains("LIKE", result.Sql);
        Assert.Contains("ESCAPE '\\'", result.Sql);

        var parameterName = result.Parameters.ParameterNames.Single();
        var boundValue = result.Parameters.Get<string>(parameterName);

        Assert.Equal("50\\%\\_off", boundValue);
    }

    [Fact]
    public void EscapeLikeValue_EscapesAllWildcardsAndBackslash()
    {
        // Entrada literal: 100%_[a]\b (o "]" de fechamento não precisa de
        // escape em T-SQL — só "%", "_", "[" e o próprio "\" precisam).
        var escaped = DapperSqlBuilder.EscapeLikeValue("100%_[a]\\b");

        Assert.Equal("100\\%\\_\\[a]\\\\b", escaped);
    }
}
