using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kopi.Core;
public sealed record BrowserCommand(string Id, string Kind, object Payload);
public interface IBrowserSession { Task<string> Send(string kind, object payload, CancellationToken ct); }
public sealed class BrowserBridge : IBrowserSession, IDisposable {
    WebApplication? app;
    readonly ConcurrentQueue<BrowserCommand> queue = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource<string>> pending = new();
    string token = "", origin = "";
    public string PairCode { get; private set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
    public bool Observing { get; set; }
    public bool Suggestions { get; set; }
    public DateTime LastSeen { get; private set; }
    public event Action<Evidence>? Received;
    public void Start() {
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(o=>o.Limits.MaxRequestBodySize=100000).UseUrls("http://127.0.0.1:47653");
        app=builder.Build();app.Run(Handle);app.StartAsync().GetAwaiter().GetResult();
    }
    async Task Handle(HttpContext c) {
        try {
            var o = c.Request.Headers.Origin.ToString();
            if (!o.StartsWith("chrome-extension://", StringComparison.Ordinal) || o.Length != 51) { c.Response.StatusCode = 403; return; }
            c.Response.Headers.AccessControlAllowOrigin = o;
            c.Response.Headers.AccessControlAllowHeaders = "Authorization,Content-Type,X-Kopi-Pair";
            c.Response.Headers.AccessControlAllowMethods = "POST,OPTIONS";
            if (c.Request.Method == "OPTIONS") return;
            if (c.Request.Method != "POST") { c.Response.StatusCode = 405; return; }
            if (c.Request.ContentLength is null or > 100000) { c.Response.StatusCode = 413; return; }
            if (c.Request.Path == "/pair") {
                if (c.Request.Headers["X-Kopi-Pair"] != PairCode) { c.Response.StatusCode = 401; return; }
                token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));origin=o;PairCode=Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
                await Reply(c,new{token});return;
            }
            if(token.Length==0||o!=origin||c.Request.Headers.Authorization!="Bearer "+token){c.Response.StatusCode=401;return;}
            LastSeen=DateTime.UtcNow;
            using var reader=new StreamReader(c.Request.Body);var body=await reader.ReadToEndAsync(c.RequestAborted);
            switch(c.Request.Path.Value) {
                case "/ping": await Reply(c,new{ok=true});break;
                case "/poll":
                    BrowserCommand? command=null;
                    while(queue.TryDequeue(out var candidate)){if(pending.ContainsKey(candidate.Id)){command=candidate;break;}}
                    await Reply(c,new{observing=Observing,suggestions=Suggestions,command});break;
                case "/event":
                    if(Observing||Suggestions)Received?.Invoke(Privacy.Sanitize(Json.Read<Evidence>(body)));
                    await Reply(c,new{ok=true});break;
                case "/result":
                    var r=Json.Read<Dictionary<string,string>>(body);
                    if(pending.TryRemove(r["id"],out var tcs)) {
                        if(r.TryGetValue("error",out var error)&&error.Length>0)tcs.TrySetException(new InvalidOperationException(error));
                        else tcs.TrySetResult(r.GetValueOrDefault("result","{}"));
                    }
                    await Reply(c,new{ok=true});break;
                default:c.Response.StatusCode=404;break;
            }
        }catch(Exception ex) when(ex is not OutOfMemoryException){c.Response.StatusCode=400;}
    }
    static Task Reply(HttpContext c,object value) {c.Response.ContentType="application/json";return c.Response.WriteAsync(Json.Write(value));}
    public async Task<string> Send(string kind,object payload,CancellationToken ct) {
        if(DateTime.UtcNow-LastSeen>TimeSpan.FromSeconds(10))throw new InvalidOperationException("Connect the Kopi extension in your browser first.");
        var id=Guid.NewGuid().ToString("N");var tcs=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id]=tcs;queue.Enqueue(new(id,kind,payload));
        try{return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(90),ct);}finally{pending.TryRemove(id,out _);}
    }
    public void Dispose(){foreach(var t in pending.Values)t.TrySetCanceled();if(app!=null){app.StopAsync().GetAwaiter().GetResult();app.DisposeAsync().AsTask().GetAwaiter().GetResult();}}
}
