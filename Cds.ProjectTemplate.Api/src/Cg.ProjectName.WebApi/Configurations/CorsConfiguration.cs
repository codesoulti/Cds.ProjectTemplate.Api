using System.Net;

namespace Cg.ProjectName.WebApi.Configurations;

public static class CorsConfiguration
{
    // Faixas de IP privadas (RFC 1918) usadas apenas para permitir o fluxo
    // de desenvolvimento local (frontend rodando em outra máquina da rede
    // interna, dispositivo físico testando contra a API local, etc.).
    private static readonly (byte[] Network, int PrefixLength)[] _privateIpv4Ranges =
    [
        ([10, 0, 0, 0], 8),
        ([172, 16, 0, 0], 12),
        ([192, 168, 0, 0], 16),
    ];

    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var origins = configuration
            .GetSection("CorsOrigins:AllowedOrigins")
            .Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy("DevCors", policy =>
            {
                // Antes, nenhuma origem era configurada (nem WithOrigins, nem
                // AllowAnyOrigin/SetIsOriginAllowed) enquanto AllowCredentials()
                // já estava habilitado — sem uma origem explícita, o ASP.NET
                // Core nega TODA requisição cross-origin por padrão, então essa
                // política nunca funcionou. (E não dá para simplesmente somar
                // AllowAnyOrigin() a AllowCredentials(): essa combinação é
                // proibida e lança exceção em runtime.)
                if (origins is { Length: > 0 })
                {
                    // Produção/homologação: lista explícita via appsettings
                    // ("CorsOrigins:AllowedOrigins").
                    policy.WithOrigins(origins);
                }
                else if (environment.IsDevelopment())
                {
                    // Nenhuma origem configurada E ambiente de desenvolvimento:
                    // permite apenas hosts usuais de dev local, nunca "qualquer
                    // origem" — mantém AllowCredentials() seguro.
                    //
                    // CORREÇÃO DE SEGURANÇA (auditoria final): a versão
                    // anterior comparava o HOSTNAME por prefixo de string
                    // (Host.StartsWith("10.")), o que é bypassável por um
                    // domínio de DNS real registrado por um atacante, como
                    // "10.attacker.com" — StartsWith("10.") retorna true para
                    // esse host mesmo não sendo um IP privado. A checagem
                    // agora exige que o host seja um LITERAL de IP válido
                    // (IPAddress.TryParse) e então valida a faixa por
                    // comparação binária de octetos/prefixo (IsPrivateIPv4),
                    // não mais por substring. Além disso, esse fallback
                    // permissivo só é usado com IsDevelopment() == true —
                    // em qualquer outro ambiente sem "CorsOrigins:AllowedOrigins"
                    // configurado, a política abaixo nega tudo por padrão
                    // (fail-closed), em vez de arriscar adivinhar origens
                    // "seguras" em produção.
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrEmpty(origin))
                            return false;

                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                            return false;

                        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (uri.Host.EndsWith(".ngrok-free.app", StringComparison.OrdinalIgnoreCase))
                            return true;

                        return IPAddress.TryParse(uri.Host, out var address)
                            && IsPrivateIPv4(address);
                    });
                }
                else
                {
                    // Produção/homologação sem "CorsOrigins:AllowedOrigins"
                    // configurado: nega toda origem cross-origin por padrão
                    // (fail-closed) em vez de herdar um fallback pensado para
                    // desenvolvimento local. Configure a lista explícita via
                    // appsettings.Production.json ou variável de ambiente
                    // "CorsOrigins__AllowedOrigins__0" antes do deploy.
                    policy.SetIsOriginAllowed(_ => false);
                }

                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static bool IsPrivateIPv4(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var addressBytes = address.GetAddressBytes();

        foreach (var (network, prefixLength) in _privateIpv4Ranges)
        {
            if (IsInRange(addressBytes, network, prefixLength))
                return true;
        }

        return false;
    }

    private static bool IsInRange(byte[] address, byte[] network, int prefixLength)
    {
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (address[i] != network[i])
                return false;
        }

        if (remainingBits == 0)
            return true;

        var mask = (byte)~(0xFF >> remainingBits);
        return (address[fullBytes] & mask) == (network[fullBytes] & mask);
    }
}
