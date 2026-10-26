using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using Gateway.Application;

namespace Gateway.Infrastructure.Persistence;

// Development-only create-only encrypted-envelope spool, not an Azure Blob adapter or scanner.
// The operator owns root ACLs. Refuse reparse ancestors; no caller-supplied path reaches this API.
public sealed class LocalQuarantineObjects:IQuarantineObjects
{
    private readonly string root;
    public LocalQuarantineObjects(string root)
    {
        this.root=Path.GetFullPath(root);
        if (!Directory.Exists(this.root)) throw new InvalidOperationException("QUARANTINE_ROOT_MUST_EXIST_WITH_PRIVATE_ACL");
        CheckRoot();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Local quarantine requires reviewed Windows private ACLs.");
        var acl=new DirectoryInfo(this.root).GetAccessControl();
        var current=WindowsIdentity.GetCurrent().User?.Value ?? throw new IOException("QUARANTINE_IDENTITY_UNKNOWN");
        if(!acl.AreAccessRulesProtected) throw new IOException("QUARANTINE_ACL_NOT_PRIVATE");
        foreach(FileSystemAccessRule rule in acl.GetAccessRules(true,true,typeof(SecurityIdentifier)))
            if(rule.AccessControlType==AccessControlType.Allow && rule.IdentityReference.Value!=current && rule.IdentityReference.Value is not ("S-1-5-18" or "S-1-5-32-544"))
                throw new IOException("QUARANTINE_ACL_NOT_PRIVATE");
    }
    private void CheckRoot()
    {
        for(var item=new DirectoryInfo(root);item is not null;item=item.Parent)
            if ((item.Attributes&FileAttributes.ReparsePoint)!=0) throw new IOException("QUARANTINE_REPARSE_ROOT");
    }
    private string PathFor(Guid token,string extension)
    {
        CheckRoot();if (token==Guid.Empty) throw new ArgumentException("Invalid private token");
        var path=Path.Combine(root,token.ToString("N")+extension);
        if (File.Exists(path) && (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0) throw new IOException("QUARANTINE_REPARSE_OBJECT");
        return path;
    }
    public async Task<StoredQuarantine> Write(Guid token,Stream input,CancellationToken ct)
    {
        var part=PathFor(token,".part");var final=PathFor(token,".blob");
        var created=false;
        try
        {
            long length=0;using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using(var output=new FileStream(part,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,FileOptions.Asynchronous))
            {
                created=true;var buffer=new byte[65536];int count;
                while((count=await input.ReadAsync(buffer.AsMemory(0,(int)Math.Min(buffer.Length,QuarantineLimits.MaxBytes-length+1)),ct))!=0)
                {
                    length+=count;if(length>QuarantineLimits.MaxBytes) throw new WorkspaceFault(413,"UPLOAD_TOO_LARGE");
                    hash.AppendData(buffer,0,count);await output.WriteAsync(buffer.AsMemory(0,count),ct);
                }
                if(length==0) throw new WorkspaceFault(400,"UPLOAD_EMPTY");
                await output.FlushAsync(ct);output.Flush(true);
            }
            File.Move(part,final,false); // Unique create-only key, never overwrite a bound candidate.
            return new(token,length,Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally {if(created && File.Exists(part)) File.Delete(part);}
    }
    public async Task Verify(StoredQuarantine stored,CancellationToken ct)
    {
        await using var file=new FileStream(PathFor(stored.Token,".blob"),FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.Asynchronous);
        if(file.Length!=stored.Bytes || Convert.ToHexString(await SHA256.HashDataAsync(file,ct))!=stored.Sha256)
            throw new IOException("QUARANTINE_BYTES_CHANGED");
    }
    // Internal operator probe only. Keep this handle open through SDK readback and final hash checks.
    public FileStream OpenInspection(Guid token) => new(PathFor(token,".blob"),FileMode.Open,
        FileAccess.Read,FileShare.Read,65536,FileOptions.Asynchronous);
    public Task Delete(Guid token,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();File.Delete(PathFor(token,".part"));File.Delete(PathFor(token,".blob"));return Task.CompletedTask;
    }
    public IEnumerable<(Guid Token,DateTimeOffset ModifiedAt)> Candidates(int maximum)
    {
        CheckRoot();if(maximum is <1 or >1000) throw new ArgumentOutOfRangeException(nameof(maximum));
        return Directory.EnumerateFiles(root).Take(maximum).Select(path=>new FileInfo(path))
            .Where(f=>(f.Extension is ".part" or ".blob") && (f.Attributes&FileAttributes.ReparsePoint)==0 && Guid.TryParseExact(Path.GetFileNameWithoutExtension(f.Name),"N",out _))
            .Select(f=>(Guid.ParseExact(Path.GetFileNameWithoutExtension(f.Name),"N"),(DateTimeOffset)f.LastWriteTimeUtc)).ToArray();
    }
}
