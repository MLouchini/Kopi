using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Kopi.Core;
public static class VerifiedDownload {
    public static async Task<string> Hash(string path, CancellationToken ct) {
        await using var stream=File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream,ct));
    }
    public static async Task Fetch(HttpClient client,DownloadAsset asset,string path,IProgress<string> progress,CancellationToken ct) {
        if(!Uri.TryCreate(asset.Url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||asset.Sha256.Length!=64||!asset.Sha256.All(Uri.IsHexDigit))throw new InvalidDataException("Download requires HTTPS and a valid SHA-256 digest.");
        var expected=asset.Sha256.ToUpperInvariant();
        if(File.Exists(path)&&await Hash(path,ct)==expected){progress.Report($"Verified cached {asset.FileName}");return;}
        var partial=path+".download";
        for(int attempt=0;attempt<3;attempt++) {
            ct.ThrowIfCancellationRequested();
            var offset=File.Exists(partial)?new FileInfo(partial).Length:0;
            try {
                using var request=new HttpRequestMessage(HttpMethod.Get,uri);
                if(offset>0)request.Headers.Range=new RangeHeaderValue(offset,null);
                using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
                if(response.StatusCode==HttpStatusCode.RequestedRangeNotSatisfiable){if(File.Exists(partial)&&await Hash(partial,ct)==expected){File.Move(partial,path,true);return;}File.Delete(partial);continue;}
                response.EnsureSuccessStatusCode();
                var resumed=response.StatusCode==HttpStatusCode.PartialContent;
                if(resumed&&response.Content.Headers.ContentRange?.From!=offset)throw new InvalidDataException("Server returned an invalid download range.");
                if(!resumed)offset=0; // A server ignoring Range must replace, never append.
                var total=response.Content.Headers.ContentRange?.Length??response.Content.Headers.ContentLength+offset;
                await using(var input=await response.Content.ReadAsStreamAsync(ct))
                await using(var output=new FileStream(partial,offset>0?FileMode.Append:FileMode.Create,FileAccess.Write,FileShare.None)) {
                    var buffer=new byte[1024*1024];long downloaded=offset;int count;
                    while((count=await input.ReadAsync(buffer,ct))>0){await output.WriteAsync(buffer.AsMemory(0,count),ct);downloaded+=count;progress.Report($"{asset.FileName}: {downloaded/1048576} / {(total.HasValue?total/1048576:0)} MB{(offset>0?" · resumed":"")}");}
                }
                progress.Report($"Checking integrity of {asset.FileName}…");
                if(await Hash(partial,ct)!=expected){File.Delete(partial);throw new InvalidDataException("Download checksum mismatch. File rejected; no unverified model will run.");}
                File.Move(partial,path,true);return;
            }catch(Exception ex) when(attempt<2&&!ct.IsCancellationRequested&&(ex is HttpRequestException||ex is IOException&&ex is not InvalidDataException)) {
                progress.Report($"Connection interrupted. Resuming download (attempt {attempt+2} of 3)…");await Task.Delay(500*(attempt+1),ct);
            }
        }
        throw new IOException("Download could not complete. Try again; partial progress is retained, and the complete file must pass its checksum before use.");
    }
}
