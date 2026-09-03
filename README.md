# Cg.ProjectTemplate.Api

[![CI](https://github.com/<org>/<repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<org>/<repo>/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-unspecified-lightgrey)

> Substitua `<org>/<repo>` pela URL real do repositório após publicá-lo no GitHub, para o badge de CI refletir o status real do workflow. Nenhuma licença foi definida — adicione um arquivo `LICENSE` antes de publicar publicamente, caso pretenda licenciar o uso deste código.

Template de backend em **.NET 10** seguindo **Clean Architecture**, com CQRS via MediatR, persistência híbrida (EF Core para escrita + Dapper para leitura), cache distribuído, jobs em background e observabilidade estruturada. Construído como ponto de partida para novos serviços — o domínio de exemplo (`DemoEmployee`/`DemoOffice`) existe só para exercitar a arquitetura de ponta a ponta e serve de referência de como adicionar uma nova feature.

> Renomeie `Cg.ProjectName` para o nome real do seu projeto (pastas, `.csproj`, namespaces, `.slnx`) ao usar este repositório como ponto de partida — é uma operação de find-and-replace em todo o repositório.

## Índice

- [Arquitetura](#arquitetura)
- [Stack técnica](#stack-técnica)
- [Padrões de projeto aplicados](#padrões-de-projeto-aplicados)
- [Funcionalidades](#funcionalidades)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Como executar](#como-executar)
- [Configuração](#configuração)
- [Testes](#testes)
- [Docker](#docker)
- [CI](#ci)
- [Endpoints da API](#endpoints-da-api)
- [Limitações conhecidas / roadmap](#limitações-conhecidas--roadmap)

## Arquitetura

Clean Architecture em camadas, com a regra de dependência apontando sempre para dentro (Domain não depende de nada; as camadas externas dependem de abstrações do Domain/Application, nunca o contrário):

```
┌─────────────────────────────────────────────────────────┐
│  Cg.ProjectName.WebApi                                   │
│  Controllers, Middleware de exceção, Swagger, CORS,       │
│  SignalR, Rate Limiting                                   │
└───────────────────────┬─────────────────────────────────┘
                         │
┌───────────────────────▼─────────────────────────────────┐
│  Cg.ProjectName.Application                               │
│  Commands/Queries (MediatR), Handlers, DTOs,               │
│  Validators (FluentValidation), AutoMapper Profiles        │
└───────────────────────┬─────────────────────────────────┘
                         │
┌───────────────────────▼─────────────────────────────────┐
│  Cg.ProjectName.Domain                                     │
│  Entidades, regras de negócio, interfaces de repositório,  │
│  exceções de domínio — zero dependência de infraestrutura  │
└───────────────────────▲─────────────────────────────────┘
                         │ implementa
┌───────────────────────┴─────────────────────────────────┐
│  Cg.ProjectName.Infrastructure.Data                        │
│  EF Core (escrita/transações) + Dapper (leitura/queries)   │
├───────────────────────────────────────────────────────────┤
│  Cg.ProjectName.Infrastructure.CrossCutting.Ioc/Shared      │
│  Composição de DI, pipeline behaviors, logging, Redis,     │
│  Hangfire, health checks                                    │
└───────────────────────────────────────────────────────────┘
```

A persistência é híbrida de propósito: **EF Core** cuida do lado de escrita (tracking, Unit of Work, migrations, concorrência otimista), enquanto **Dapper** cuida do lado de leitura (queries otimizadas, sem tracking, com paginação/ordenação/filtros construídos dinamicamente). Nenhum dos dois é usado para o que o outro faz melhor.

## Stack técnica

| Categoria | Tecnologia |
|---|---|
| Runtime | .NET 10 / C# 13 |
| API | ASP.NET Core Web API |
| Mediação/CQRS | MediatR 14 |
| ORM (escrita) | Entity Framework Core 10 + SQL Server |
| Micro-ORM (leitura) | Dapper 2 |
| Validação | FluentValidation 12 |
| Mapeamento | AutoMapper 16 |
| Cache distribuído | Redis (StackExchange.Redis) — com fallback automático para cache em memória quando desabilitado/não configurado |
| Jobs em background | Hangfire (storage em SQL Server) |
| Logging | Serilog (console + arquivo, enriquecido com exception details, correlação por TraceId) |
| Documentação de API | Swagger / Swashbuckle, com XML comments |
| Testes | xUnit |
| Containerização | Docker (build multi-stage) |
| CI | GitHub Actions |

## Padrões de projeto aplicados

- **Clean Architecture** — separação em Domain/Application/Infrastructure/WebApi com a regra de dependência apontando para dentro.
- **CQRS** — Commands e Queries como tipos distintos, cada um com seu Handler dedicado (MediatR), em vez de um serviço genérico fazendo tudo.
- **Pipeline Behaviors (Decorator)** — validação, cache, invalidação de cache e Unit of Work aplicados a toda requisição MediatR via decoradores encadeados, na ordem: `Validation → Cache → CacheInvalidation → UnitOfWork`.
- **Repository + Unit of Work** — repositórios de escrita (EF Core, com tracking) e leitura (Dapper, sem tracking) atrás de interfaces definidas no Domain; a transação e o `SaveChanges` são responsabilidade do `UnitOfWorkBehavior`, não do handler.
- **Rich Domain Model** — mudanças de estado passam por métodos de domínio explícitos (`DemoEmployee.Change/Activate/Inactivate`), nunca por um mapeamento cego que sobrescreve a entidade rastreada.
- **Optimistic Concurrency (token de concorrência)** — coluna `rowversion` do SQL Server, exposta nos DTOs de leitura e aceita de volta no comando de atualização, para detectar edições concorrentes (409 Conflict) em vez de last-writer-wins silencioso.
- **Soft Delete transversal** — aplicado via `IEntityTypeConfiguration` genérico + global query filter, para qualquer entidade que implemente `EntitySoftDeletable<T>`, sem repetir a regra em cada configuração individual.
- **Module Initializers** — composição de DI organizada por camada (`InfrastructureModuleInitializer`, `ApplicationModuleInitializer`, `WebApiModuleInitializer`) em vez de um único `Program.cs` monolítico.
- **Options/Strategy para infraestrutura opcional** — Redis e Hangfire são "liga/desliga" via configuração (`Redis:Enabled`, `Hangfire:Enabled`), com fallback seguro (cache em memória) quando desabilitados.
- **Execution Strategy (retry resiliente)** — toda escrita transacional roda dentro da `IExecutionStrategy` do EF Core (retry automático em falha transiente do SQL Server), com o `ChangeTracker` limpo a cada tentativa para evitar duplicação de dados em um retry.

## Funcionalidades

- CRUD completo de funcionários de demonstração (`DemoEmployee`), com filial (`DemoOffice`) criada sob demanda por nome.
- Listagem paginada, com filtro por nome/filial/status e ordenação por múltiplos campos.
- Validação de entrada centralizada (FluentValidation) com resposta 400 padronizada.
- Tratamento centralizado de exceções (`ValidationExceptionMiddleware`), mapeando exceções de domínio/infraestrutura para os status HTTP corretos (400/401/404/409/500), sem try/catch espalhado pelos controllers.
- Cache de queries e invalidação automática em comandos, via Redis (ou memória).
- Processamento de jobs em background via Hangfire, com dashboard protegido.
- Health checks separados por liveness (`/health/live`) e readiness (`/health/ready`, checando SQL Server e Redis).
- Rate limiting global por IP.
- Logs estruturados (Serilog) com correlação por `TraceId`, prontos para um coletor de logs (stdout) ou arquivo local.
- Aplicação automática de migrations pendentes no startup (fail-fast em caso de erro).
- Documentação interativa da API via Swagger, incluindo os comentários XML dos controllers/DTOs.

## Estrutura de pastas

```
src/
  Cg.ProjectName.Domain/                          # entidades, regras de negócio, interfaces
  Cg.ProjectName.Application/                      # commands/queries, handlers, DTOs, validators
  Cg.ProjectName.Infrastructure.Data/               # EF Core + Dapper, migrations, repositórios
  Cg.ProjectName.Infrastructure.CrossCutting.Ioc/    # composição de DI, health checks, Hangfire/Redis
  Cg.ProjectName.Infrastructure.CrossCutting.Shared/ # pipeline behaviors, logging
  Cg.ProjectName.Infrastructure.CrossCutting.Security/ # reservado para autenticação/autorização (ver Limitações)
  Cg.ProjectName.WebApi/                            # controllers, middleware, Swagger, CORS, Program.cs
tests/
  Cg.ProjectName.Test/                              # testes de unidade (xUnit) — Domain, Application, WebApi
```

## Como executar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (local, container ou Azure SQL) — acessível via a connection string configurada
- Redis (opcional — sem ele, a aplicação usa cache em memória automaticamente)

### Passo a passo

```bash
# 1. Clone o repositório
git clone <url-do-repositorio>
cd Cg.ProjectName

# 2. Configure a connection string do SQL Server
#    (edite src/Cg.ProjectName.WebApi/appsettings.Development.json,
#     ou use variáveis de ambiente — ver seção Configuração)

# 3. Restaure os pacotes
dotnet restore Cg.ProjectName.slnx

# 4. Rode a API — migrations pendentes são aplicadas automaticamente no
#    startup (Database:AutoMigrate=true por padrão)
dotnet run --project src/Cg.ProjectName.WebApi
```

A API sobe em `https://localhost:7066` (e `http://localhost:5119`) em modo Development, com o Swagger disponível na raiz (`/swagger`).

### Gerando uma nova migration

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/Cg.ProjectName.Infrastructure.Data \
  --startup-project src/Cg.ProjectName.WebApi
```

## Configuração

Toda configuração segue o padrão de camadas do .NET (`appsettings.json` → `appsettings.{Environment}.json` → variáveis de ambiente, na ordem de precedência crescente). **Nenhum segredo real deve ser commitado** — os arquivos deste repositório contêm apenas valores de desenvolvimento local.

| Chave | Descrição | Padrão |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Connection string do SQL Server | `Server=localhost;...` (dev) |
| `Database:AutoMigrate` | Aplica migrations pendentes automaticamente no startup | `true` |
| `Redis:Enabled` | Liga/desliga o cache distribuído via Redis | `true` |
| `Redis:ConnectionString` | Connection string do Redis (ignorada se `Enabled=false`) | `localhost:6379` (dev) |
| `Hangfire:Enabled` | Liga/desliga o servidor e dashboard do Hangfire | `true` |
| `Hangfire:DashboardPath` | Caminho do dashboard do Hangfire | `/hangfire` |
| `CorsOrigins:AllowedOrigins` | Lista explícita de origens permitidas (produção/homologação) | vazio |
| `Logging:EnableFileSink` | Habilita o sink de arquivo do Serilog, além do console | `true` |

Em **produção**, defina `CorsOrigins:AllowedOrigins` explicitamente — sem essa lista, e fora de ambiente de Development, toda requisição cross-origin é negada por padrão (fail-closed). Todas as demais chaves sensíveis (connection strings, credenciais) devem vir de variáveis de ambiente ou de um cofre de segredos (Azure Key Vault, AWS Secrets Manager, etc.), nunca de um arquivo commitado — o formato de variável de ambiente segue a convenção do .NET, por exemplo `ConnectionStrings__DefaultConnection`.

## Testes

```bash
dotnet test Cg.ProjectName.slnx
```

Cobrem validators (FluentValidation), regras de domínio das entidades, geração de SQL do Dapper (`DapperSqlBuilder`/`DapperMapping`) e o middleware central de tratamento de exceções — todos como testes de unidade, sem dependência de banco de dados real.

## Docker

```bash
docker build -t cg-projectname-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=CgProjectName;User Id=sa;Password=<sua-senha>;TrustServerCertificate=True;" \
  cg-projectname-api
```

A imagem é construída em múltiplos estágios (SDK só na etapa de build, runtime enxuto na imagem final) e roda como usuário não-root.

## CI

Todo push/PR nas branches principais dispara um workflow de GitHub Actions (`.github/workflows/ci.yml`) que restaura, builda e roda os testes automatizados da solução — veja o badge no topo deste README (ajuste `<org>/<repo>` ao publicar).

## Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/DemoEmployee/List` | Lista paginada, com filtro e ordenação |
| `GET` | `/api/DemoEmployee/{id}` | Detalhe de um funcionário |
| `POST` | `/api/DemoEmployee` | Cria um funcionário (201 Created) |
| `PUT` | `/api/DemoEmployee` | Atualiza um funcionário (409 se o `RowVersion` estiver desatualizado) |
| `DELETE` | `/api/DemoEmployee/{id}` | Remove (soft delete) um funcionário |
| `GET` | `/health/live` | Liveness — processo de pé |
| `GET` | `/health/ready` | Readiness — SQL Server e Redis acessíveis |
| `GET` | `/hangfire` | Dashboard de jobs em background |

A especificação completa (schemas, exemplos de request/response) está disponível no Swagger em tempo de execução (`/swagger`, apenas em Development).

## Limitações conhecidas / roadmap

Este repositório é publicado com transparência sobre o que ainda falta — nenhum desses pontos foi escondido:

- **Sem autenticação/autorização.** Não há nenhum esquema de autenticação configurado (o projeto `Cg.ProjectName.Infrastructure.CrossCutting.Security` existe como stub reservado para isso). Antes de qualquer uso além de demonstração/portfólio, é necessário adicionar um esquema real (JWT Bearer, OAuth2/OpenID Connect, etc.), proteger os controllers com `[Authorize]` e habilitar o `AddSecurityRequirement` já preparado (comentado) em `SwaggerConfiguration`.
- Este README documenta o estado do template; ajuste-o conforme o projeto real evoluir a partir daqui.
