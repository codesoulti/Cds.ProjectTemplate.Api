using Cg.ProjectName.Domain.Exceptions;
using Cg.ProjectName.WebApi.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cg.ProjectName.Test.WebApi.Middleware;

// ValidationExceptionMiddleware é o único lugar que decide 400/401/404/409/500
// para toda a API (7 catches distintos) e não tinha nenhum teste até agora —
// o gap de cobertura mais evidente encontrado na auditoria final. Os testes
// aqui exercitam a própria instância do middleware com um RequestDelegate
// falso que lança a exceção sob teste, sem precisar de um servidor HTTP real
// (TestServer/WebApplicationFactory) só para verificar mapeamento de status
// code.
public class ValidationExceptionMiddlewareTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Cg.ProjectName.Test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static async Task<(int StatusCode, string Body)> InvokeAsync(
        RequestDelegate next,
        string environmentName = "Production")
    {
        var middleware = new ValidationExceptionMiddleware(
            next,
            NullLogger<ValidationExceptionMiddleware>.Instance,
            new FakeHostEnvironment { EnvironmentName = environmentName });

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task InvokeAsync_WithNoException_PassesThroughUntouched()
    {
        var (statusCode, _) = await InvokeAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status204NoContent, statusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithFluentValidationException_Returns400()
    {
        var (statusCode, body) = await InvokeAsync(
            _ => throw new ValidationException("Nome é obrigatório."));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Contains("Validation Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Mesmo caminho usado por BaseController.GetCurrentUserId/Email
        // quando não há usuário autenticado.
        var (statusCode, body) = await InvokeAsync(
            _ => throw new UnauthorizedAccessException("Usuário não autenticado."));

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.Contains("Unauthorized Access", body);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        var (statusCode, body) = await InvokeAsync(
            _ => throw NotFoundException.For<object>(Guid.NewGuid()));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Contains("Resource Not Found", body);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        // Cobre também ArgumentOutOfRangeException (subtipo de
        // ArgumentException), usado pelas guardas de paginação em
        // DapperRepository.QueryPagedAsync.
        var (statusCode, body) = await InvokeAsync(
            _ => throw new ArgumentOutOfRangeException("pageSize", "PageSize deve ser maior ou igual a 1."));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Contains("Invalid Request", body);
    }

    [Fact]
    public async Task InvokeAsync_WithDbUpdateConcurrencyException_Returns409()
    {
        // DbUpdateConcurrencyException deriva de DbUpdateException — este
        // teste garante que o catch mais específico continua vindo primeiro
        // (senão cairia no branch genérico de DbUpdateException e viraria
        // 500 em vez de 409).
        var (statusCode, body) = await InvokeAsync(
            _ => throw new DbUpdateConcurrencyException("Conflito de concorrência."));

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Contains("Conflict", body);
    }

    [Fact]
    public async Task InvokeAsync_WithGenericDbUpdateException_Returns500WithDatabaseErrorMessage()
    {
        var (statusCode, body) = await InvokeAsync(
            _ => throw new DbUpdateException("Falha ao salvar."));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Contains("Database Error", body);
    }

    [Fact]
    public async Task InvokeAsync_WithUnmappedException_InProduction_HidesExceptionMessage()
    {
        var (statusCode, body) = await InvokeAsync(
            _ => throw new InvalidOperationException("detalhe interno sensível"),
            environmentName: "Production");

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.DoesNotContain("detalhe interno sensível", body);
    }

    [Fact]
    public async Task InvokeAsync_WithUnmappedException_InDevelopment_IncludesExceptionMessage()
    {
        var (statusCode, body) = await InvokeAsync(
            _ => throw new InvalidOperationException("detalhe para debug local"),
            environmentName: Environments.Development);

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Contains("detalhe para debug local", body);
    }
}
