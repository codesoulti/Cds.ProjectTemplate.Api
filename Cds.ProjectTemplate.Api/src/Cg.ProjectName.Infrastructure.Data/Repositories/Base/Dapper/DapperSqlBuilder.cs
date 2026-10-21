using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Enums.Dapper;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper.Sqls;
using Dapper;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper.Queries
{
    public sealed class DapperSqlResult
    {
        public required string Sql { get; init; }

        public DynamicParameters Parameters { get; init; } = new();
    }

    public static class DapperSqlBuilder
    {
        public static DapperSqlResult Build<TEntity, TKey>(
            DapperQueryOptions<TEntity>? options = null)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            options ??= new DapperQueryOptions<TEntity>();

            ValidatePagination(options);

            var mapping = DapperMapping<TEntity, TKey>.Instance;

            var parameters = new DynamicParameters();

            var sql = new StringBuilder();

            sql.Append("SELECT ");
            sql.Append(
                BuildSelect(
                    mapping,
                    options.Columns));

            sql.AppendLine();
            sql.Append("FROM ");
            sql.Append(mapping.TableName);

            AppendWhere(
                sql,
                mapping,
                options.Where,
                parameters);

            AppendOrderBy(
                sql,
                mapping,
                options.OrderBy);

            AppendPagination(
                sql,
                mapping,
                options,
                parameters);

            return new DapperSqlResult
            {
                Sql = sql.ToString(),
                Parameters = parameters
            };
        }

        public static DapperSqlResult BuildCount<TEntity, TKey>(
            DapperQueryOptions<TEntity>? options = null)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            options ??= new DapperQueryOptions<TEntity>();

            var mapping = DapperMapping<TEntity, TKey>.Instance;

            var parameters = new DynamicParameters();

            var sql = new StringBuilder();

            sql.Append("SELECT COUNT(1)");
            sql.AppendLine();
            sql.Append("FROM ");
            sql.Append(mapping.TableName);

            AppendWhere(
                sql,
                mapping,
                options.Where,
                parameters);

            return new DapperSqlResult
            {
                Sql = sql.ToString(),
                Parameters = parameters
            };
        }

        private static string BuildSelect<TEntity, TKey>(
            DapperMapping<TEntity, TKey> mapping,
            Expression<Func<TEntity, object>>? columns)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            if (columns is null)
                return "*";

            var properties = ExtractProperties(columns);

            if (properties.Count == 0)
                return "*";

            var result = properties
                .Select(property =>
                {
                    var column = mapping.GetColumn(property.Name);

                    if (column is null)
                    {
                        throw new InvalidOperationException(
                            $"A propriedade '{property.Name}' " +
                            $"não está mapeada na entidade '{typeof(TEntity).Name}'.");
                    }

                    return column.QuotedColumnName;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result.Count == 0
                ? "*"
                : string.Join(", ", result);
        }

        public static DapperSqlResult BuildById<TEntity, TKey>(
            TKey id,
            Expression<Func<TEntity, object>>? columns = null)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            var mapping = DapperMapping<TEntity, TKey>.Instance;

            var parameters = new DynamicParameters();

            parameters.Add("Id", id);

            var select = BuildSelect(
                mapping,
                columns);

            var conditions = new List<string>
            {
                $"{mapping.Key.QuotedColumnName} = @Id"
            };

            // Mesma regra de soft-delete aplicada por Build/BuildCount — busca
            // por id não deve "enxergar" registros logicamente excluídos.
            var softDeleteFilter = DapperSoftDeleteSql.Filter<TEntity>();

            if (!string.IsNullOrWhiteSpace(softDeleteFilter))
            {
                conditions.Add(softDeleteFilter);
            }

            return new DapperSqlResult
            {
                Sql = $"""
            SELECT {select}
            FROM {mapping.TableName}
            WHERE {string.Join(" AND ", conditions)}
            """,

                Parameters = parameters
            };
        }

        private static void AppendWhere<TEntity, TKey>(
            StringBuilder sql,
            DapperMapping<TEntity, TKey> mapping,
            Expression<Func<TEntity, bool>>? where,
            DynamicParameters parameters)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {

            var conditions = new List<string>();

            var softDeleteFilter = DapperSoftDeleteSql.Filter<TEntity>();

            if (!string.IsNullOrWhiteSpace(softDeleteFilter))
            {
                conditions.Add(softDeleteFilter);
            }

            if (where is not null)
            {
                var builder = new WhereExpressionBuilder<TEntity, TKey>(
                    mapping,
                    parameters);

                var expression = builder.Build(where);

                if (!string.IsNullOrWhiteSpace(expression))
                {
                    conditions.Add(expression);
                }
            }

            if (conditions.Count == 0)
                return;

            sql.AppendLine();
            sql.Append("WHERE ");
            sql.Append(string.Join(" AND ", conditions));
        }

        private static void AppendOrderBy<TEntity, TKey>(
            StringBuilder sql,
            DapperMapping<TEntity, TKey> mapping,
            IReadOnlyCollection<DapperSortOptions<TEntity>> orderBy)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            if (orderBy.Count == 0)
                return;

            var columns = new List<string>();

            foreach (var sort in orderBy)
            {
                var properties = ExtractProperties(sort.Expression);

                foreach (var property in properties)
                {
                    var column = mapping.GetColumn(property.Name);

                    if (column is null)
                    {
                        throw new InvalidOperationException(
                            $"A propriedade '{property.Name}' " +
                            $"não está mapeada na entidade '{typeof(TEntity).Name}'.");
                    }

                    var direction = sort.Direction == SortDirection.Desc
                        ? "DESC"
                        : "ASC";

                    columns.Add(
                        $"{column.QuotedColumnName} {direction}");
                }
            }

            if (columns.Count == 0)
                return;

            sql.AppendLine();
            sql.Append("ORDER BY ");
            sql.Append(string.Join(", ", columns));
        }

        private static void AppendPagination<TEntity, TKey>(
            StringBuilder sql,
            DapperMapping<TEntity, TKey> mapping,
            DapperQueryOptions<TEntity> options,
            DynamicParameters parameters)
            where TEntity : Entity<TKey>
            where TKey : IEquatable<TKey>
        {
            if (!options.HasPagination)
                return;

            if (options.OrderBy.Count == 0)
            {
                sql.AppendLine();
                sql.Append("ORDER BY ");
                sql.Append(mapping.Key.QuotedColumnName);
            }

            parameters.Add(
                "Offset",
                options.Offset);

            parameters.Add(
                "PageSize",
                options.PageSize!.Value);

            sql.AppendLine();
            sql.Append("OFFSET @Offset ROWS");

            sql.AppendLine();
            sql.Append("FETCH NEXT @PageSize ROWS ONLY");
        }

        private static void ValidatePagination<TEntity>(
            DapperQueryOptions<TEntity> options)
        {
            if (!options.HasPagination)
                return;

            if (!options.Page.HasValue ||
                !options.PageSize.HasValue)
            {
                throw new ArgumentException(
                    "Page e PageSize devem ser informados juntos.");
            }

            if (options.Page.Value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.Page),
                    "Page deve ser maior ou igual a 1.");
            }

            if (options.PageSize.Value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.PageSize),
                    "PageSize deve ser maior ou igual a 1.");
            }
        }

        private static IReadOnlyList<PropertyInfo> ExtractProperties<TEntity>(
            Expression<Func<TEntity, object>> expression)
        {
            var body = RemoveConvert(expression.Body);

            return body switch
            {
                MemberExpression member =>
                    ExtractMember(member),

                NewExpression newExpression =>
                    ExtractNewExpression(newExpression),

                _ => throw new ArgumentException(
                    "A expressão deve conter uma propriedade da entidade " +
                    "ou um objeto anônimo contendo propriedades da entidade.",
                    nameof(expression))
            };
        }

        private static IReadOnlyList<PropertyInfo> ExtractMember(
            MemberExpression expression)
        {
            if (expression.Member is not PropertyInfo property)
            {
                throw new ArgumentException(
                    $"'{expression.Member.Name}' não é uma propriedade.");
            }

            return [property];
        }

        private static IReadOnlyList<PropertyInfo> ExtractNewExpression(
            NewExpression expression)
        {
            var properties = new List<PropertyInfo>();

            foreach (var argument in expression.Arguments)
            {
                var member =
                    RemoveConvert(argument) as MemberExpression;

                if (member?.Member is not PropertyInfo property)
                {
                    throw new ArgumentException(
                        "Todas as propriedades devem ser propriedades " +
                        "diretas da entidade.");
                }

                properties.Add(property);
            }

            return properties;
        }

        private static Expression RemoveConvert(
            Expression expression)
        {
            while (expression is UnaryExpression unary &&
                   (unary.NodeType == ExpressionType.Convert ||
                    unary.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unary.Operand;
            }

            return expression;
        }

        /// <summary>
        /// Escapa caracteres curinga do LIKE (%, _, [) em um valor de busca.
        /// Público e compartilhado porque tanto o WhereExpressionBuilder
        /// (Contains/StartsWith/EndsWith via expressão) quanto qualquer SQL
        /// manual escrito num repositório específico (ex.: uma consulta com
        /// JOIN, fora do builder genérico) precisam da mesma regra — mantê-la
        /// em um único lugar evita as duas implementações divergirem.
        /// </summary>
        public static string EscapeLikeValue(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal);
        }
    }

    internal sealed class WhereExpressionBuilder<TEntity, TKey>
        where TEntity : Entity<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly DapperMapping<TEntity, TKey> _mapping;
        private readonly DynamicParameters _parameters;

        private int _parameterIndex;

        public WhereExpressionBuilder(
            DapperMapping<TEntity, TKey> mapping,
            DynamicParameters parameters)
        {
            _mapping = mapping;
            _parameters = parameters;
        }

        public string Build(
            Expression<Func<TEntity, bool>> expression)
        {
            return Visit(expression.Body);
        }

        private string Visit(Expression expression)
        {
            expression = RemoveConvert(expression);

            return expression.NodeType switch
            {
                ExpressionType.AndAlso =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "AND"),

                ExpressionType.OrElse =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "OR"),

                ExpressionType.Equal =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "="),

                ExpressionType.NotEqual =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "<>"),

                ExpressionType.GreaterThan =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        ">"),

                ExpressionType.GreaterThanOrEqual =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        ">="),

                ExpressionType.LessThan =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "<"),

                ExpressionType.LessThanOrEqual =>
                    VisitBinary(
                        (BinaryExpression)expression,
                        "<="),

                ExpressionType.Not =>
                    VisitNot(
                        (UnaryExpression)expression),

                ExpressionType.Call =>
                    VisitMethodCall(
                        (MethodCallExpression)expression),

                ExpressionType.MemberAccess =>
                    VisitMemberAccess(
                        (MemberExpression)expression),

                ExpressionType.Constant =>
                    VisitConstant(
                        (ConstantExpression)expression),

                _ => throw new NotSupportedException(
                    $"A expressão '{expression.NodeType}' não é suportada.")
            };
        }

        private string VisitBinary(
            BinaryExpression expression,
            string operatorName)
        {
            if (IsNullExpression(expression.Right))
            {
                var left = Visit(expression.Left);

                return operatorName switch
                {
                    "=" => $"{left} IS NULL",

                    "<>" => $"{left} IS NOT NULL",

                    _ => throw new NotSupportedException(
                        $"O operador '{operatorName}' não pode ser usado com NULL.")
                };
            }

            if (IsNullExpression(expression.Left))
            {
                var right = Visit(expression.Right);

                return operatorName switch
                {
                    "=" => $"{right} IS NULL",

                    "<>" => $"{right} IS NOT NULL",

                    _ => throw new NotSupportedException(
                        $"O operador '{operatorName}' não pode ser usado com NULL.")
                };
            }

            var leftExpression = Visit(expression.Left);
            var rightExpression = Visit(expression.Right);

            if (expression.NodeType == ExpressionType.AndAlso ||
                expression.NodeType == ExpressionType.OrElse)
            {
                return $"({leftExpression} {operatorName} {rightExpression})";
            }

            return $"{leftExpression} {operatorName} {rightExpression}";
        }

        private string VisitNot(
            UnaryExpression expression)
        {
            var operand = Visit(expression.Operand);

            return $"NOT ({operand})";
        }

        private string VisitMethodCall(
            MethodCallExpression expression)
        {
            if (expression.Method.DeclaringType != typeof(string))
            {
                throw new NotSupportedException(
                    $"O método '{expression.Method.Name}' não é suportado.");
            }

            if (expression.Object is null)
            {
                throw new NotSupportedException(
                    $"O método '{expression.Method.Name}' não possui objeto.");
            }

            var column = Visit(expression.Object);

            var argument = expression.Arguments.FirstOrDefault();

            if (argument is null)
            {
                throw new NotSupportedException(
                    $"O método '{expression.Method.Name}' " +
                    "precisa de um parâmetro.");
            }

            var value = Evaluate(argument);

            // Escapa caracteres curinga do LIKE (%, _, [) que possam existir no
            // próprio valor pesquisado — sem isso, um Document/Name contendo
            // esses caracteres produziria matches incorretos (falso positivo/
            // negativo), mesmo o parâmetro já sendo bindado com segurança
            // (não há risco de injeção, apenas de resultado errado).
            if (value is string stringValue)
            {
                value = DapperSqlBuilder.EscapeLikeValue(stringValue);
            }

            var parameterName = AddParameter(value);

            return expression.Method.Name switch
            {
                nameof(string.Contains) =>
                    $"{column} LIKE '%' + {parameterName} + '%' ESCAPE '\\'",

                nameof(string.StartsWith) =>
                    $"{column} LIKE {parameterName} + '%' ESCAPE '\\'",

                nameof(string.EndsWith) =>
                    $"{column} LIKE '%' + {parameterName} ESCAPE '\\'",

                _ => throw new NotSupportedException(
                    $"O método string.{expression.Method.Name} " +
                    "não é suportado.")
            };
        }

        private string VisitMemberAccess(
            MemberExpression expression)
        {
            if (expression.Expression is not null &&
                expression.Expression.NodeType == ExpressionType.Parameter)
            {
                if (expression.Member is not PropertyInfo property)
                {
                    throw new NotSupportedException(
                        $"'{expression.Member.Name}' não é uma propriedade.");
                }

                var column = _mapping.GetColumn(property.Name);

                if (column is null)
                {
                    throw new InvalidOperationException(
                        $"A propriedade '{property.Name}' " +
                        $"não está mapeada na entidade '{typeof(TEntity).Name}'.");
                }

                return column.QuotedColumnName;
            }

            var value = Evaluate(expression);

            return AddParameter(value);
        }

        private string VisitConstant(
            ConstantExpression expression)
        {
            return AddParameter(expression.Value);
        }

        private string AddParameter(object? value)
        {
            var name = $"@p{_parameterIndex++}";

            _parameters.Add(
                name,
                value);

            return name;
        }

        /// <summary>
        /// Extrai o valor de uma sub-expressão do lado direito do filtro
        /// (tipicamente uma variável capturada por closure, ex.: "officeId"
        /// em "x.OfficeId == officeId") sem compilar nenhuma <see cref="Expression"/>.
        ///
        /// Antes, todo valor não-constante era resolvido via
        /// <c>Expression.Lambda(...).Compile()</c> — uma das operações mais
        /// caras do .NET — executada do zero a cada parâmetro, em toda
        /// chamada ao repositório. Como o caso comum é justamente "campo de
        /// uma classe de closure gerada pelo compilador" (um
        /// <see cref="MemberExpression"/> encadeado sobre uma
        /// <see cref="ConstantExpression"/>), dá para andar essa cadeia por
        /// reflection direta (FieldInfo/PropertyInfo.GetValue), muito mais
        /// barato. Só o que realmente não se encaixa nesse formato (raro —
        /// ex.: chamada de método dentro do valor) cai no fallback de compilar.
        /// </summary>
        private static object? Evaluate(
            Expression expression)
        {
            expression = RemoveConvert(expression);

            switch (expression)
            {
                case ConstantExpression constant:
                    return constant.Value;

                case MemberExpression { Expression: not null } member
                    when member.Expression.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:
                {
                    var target = Evaluate(member.Expression);

                    return member.Member switch
                    {
                        FieldInfo field => field.GetValue(target),
                        PropertyInfo property => property.GetValue(target),
                        _ => CompileAndEvaluate(expression)
                    };
                }

                default:
                    return CompileAndEvaluate(expression);
            }
        }

        private static object? CompileAndEvaluate(
            Expression expression)
        {
            var objectExpression =
                Expression.Convert(
                    expression,
                    typeof(object));

            var lambda =
                Expression.Lambda<Func<object?>>(
                    objectExpression);

            return lambda.Compile()();
        }

        private static bool IsNullExpression(
            Expression expression)
        {
            expression = RemoveConvert(expression);

            return expression is ConstantExpression
            {
                Value: null
            };
        }

        private static Expression RemoveConvert(
            Expression expression)
        {
            while (expression is UnaryExpression unary &&
                   (unary.NodeType == ExpressionType.Convert ||
                    unary.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unary.Operand;
            }

            return expression;
        }
    }
}