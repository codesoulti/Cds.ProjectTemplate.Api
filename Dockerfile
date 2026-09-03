# syntax=docker/dockerfile:1

# ---- Build stage ------------------------------------------------------
# Antes não existia nenhum Dockerfile no repositório — este é o primeiro,
# pensado para produção: build em múltiplos estágios (a imagem final não
# carrega o SDK, só o runtime), restauração de pacotes em camada separada
# (cache de layer do Docker é invalidado só quando um .csproj muda, não a
# cada alteração de código-fonte) e execução como usuário não-root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro, para o `dotnet restore` ser cacheado
# enquanto nenhuma dependência mudar.
COPY Cg.ProjectName.slnx ./
COPY src/Cg.ProjectName.Domain/Cg.ProjectName.Domain.csproj src/Cg.ProjectName.Domain/
COPY src/Cg.ProjectName.Application/Cg.ProjectName.Application.csproj src/Cg.ProjectName.Application/
COPY src/Cg.ProjectName.Infrastructure.Data/Cg.ProjectName.Infrastructure.Data.csproj src/Cg.ProjectName.Infrastructure.Data/
COPY src/Cg.ProjectName.Infrastructure.CrossCutting.Ioc/Cg.ProjectName.Infrastructure.CrossCutting.Ioc.csproj src/Cg.ProjectName.Infrastructure.CrossCutting.Ioc/
COPY src/Cg.ProjectName.Infrastructure.CrossCutting.Security/Cg.ProjectName.Infrastructure.CrossCutting.Security.csproj src/Cg.ProjectName.Infrastructure.CrossCutting.Security/
COPY src/Cg.ProjectName.Infrastructure.CrossCutting.Shared/Cg.ProjectName.Infrastructure.CrossCutting.Shared.csproj src/Cg.ProjectName.Infrastructure.CrossCutting.Shared/
COPY src/Cg.ProjectName.Service/Cg.ProjectName.Service.csproj src/Cg.ProjectName.Service/
COPY src/Cg.ProjectName.Web/Cg.ProjectName.Web.csproj src/Cg.ProjectName.Web/
COPY src/Cg.ProjectName.WebApi/Cg.ProjectName.WebApi.csproj src/Cg.ProjectName.WebApi/
COPY tests/Cg.ProjectName.Test/Cg.ProjectName.Test.csproj tests/Cg.ProjectName.Test/

RUN dotnet restore src/Cg.ProjectName.WebApi/Cg.ProjectName.WebApi.csproj

# Agora copia o restante do código-fonte e publica só o projeto de API.
COPY src/ src/

RUN dotnet publish src/Cg.ProjectName.WebApi/Cg.ProjectName.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Roda como usuário não-root — a imagem base já traz o usuário "app"
# (UID 64198) pronto para isso desde as imagens .NET 8+.
USER app

COPY --from=build --chown=app:app /app/publish .

# Migrations são aplicadas pela própria aplicação no startup (ver Program.cs,
# Database:AutoMigrate) — não há passo de migração separado aqui de
# propósito, para manter a imagem autocontida. Para deployments que
# preferem migrar como um passo isolado do pipeline, defina
# Database__AutoMigrate=false e rode a migração antes do `docker run`.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cg.ProjectName.WebApi.dll"]
