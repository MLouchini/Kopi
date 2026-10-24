using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Kopi.Core;
using Xunit;
namespace Kopi.Tests;
public sealed class DownloadTests {
    sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)=>Task.FromResult(respond(r));
    }
    [Theory][InlineData(true)][InlineData(false)]public async Task ResumesOrSafelyRestartsAndVerifies(bool supportsRange){
        var bytes=Enumerable.Range(0,10000).Select(i=>(byte)i).ToArray();var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        await File.WriteAllBytesAsync(path+".download",bytes[..4000]);
        using var client=new HttpClient(new Handler(r=>{
            Assert.Equal(4000,r.Headers.Range!.Ranges.Single().From);
            var response=new HttpResponseMessage(supportsRange?HttpStatusCode.PartialContent:HttpStatusCode.OK){Content=new ByteArrayContent(supportsRange?bytes[4000..]:bytes)};
            if(supportsRange)response.Content.Headers.ContentRange=new ContentRangeHeaderValue(4000,9999,10000);return response;
        }));
        try{await VerifiedDownload.Fetch(client,new("https://model.test/model",Convert.ToHexString(SHA256.HashData(bytes)),"model"),path,new Progress<string>(),default);Assert.Equal(bytes,await File.ReadAllBytesAsync(path));Assert.False(File.Exists(path+".download"));}finally{File.Delete(path);File.Delete(path+".download");}
    }
    [Fact]public async Task CorruptionIsRejected(){
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());using var client=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent("corrupt")}));
        await Assert.ThrowsAsync<InvalidDataException>(()=>VerifiedDownload.Fetch(client,new("https://model.test/model",new string('0',64),"model"),path,new Progress<string>(),default));Assert.False(File.Exists(path));Assert.False(File.Exists(path+".download"));
    }
}
