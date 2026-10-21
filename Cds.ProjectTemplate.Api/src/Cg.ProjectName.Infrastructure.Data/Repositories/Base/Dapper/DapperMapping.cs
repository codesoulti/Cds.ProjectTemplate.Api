using Cg.ProjectName.Domain.Entities.Shared;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

public sealed class DapperMapping<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    private static readonly DapperMapping<TEntity, TKey> _instance = new();

    private readonly IReadOnlyDictionary<string, DapperColumn> _columns;

    private DapperMapping()
    {
        EntityType = typeof(TEntity);

        TableName = ResolveTableName();

        _columns = BuildColumns();

        Key = ResolveKey();
    }

    public static DapperMapping<TEntity, TKey> Instance => _instance;

    public Type EntityType { get; }

    public string TableName { get; }

    public DapperColumn Key { get; }

    public IReadOnlyCollection<DapperColumn> Columns => (IReadOnlyCollection<DapperColumn>)_columns.Values;

    public DapperColumn? GetColumn(string propertyName)
    {
        return _columns.TryGetValue(propertyName, out var column)
            ? column
            : null;
    }

    public string GetColumnName(string propertyName)
    {
        var column = GetColumn(propertyName);

        if (column is null)
        {
            throw new InvalidOperationException(
                $"A propriedade '{propertyName}' não está mapeada na entidade '{EntityType.Name}'.");
        }

        return column.QuotedColumnName;
    }

    private string ResolveTableName()
    {
        var tableAttribute = EntityType
            .GetCustomAttribute<TableAttribute>();

        var tableName = tableAttribute?.Name;

        if (string.IsNullOrWhiteSpace(tableName))
            tableName = EntityType.Name;

        // Honra o Schema do [Table(...)] (quando informado), mantendo Dapper
        // e EF Core apontando para a mesma tabela — antes, um schema definido
        // aqui era silenciosamente ignorado pelo lado Dapper.
        var schema = tableAttribute?.Schema;

        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}";
    }

    private IReadOnlyDictionary<string, DapperColumn> BuildColumns()
    {
        var properties = EntityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x =>
                x.CanRead &&
                x.CanWrite &&
                x.GetIndexParameters().Length == 0)
            .ToList();

        var columns = new Dictionary<string, DapperColumn>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<NotMappedAttribute>() is not null)
                continue;

            // Propriedades de navegação do EF (ex.: DemoEmployee.Office,
            // DemoOffice.Employees) não são colunas físicas — sem esse filtro,
            // elas entravam no mapeamento como [Office]/[Employees], gerando
            // SQL inválido ("Invalid column name") no dia em que alguém tentar
            // projetar ou filtrar por um desses campos.
            if (IsNavigationProperty(property))
                continue;

            var columnAttribute =
                property.GetCustomAttribute<ColumnAttribute>();

            var columnName = columnAttribute?.Name;

            if (string.IsNullOrWhiteSpace(columnName))
                columnName = property.Name;

            var key =
                property.GetCustomAttribute<KeyAttribute>() is not null;

            columns.Add(
                property.Name,
                new DapperColumn(
                    property,
                    columnName,
                    QuoteIdentifier(columnName),
                    key));
        }

        return columns;
    }

    private static bool IsNavigationProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Coleções (ex.: ICollection<DemoEmployee>) são sempre navegação —
        // string e byte[] são as únicas "coleções" que de fato representam
        // uma coluna escalar (texto/varbinary).
        if (underlyingType != typeof(string) &&
            underlyingType != typeof(byte[]) &&
            typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
        {
            return true;
        }

        // Referência direta a outra entidade (ex.: DemoOffice? Office).
        return IsEntityType(underlyingType);
    }

    private static bool IsEntityType(Type type)
    {
        var current = type;

        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private DapperColumn ResolveKey()
    {
        var key = _columns.Values
            .FirstOrDefault(x => x.IsKey);

        if (key is not null)
            return key;

        // Convenção: Id
        key = _columns.Values
            .FirstOrDefault(x =>
                string.Equals(
                    x.Property.Name,
                    "Id",
                    StringComparison.OrdinalIgnoreCase));

        if (key is not null)
            return key;

        // Convenção: {EntityName}Id
        var conventionName = $"{EntityType.Name}Id";

        key = _columns.Values
            .FirstOrDefault(x =>
                string.Equals(
                    x.Property.Name,
                    conventionName,
                    StringComparison.OrdinalIgnoreCase));

        if (key is not null)
            return key;

        throw new InvalidOperationException(
            $"Não foi possível identificar a chave primária da entidade '{EntityType.Name}'. " +
            $"Utilize [Key] ou siga a convenção 'Id'/'{conventionName}'.");
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                "O identificador não pode ser vazio.",
                nameof(identifier));
        }

        if (identifier.Contains(']'))
        {
            throw new ArgumentException(
                $"Identificador inválido: '{identifier}'.",
                nameof(identifier));
        }

        return $"[{identifier}]";
    }
}

public sealed class DapperColumn
{
    public DapperColumn(
        PropertyInfo property,
        string columnName,
        string quotedColumnName,
        bool isKey)
    {
        Property = property;
        ColumnName = columnName;
        QuotedColumnName = quotedColumnName;
        IsKey = isKey;
    }

    public PropertyInfo Property { get; }

    public string PropertyName => Property.Name;

    public string ColumnName { get; }

    public string QuotedColumnName { get; }

    public bool IsKey { get; }
}