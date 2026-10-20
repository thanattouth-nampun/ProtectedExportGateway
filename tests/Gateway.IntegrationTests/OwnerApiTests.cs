using System.Net;
using System.Net.Http.Json;
using Gateway.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gateway.IntegrationTests;

// HTTP control tests only. Real SQL persistence/transactions are tested in Gateway.SqlTests.
public sealed class OwnerApiTests
{
    private static readonly Guid Tenant=Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Owner=Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid A=Guid.Parse("33333333-3333-4333-8333-333333333333");
    private readonly RecordingStore store=new();
    private WebApplicationFactory<Program> Factory()=>new BffFactory().WithWebHostBuilder(b=>b.ConfigureServices(s=>{
        s.AddSingleton(new PortalIdentityDirectory(Tenant,[new(Owner,"Owner","owner@example.test","http-test"),new(A,"A","a@example.test","http-test")]));
        s.AddSingleton<IOwnerWorkspace>(store);
    }));
    private static HttpClient Client(WebApplicationFactory<Program> factory,string persona="Publisher")
    {
        var client=factory.CreateClient(new() {BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        client.DefaultRequestHeaders.Add("Test-Identity",persona); return client;
    }
    private static async Task Csrf(HttpClient client)
    {
        var token=await client.GetFromJsonAsync<Dictionary<string,string>>("/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",token!["token"]);
    }
    [Theory][InlineData("Guest")][InlineData("MissingMemberClaim")][InlineData("DuplicateMemberClaim")][InlineData("ForeignPublisher")]
    public async Task Guest_unknown_member_and_foreign_tenant_denials_do_not_reach_store(string persona)
    {
        using var factory=Factory(); using var client=Client(factory,persona);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/my/documents")).StatusCode);
        Assert.Equal(0,store.Calls);
    }
    [Fact] public async Task Reader_member_can_create_own_draft_without_publisher_role()
    {
        using var factory=Factory(); using var client=Client(factory,"A"); await Csrf(client);
        var response=await client.PostAsJsonAsync("/api/documents",new {title="A draft",purpose="Synthetic",requesterObjectIds=Array.Empty<Guid>()});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        Assert.Equal(A,store.Actor!.Principal.ObjectId); Assert.True(store.Actor.IsWorkspaceMember);
    }
    [Fact] public async Task Create_uses_session_owner_and_sends_ETag_without_source_fields()
    {
        using var factory=Factory(); using var client=Client(factory); await Csrf(client);
        var response=await client.PostAsJsonAsync("/api/documents",new {title="Draft",purpose="Synthetic",requesterObjectIds=new[]{A}});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode); Assert.Equal("\"test-etag\"",response.Headers.ETag!.ToString());
        Assert.Equal(Tenant,store.Actor!.Principal.TenantId); Assert.Equal(Owner,store.Actor.Principal.ObjectId);
        Assert.True(store.Actor.IsWorkspaceMember); Assert.Equal(A,Assert.Single(store.Edit!.RequesterObjectIds));
        var body=await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("blob",body,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NoSourceUploaded",body); Assert.Contains("false",body);
    }
    [Fact] public async Task Csrf_and_unknown_security_fields_block_before_store()
    {
        using var factory=Factory(); using var client=Client(factory);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/documents",new {})).StatusCode);
        await Csrf(client);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/documents",new {title="Draft",purpose="Synthetic",requesterObjectIds=Array.Empty<Guid>(),ownerOid=Owner})).StatusCode);
        Assert.Equal(0,store.Calls);
    }
    [Fact] public async Task Patch_passes_exact_IfMatch_and_returns_structured_stale_error()
    {
        using var factory=Factory(); using var client=Client(factory); await Csrf(client);
        var id=Guid.NewGuid();
        var request=new HttpRequestMessage(HttpMethod.Patch,$"/api/documents/{id}") {Content=JsonContent.Create(new {title="Edited",purpose="Synthetic",requesterObjectIds=Array.Empty<Guid>()})};
        request.Headers.TryAddWithoutValidation("If-Match","\"stale\"");
        var response=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.PreconditionFailed,response.StatusCode);
        Assert.Equal("\"stale\"",store.ETag); Assert.Contains("REVISION_CONFLICT",await response.Content.ReadAsStringAsync());
        Assert.Equal("application/problem+json",response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store",response.Headers.CacheControl!.ToString());
    }
    [Fact] public async Task Unconfigured_real_store_returns_503_to_publisher()
    {
        using var factory=new BffFactory(); using var client=Client(factory);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.GetAsync("/api/my/documents")).StatusCode);
    }
    [Fact] public async Task Oversized_metadata_is_rejected_before_binding_or_store()
    {
        using var factory=Factory(); using var client=Client(factory); await Csrf(client);
        foreach(var path in new[]{"/api/documents","/API/documents/"})
        {
            var response=await client.PostAsJsonAsync(path,new {title=new string('x',20000),purpose="Synthetic",requesterObjectIds=Array.Empty<Guid>()});
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge,response.StatusCode);
            Assert.Contains("REQUEST_TOO_LARGE",await response.Content.ReadAsStringAsync()); Assert.Equal(0,store.Calls);
        }
    }
    private sealed class RecordingStore:IOwnerWorkspace
    {
        public int Calls; public OwnerActor? Actor; public DraftEdit? Edit; public string? ETag;
        private static OwnerDocumentView View()=>new(Guid.NewGuid(),"Draft","Synthetic",1,1,"\"test-etag\"",[],"Draft","NoSourceUploaded",false);
        public Task<OwnerDocumentPage> List(OwnerActor actor,int skip,CancellationToken ct=default) {Calls++; return Task.FromResult(new OwnerDocumentPage([],0,skip,25));}
        public Task<OwnerDocumentView> Find(OwnerActor actor,Guid id,CancellationToken ct=default)=>Task.FromResult(View());
        public Task<OwnerDocumentView> Create(OwnerActor actor,DraftEdit edit,Guid correlation,CancellationToken ct=default)
        {Calls++;Actor=actor;Edit=edit; return Task.FromResult(View());}
        public Task<OwnerDocumentView> Update(OwnerActor actor,Guid id,string etag,DraftEdit edit,Guid correlation,CancellationToken ct=default)
        {Calls++;ETag=etag; throw new WorkspaceFault(412,"REVISION_CONFLICT");}
        public Task<OwnerAuditView[]> Audit(OwnerActor actor,Guid id,CancellationToken ct=default)=>Task.FromResult(Array.Empty<OwnerAuditView>());
    }
}
