using Gateway.Application;
using Gateway.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Web;

public static class QuarantineEndpoints
{
    public static void AddLocalQuarantine(this WebApplicationBuilder builder)
    {
        if(!builder.Configuration.GetValue<bool>("Quarantine:EnableLocalSpike")) return;
        if(!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Local quarantine spike is Development-only.");
        if(!builder.Configuration.GetValue<bool>("Quarantine:SyntheticOnlyAcknowledged")) throw new InvalidOperationException("Synthetic local spike acknowledgment required.");
        if(string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Gateway"))) throw new InvalidOperationException("Local SQL store required.");
        var root=builder.Configuration["Quarantine:PrivateRoot"] ?? throw new InvalidOperationException("Private root required.");
        var absolute=Path.GetFullPath(root);var webRoot=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,"wwwroot"));
        if(absolute.StartsWith(webRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || absolute==webRoot)
            throw new InvalidOperationException("Quarantine must be outside web root.");
        builder.Services.AddSingleton<IQuarantineObjects>(new LocalQuarantineObjects(absolute));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<SqlQuarantineIntake>();
        builder.Services.AddScoped<IQuarantineIntake>(s=>s.GetRequiredService<SqlQuarantineIntake>());
    }
    public static void MapQuarantine(this WebApplication app)
    {
        var routes=app.MapGroup("/api/documents/{document:guid}/uploads").RequireAuthorization("InternalWorkspaceMember").WithTags("Local quarantine spike");
        routes.MapGet("",(Guid document,HttpContext ctx,CancellationToken ct)=>Call(ctx,
            async (store,actor)=>Results.Ok(await store.List(actor,document,ct)))).Produces<QuarantineVersionView[]>();
        routes.MapPost("",(Guid document,HttpContext ctx,CancellationToken ct)=>Call(ctx,async (store,actor)=>
        {
            if(ctx.Request.QueryString.HasValue || !string.Equals(ctx.Request.ContentType,"application/octet-stream",StringComparison.OrdinalIgnoreCase))
                return Error(400,"UPLOAD_REQUEST_INVALID");
            if(ctx.Request.Headers.Keys.Any(h=>h.StartsWith("X-Upload-",StringComparison.OrdinalIgnoreCase) && !h.Equals("X-Upload-Filename",StringComparison.OrdinalIgnoreCase)))
                return Error(400,"UPLOAD_REQUEST_INVALID");
            if(ctx.Request.ContentLength>QuarantineLimits.MaxBytes) return Error(413,"UPLOAD_TOO_LARGE");
            if(ctx.Request.Headers["Idempotency-Key"].Count!=1 || !Guid.TryParse(ctx.Request.Headers["Idempotency-Key"],out var key)) return Error(400,"UPLOAD_REQUEST_INVALID");
            var filename=ctx.Request.Headers["X-Upload-Filename"];
            if(filename.Count!=1 || ctx.Request.Headers["X-Synthetic-Fixture"].ToString()!="true") return Error(400,"UPLOAD_REQUEST_INVALID");
            string decoded;
            try {decoded=Uri.UnescapeDataString(filename.ToString());} catch(UriFormatException) {return Error(400,"UPLOAD_REQUEST_INVALID");}
            QuarantineLimits.Validate(actor,actor.Principal.TenantId,key,decoded,true);
            var row=await store.Upload(actor,document,ctx.Request.Headers.IfMatch.ToString(),key,decoded,true,ctx.Request.Body,ct);
            return Results.Ok(row);
        })).Accepts<Stream>("application/octet-stream").Produces<QuarantineVersionView>();
    }
    private static async Task<IResult> Call(HttpContext ctx,Func<IQuarantineIntake,OwnerActor,Task<IResult>> run)
    {
        var directory=ctx.RequestServices.GetService<PortalIdentityDirectory>();
        if(directory is null || !MemberWorkspaceAccess.IsAllowed(ctx.User,directory.TenantId)) return Error(403,"INTERNAL_MEMBER_REQUIRED");
        var store=ctx.RequestServices.GetService<IQuarantineIntake>();
        if(store is null) return Error(503,"QUARANTINE_SPIKE_NOT_CONFIGURED");
        var actor=new OwnerActor(new(Guid.Parse(ctx.User.FindFirst("tid")!.Value),Guid.Parse(ctx.User.FindFirst("oid")!.Value)),true);
        ctx.Response.Headers["X-Correlation-ID"]=Guid.NewGuid().ToString("D");
        try {return await run(store,actor);}
        catch(WorkspaceFault e) {return Error(e.Status,e.Message);}
        catch(BadHttpRequestException e) {return Error(e.StatusCode,e.StatusCode==413?"UPLOAD_TOO_LARGE":"UPLOAD_REQUEST_INVALID");}
        catch(Exception e) when(e is IOException or SqlException or DbUpdateException or OperationCanceledException)
        {return Error(503,"UPLOAD_NOT_CONFIRMED_RELOAD_STATUS");}
    }
    private static IResult Error(int status,string code)=>Results.Problem(statusCode:status,title:code,extensions:new Dictionary<string,object?>{["code"]=code});
}
