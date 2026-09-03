using Microsoft.OpenApi;
using System.Reflection;

namespace Cg.ProjectName.WebApi.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Cg.ProjectName API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "Digite: Bearer {seu token}"
                });

            //options.AddSecurityRequirement(
            //    new OpenApiSecurityRequirement
            //    {
            //        {
            //            new OpenApiSecurityScheme
            //            {
            //                Reference =
            //                    new OpenApiReference
            //                    {
            //                        Type =
            //                            ReferenceType.SecurityScheme,

            //                        Id = "Bearer"
            //                    }
            //            },

            //            Array.Empty<string>()
            //        }
            //    });

            // Antes, GenerateDocumentationFile não estava habilitado no
            // .csproj — nenhum .xml de documentação era gerado no build, então
            // esta chamada (se existisse) não teria nada para ler. Todos os
            // comentários /// de controllers/DTOs ficavam invisíveis no
            // Swagger. Ver <GenerateDocumentationFile> em Cg.ProjectName.WebApi.csproj.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();

            app.UseSwaggerUI();
        }

        return app;
    }
}