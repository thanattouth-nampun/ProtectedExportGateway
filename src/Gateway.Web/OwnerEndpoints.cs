using System.Security.Claims;
using Gateway.Application;
using Gateway.Domain;
using Gateway.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Web;

public sealed record RequesterOptionDto(Guid ObjectId,string DisplayName);
public static class OwnerEndpoints
{
    public static void AddOwnerWorkspace(this WebApplicationBuilder builder,Guid tenant)
    {
        var identities=builder.Configuration.GetSection("Portal:Identities").Get<ReviewedPortalIdentity[]>() ?? [];
        if (identities.Length>0) builder.Services.AddSingleton(new PortalIdentityDirectory(tenant,identities));
        var connection=builder.Configuration.GetConnectionString("Gateway");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            if (identities.Length==0) throw new InvalidOperationException("A reviewed portal identity directory is required for the document store.");
            var options=new DbContextOptionsBuilder<GatewayDbContext>().UseSqlServer(connection,sql=>sql.UseCompatibilityLevel(160)).Options;
            builder.Services.AddSingleton(options);
            builder.Services.AddScoped<IOwnerWorkspace,SqlOwnerWorkspace>();
        }
        // No automatic database migration, tenant change, source intake or Ready writer.
    }
    public static void MapOwnerWorkspace(this WebApplication app)
    {
        var group=app.MapGroup("/api").RequireAuthorization("InternalWorkspaceMember").WithTags("Own documents");
        group.MapGet("/document-requester-options",(HttpContext ctx)=>
        {
            var directory=ctx.RequestServices.GetService<PortalIdentityDirectory>();
            if (directory is null) return Problem(503,"PORTAL_DIRECTORY_UNAVAILABLE");
            if (!TryActor(ctx,directory,out _)) return Problem(403,"INTERNAL_MEMBER_REQUIRED");
            return Results.Ok(directory.Options.Select(x=>new RequesterOptionDto(x.ObjectId,x.DisplayName)).ToArray());
        }).Produces<RequesterOptionDto[]>();
        group.MapGet("/my/documents",(HttpContext ctx,int? skip,CancellationToken ct)=>Call(ctx,
            async (store,actor,_)=>Results.Ok(await store.List(actor,skip ?? 0,ct)))).Produces<OwnerDocumentPage>();
        group.MapGet("/documents/{id:guid}",(Guid id,HttpContext ctx,CancellationToken ct)=>Call(ctx,
            async (store,actor,_)=>Respond(ctx,await store.Find(actor,id,ct)))).Produces<OwnerDocumentView>();
        group.MapPost("/documents",(DraftEdit edit,HttpContext ctx,CancellationToken ct)=>Call(ctx,
            async (store,actor,correlation)=>{
                var doc=await store.Create(actor,edit,correlation,ct);
                ctx.Response.Headers.ETag=doc.ETag;
                return Results.Created($"/api/documents/{doc.Id:D}",doc);
            })).Produces<OwnerDocumentView>(201);
        group.MapPatch("/documents/{id:guid}",(Guid id,DraftEdit edit,HttpContext ctx,CancellationToken ct)=>Call(ctx,
            async (store,actor,correlation)=>Respond(ctx,await store.Update(actor,id,ctx.Request.Headers.IfMatch.ToString(),edit,correlation,ct))))
            .Produces<OwnerDocumentView>();
        group.MapGet("/documents/{id:guid}/audit",(Guid id,HttpContext ctx,CancellationToken ct)=>Call(ctx,
            async (store,actor,_)=>Results.Ok(await store.Audit(actor,id,ct)))).Produces<OwnerAuditView[]>();
    }
    private static IResult Respond(HttpContext ctx,OwnerDocumentView doc)
    { ctx.Response.Headers.ETag=doc.ETag; return Results.Ok(doc); }
    private static bool TryActor(HttpContext ctx,PortalIdentityDirectory directory,out OwnerActor actor)
    {
        actor=new(new(Guid.Empty,Guid.Empty),false);
        if (!MemberWorkspaceAccess.IsAllowed(ctx.User,directory.TenantId)) return false;
        actor=new(new(Guid.Parse(ctx.User.FindFirstValue("tid")!),Guid.Parse(ctx.User.FindFirstValue("oid")!)),true);
        return true;
    }
    private static async Task<IResult> Call(HttpContext ctx,Func<IOwnerWorkspace,OwnerActor,Guid,Task<IResult>> action)
    {
        var correlation=Guid.NewGuid(); ctx.Response.Headers["X-Correlation-ID"]=correlation.ToString("D");
        var directory=ctx.RequestServices.GetService<PortalIdentityDirectory>();
        if (directory is null) return Problem(503,"PORTAL_DIRECTORY_UNAVAILABLE");
        if (!TryActor(ctx,directory,out var actor)) return Problem(403,"INTERNAL_MEMBER_REQUIRED");
        var store=ctx.RequestServices.GetService<IOwnerWorkspace>();
        if (store is null) return Problem(503,"DOCUMENT_STORE_UNAVAILABLE");
        try { return await action(store,actor,correlation); }
        catch (WorkspaceFault e) { return Problem(e.Status,e.Message); }
        catch (SqlException) { return Problem(503,"DOCUMENT_STORE_UNAVAILABLE"); }
        catch (DbUpdateException) { return Problem(503,"DOCUMENT_STORE_UNAVAILABLE"); }
    }
    private static IResult Problem(int status,string code)=>Results.Problem(statusCode:status,title:code,
        extensions:new Dictionary<string,object?> { ["code"]=code });
}
