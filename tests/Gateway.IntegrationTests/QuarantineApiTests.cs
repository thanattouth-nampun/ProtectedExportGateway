using System.Net;
using System.Net.Http.Json;
using Gateway.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class QuarantineApiTests
{
    private static readonly Guid Tenant=Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid A=Guid.Parse("33333333-3333-4333-8333-333333333333");
    private readonly RecordingIntake store=new();
    private WebApplicationFactory<Program> Factory(bool configured=true)=>new BffFactory().WithWebHostBuilder(b=>b.ConfigureServices(s=>{
        s.AddSingleton(new PortalIdentityDirectory(Tenant,[new(A,"A","a@example.test","http-test")]));
        if(configured) s.AddSingleton<IQuarantineIntake>(store);
    }));
    private static HttpClient Client(WebApplicationFactory<Program> f,string persona="A")
    {var client=f.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});client.DefaultRequestHeaders.Add("Test-Identity",persona);return client;}
    private static async Task Csrf(HttpClient client)
    {var token=await client.GetFromJsonAsync<Dictionary<string,string>>("/auth/csrf");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",token!["token"]);}
    private static HttpRequestMessage Upload(string suffix="",HttpContent? content=null)
    {
        var r=new HttpRequestMessage(HttpMethod.Post,$"/api/documents/{Guid.NewGuid()}/uploads{suffix}") {Content=content ?? new ByteArrayContent([1,2,3])};
        r.Content.Headers.ContentType=new("application/octet-stream");r.Headers.Add("X-Upload-Filename","synthetic.xlsx");
        r.Headers.Add("X-Synthetic-Fixture","true");r.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());r.Headers.TryAddWithoutValidation("If-Match","\"expected\"");return r;
    }
    [Fact] public async Task Session_identity_and_headers_are_bound_server_side_without_private_binding_in_response()
    {
        using var f=Factory();using var c=Client(f);await Csrf(c);using var request=Upload();var response=await c.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal(A,store.Actor!.Principal.ObjectId);Assert.True(store.Actor.IsWorkspaceMember);
        Assert.Equal("\"expected\"",store.ETag);Assert.Equal(3,store.Bytes);Assert.True(store.Synthetic);
        var body=await response.Content.ReadAsStringAsync();Assert.Contains("AwaitingValidation",body);Assert.Contains("LocalQuarantineSpike",body);
        Assert.DoesNotContain("token",body,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("blob",body,StringComparison.OrdinalIgnoreCase);Assert.Contains("\"canPublish\":false",body);
        Assert.Contains("no-store",response.Headers.CacheControl!.ToString());
    }
    [Fact] public async Task Csrf_synthetic_ack_and_forbidden_security_fields_block_before_store()
    {
        using var f=Factory();using var c=Client(f);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(Upload())).StatusCode);await Csrf(c);
        using(var r=Upload()) {r.Headers.Remove("X-Synthetic-Fixture");Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(r)).StatusCode);}
        using(var r=Upload()) {r.Headers.Add("X-Upload-Owner","someone@example.test");Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(r)).StatusCode);}
        using(var r=Upload()) {r.Headers.Remove("X-Upload-Filename");r.Headers.Add("X-Upload-Filename","..%2Foutside.xlsx");Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(r)).StatusCode);}
        Assert.Equal(HttpStatusCode.BadRequest,(await c.SendAsync(Upload("?label=caller"))).StatusCode);Assert.Equal(0,store.Calls);
    }
    [Fact] public void Production_cannot_enable_local_intake_and_missing_synthetic_ack_rejects_startup()
    {
        using(var production=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{
            b.UseEnvironment("Production");b.UseSetting("Quarantine:EnableLocalSpike","true");}))
            Assert.Throws<InvalidOperationException>(()=>production.CreateClient());
        using(var development=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{
            b.UseEnvironment("Development");b.UseSetting("Quarantine:EnableLocalSpike","true");b.UseSetting("Quarantine:SyntheticOnlyAcknowledged","false");}))
            Assert.Throws<InvalidOperationException>(()=>development.CreateClient());
    }
    [Theory][InlineData("Guest")][InlineData("MissingMemberClaim")][InlineData("ForeignPublisher")]
    public async Task Nonmember_or_foreign_identity_cannot_list_intake(string persona)
    {using var f=Factory();using var c=Client(f,persona);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/documents/{Guid.NewGuid()}/uploads")).StatusCode);Assert.Equal(0,store.Calls);}
    [Fact] public async Task Disabled_dependency_is_503_and_not_a_fake_upload_success()
    {using var f=Factory(false);using var c=Client(f);await Csrf(c);Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.SendAsync(Upload())).StatusCode);}
    [Fact] public async Task Chunked_body_is_bounded_even_when_handler_streams_without_content_length()
    {
        using var f=Factory();using var c=Client(f);await Csrf(c);using var r=Upload(content:new StreamingContent());
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.SendAsync(r)).StatusCode);
    }
    private sealed class RecordingIntake:IQuarantineIntake
    {
        public int Calls;public OwnerActor? Actor;public string? ETag;public long Bytes;public bool Synthetic;
        public Task<QuarantineVersionView[]> List(OwnerActor actor,Guid doc,CancellationToken ct=default)=>Task.FromResult(Array.Empty<QuarantineVersionView>());
        public async Task<QuarantineVersionView> Upload(OwnerActor actor,Guid doc,string etag,Guid key,string filename,bool synthetic,Stream input,CancellationToken ct=default)
        {Calls++;Actor=actor;ETag=etag;Synthetic=synthetic;var buffer=new byte[65536];int n;while((n=await input.ReadAsync(buffer,ct))!=0) Bytes+=n;return new(Guid.NewGuid(),filename,"AwaitingValidation",Bytes,new string('A',64),DateTimeOffset.UtcNow,null);}
    }
    private sealed class StreamingContent:HttpContent
    {
        protected override bool TryComputeLength(out long length) {length=0;return false;}
        protected override async Task SerializeToStreamAsync(Stream stream,TransportContext? context)
        {var chunk=new byte[65536];for(var i=0;i<162;i++) await stream.WriteAsync(chunk);}
    }
}
