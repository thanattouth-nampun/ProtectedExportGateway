using System.Net.Http.Json;
using Gateway.Application;
using Gateway.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class OwnerCompositionTests
{
    [Fact] public async Task Reviewed_config_binds_directory_and_sql_store_without_automatic_database_migration()
    {
        using var factory=new BffFactory().WithWebHostBuilder(b=>{
            b.UseSetting("WebIdentity:TenantId","11111111-1111-1111-1111-111111111111");
            b.UseSetting("Portal:Identities:0:ObjectId","22222222-2222-2222-2222-222222222222");
            b.UseSetting("Portal:Identities:0:DisplayName","Synthetic reviewed owner");
            b.UseSetting("Portal:Identities:0:RmsAddress","owner@example.test");
            b.UseSetting("Portal:Identities:0:ReviewReference","test-only directory review");
            // Deliberately nonexistent database: startup/options must not connect or auto-migrate.
            b.UseSetting("ConnectionStrings:Gateway","Server=(localdb)\\MSSQLLocalDB;Database=GatewayNotCreatedByCompositionTest;Integrated Security=true;Encrypt=false");
        });
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        client.DefaultRequestHeaders.Add("Test-Identity","Publisher");
        var options=await client.GetFromJsonAsync<Gateway.Web.RequesterOptionDto[]>("/api/document-requester-options");
        Assert.Equal("Synthetic reviewed owner",Assert.Single(options!).DisplayName);
        using var scope=factory.Services.CreateScope();
        Assert.IsType<SqlOwnerWorkspace>(scope.ServiceProvider.GetRequiredService<IOwnerWorkspace>());
    }
}
