using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Domain.Entities.Demos;

/// <summary>
/// Read-model achatado para a listagem de <see cref="DemoEmployee"/>, já
/// trazendo o nome da filial (<see cref="OfficeName"/>) via LEFT JOIN com
/// DemoOffices. Não é uma entidade de persistência (não deriva de
/// Entity&lt;T&gt;, não é rastreada pelo EF) — existe só para o caso de uso
/// de listagem.
///
/// Poderíamos ter adicionado OfficeName direto em DemoEmployee (Dapper
/// preencheria via SELECT ... AS OfficeName sem precisar de nenhuma
/// configuração extra), mas isso colocaria uma propriedade que só faz
/// sentido nessa projeção dentro do agregado de domínio, usada por todo o
/// resto do sistema (EF, DemoOfficeWriterRepository, os outros
/// handlers). Um read-model dedicado é o padrão usual de CQRS com Dapper:
/// o lado de leitura devolve exatamente a forma que a tela precisa, sem
/// forçar o agregado a carregar campos de apresentação.
/// </summary>
public sealed class DemoEmployeeListItem
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Document { get; init; } = string.Empty;

    public DateTime DateHire { get; init; }

    // Faltava desde a criação deste read-model: GetListAsync nunca
    // selecionava esta coluna, então ListDemoEmployeeDto.DateTermination
    // sempre voltava null mesmo para funcionários desligados. Passou a
    // importar mais ainda agora que GetDemoEmployeeHandler também usa este
    // mesmo read-model (via GetDetailByIdAsync) para o detalhe de um único
    // funcionário.
    public DateTime? DateTermination { get; init; }

    public decimal Salary { get; init; }

    public EStatus Status { get; init; }

    public Guid OfficeId { get; init; }

    /// <summary>
    /// Nome da filial no momento da consulta. Nunca é filtrado por
    /// soft-delete da filial (não existe hoje um fluxo de exclusão de
    /// DemoOffice), então mesmo que isso mude no futuro, o nome histórico
    /// continua aparecendo aqui — ajustável em
    /// <see cref="Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees.DemoEmployeeReadRepository"/>
    /// caso a regra de negócio deva ser outra.
    /// </summary>
    public string? OfficeName { get; init; }

    /// <summary>
    /// Token de concorrência otimista (SQL Server rowversion) do registro no
    /// momento desta consulta. Projetado aqui (e não só em DemoEmployee)
    /// porque o lado de leitura (Dapper) precisa devolvê-lo ao cliente para
    /// permitir concorrência otimista de ponta a ponta — ver
    /// GetDemoEmployeeDto/ListDemoEmployeeDto.RowVersion e
    /// UpdateDemoEmployeeCommand.RowVersion.
    /// </summary>
    public byte[]? RowVersion { get; init; }
}
