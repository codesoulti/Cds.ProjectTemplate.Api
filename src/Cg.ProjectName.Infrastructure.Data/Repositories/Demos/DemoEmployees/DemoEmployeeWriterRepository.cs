using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.EfCore;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees;

/// <summary>
/// Initializes a new instance of DemoEmployeeWriterRepository
/// </summary>
/// <param name="context">The database context</param>
public class DemoEmployeeWriterRepository(CgProjectNameDbContext context) : 
    EfRepository<DemoEmployee, Guid>(context), 
    IDemoEmployeeWriterRepository
{
}
