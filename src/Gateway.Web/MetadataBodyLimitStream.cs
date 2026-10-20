namespace Gateway.Web;

// Bounds actual reads even when Content-Length is absent and when TestServer has no Kestrel feature.
internal sealed class MetadataBodyLimitStream(Stream inner,long limit):Stream
{
    private long seen;
    private int Allowed(int requested)=>(int)Math.Min(requested,Math.Max(1,limit-seen+1));
    private int Count(int read)
    {
        seen+=read;
        if (seen>limit) throw new BadHttpRequestException("METADATA_TOO_LARGE",413);
        return read;
    }
    public override int Read(byte[] buffer,int offset,int count)=>Count(inner.Read(buffer,offset,Allowed(count)));
    public override int Read(Span<byte> buffer)=>Count(inner.Read(buffer[..Allowed(buffer.Length)]));
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)
        =>Count(await inner.ReadAsync(buffer[..Allowed(buffer.Length)],cancellationToken));
    public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken cancellationToken)
        =>ReadAsync(buffer.AsMemory(offset,count),cancellationToken).AsTask();
    public override bool CanRead=>inner.CanRead;
    public override bool CanSeek=>false;
    public override bool CanWrite=>false;
    public override long Length=>throw new NotSupportedException();
    public override long Position {get=>seen;set=>throw new NotSupportedException();}
    public override void Flush() { }
    public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
    public override void SetLength(long value)=>throw new NotSupportedException();
    public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
}
