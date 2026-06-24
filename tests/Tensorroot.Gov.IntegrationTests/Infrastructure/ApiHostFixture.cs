using Xunit;

namespace Tensorroot.Gov.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture compartilhada: o ApiHost (TestServer in-memory) sobe UMA vez por colecao de testes — o boot
/// (descoberta de modulos + banco de controle + auto-seed) e caro. Os testes ISOLAM o estado entre si
/// provisionando TENANTS proprios (banco dedicado por tenant), nunca compartilhando dados.
/// </summary>
public sealed class ApiHostFixture : IAsyncLifetime
{
    /// <summary>A fabrica do ApiHost sob teste.</summary>
    public CustomWebApplicationFactory Factory { get; } = new();

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        // Forca o boot do host (e o auto-seed de DEV) antes do primeiro teste.
        _ = Factory.Services;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

/// <summary>Colecao xUnit que compartilha o <see cref="ApiHostFixture"/> entre as classes E2E.</summary>
[CollectionDefinition(Nome)]
public sealed class ApiHostCollection : ICollectionFixture<ApiHostFixture>
{
    /// <summary>Nome da colecao (referenciado por [Collection] nas classes de teste).</summary>
    public const string Nome = "ApiHost E2E";
}
