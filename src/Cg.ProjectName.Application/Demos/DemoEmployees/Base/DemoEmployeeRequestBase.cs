namespace Cg.ProjectName.Application.Demos.DemoEmployees.Base;

// Antes herdava de Entity<Guid> (um tipo de persistência do Domain) só para
// ganhar a propriedade Id — inheritance-for-code-reuse sem relação "é um"
// real. Efeito colateral concreto: CreateDemoEmployeeCommand (um comando de
// CRIAÇÃO) era obrigado a carregar um Id que nunca usa (o handler sempre
// gera um Guid novo via DemoEmployee.Create). Id e Status agora vivem só em
// UpdateDemoEmployeeCommand, que é de fato o único caso de uso que precisa
// deles — criar sempre nasce Active (ver DemoEmployee.Create) e não expõe
// esse campo para o cliente decidir.
public abstract class DemoEmployeeCommandBase
{
    public required string Name { get; set; }

    public required string Document { get; set; }

    public required DateTime DateHire { get; set; }

    public DateTime? DateTermination { get; set; }

    public required decimal Salary { get; set; }

    public required string OfficeName { get; set; }
}
