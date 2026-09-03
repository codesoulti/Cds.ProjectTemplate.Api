# Instruções para o GitHub Copilot — Cg.ProjectName

Este repositório é um template de backend em .NET 10, em Clean Architecture,
usado como ponto de partida para novos projetos da Inov Automação. Ao sugerir
código aqui, siga estas convenções em vez das práticas genéricas padrão.

## Arquitetura e camadas

- `src/Cg.ProjectName.Domain`: entidades, regras de negócio, interfaces de
  repositório e exceções de domínio. Não deve depender de nenhuma outra
  camada nem de bibliotecas de infraestrutura (EF Core, Dapper, etc.).
- `src/Cg.ProjectName.Application`: casos de uso via MediatR (Commands/Queries +
  Handlers), DTOs, Validators (FluentValidation) e AutoMapper Profiles.
- `src/Cg.ProjectName.Infrastructure.Data`: implementação dos repositórios —
  EF Core para escrita (`*WriterRepository`, com tracking e Unit of Work) e
  Dapper para leitura (`*ReadRepository`, sem tracking, via
  `DapperSqlBuilder`/`DapperMapping`, que já aplicam soft-delete e schema
  automaticamente a partir do `[Table(...)]` da entidade).
- `src/Cg.ProjectName.Infrastructure.CrossCutting.Ioc`: composição da injeção de
  dependência, organizada em `ModuleInitializers` por camada
  (`InfrastructureModuleInitializer`, `ApplicationModuleInitializer`,
  `WebApiModuleInitializer`) e `Configurations` (MediatR, AutoMapper, Redis,
  Hangfire). Registros de infraestrutura/aplicação (banco, cache,
  mensageria, pipelines) pertencem aqui — nunca duplique um registro que já
  existe em outro lugar (ex.: não registre `AddMediatR` de novo em outro
  projeto).
- `src/Cg.ProjectName.Infrastructure.CrossCutting.Shared`: pipeline behaviors do
  MediatR (Validation, Cache, CacheInvalidation, UnitOfWork) e utilitários
  cross-cutting (logging).
- `src/Cg.ProjectName.WebApi`: controllers, middleware de tratamento de
  exceções e configurações específicas de apresentação HTTP (CORS, Swagger,
  SignalR). Controllers devem ficar finos — validação e regras de negócio
  vão no Handler; exceções de negócio (`NotFoundException`,
  `KeyNotFoundException`, `ValidationException`, `ArgumentException`,
  conflitos de concorrência) são tratadas centralmente por
  `ValidationExceptionMiddleware`, não em try/catch por controller.
- `tests/Cg.ProjectName.Test`: testes de unidade (xUnit) para Domain e
  Application.

## Convenções de código

- Comentários de intenção/negócio em português; nomes de tipos, membros e
  mensagens de log em inglês.
- Novas features seguem a estrutura de pasta por caso de uso:
  `Commands|Queries/<NomeDaFeature>/{Command,Handler,Dto,Validator,Profile}`.
- Entidades com soft-delete usam `EntitySoftDeletable<T>`/
  `EntityAudititedAndSoftDeletable<T>`; use `DateTime.UtcNow` (nunca
  `DateTime.Now`) em qualquer timestamp de auditoria.
- Ao criar-ou-buscar um registro por uma chave única (ex.: nome), siga o
  padrão de `DemoOfficeWriterRepository.GetOrCreateByNameAsync`: leitura
  otimista seguida de tratamento de `DbUpdateException` por violação de
  índice único, em vez de um simples "if not exists, cria" (isso evita uma
  condição de corrida entre requisições concorrentes).
- Mudanças em uma entidade já rastreada pelo EF Core devem passar por um
  método de domínio explícito (ex.: `DemoEmployee.Change(...)`,
  `Activate()`/`Inactivate()`), nunca por um `_mapper.Map(command, entity)`
  cego que ignora invariantes.

Este arquivo substitui um conteúdo genérico de boilerplate do Azure MCP que
não tinha relação com este projeto (que não usa Azure).
