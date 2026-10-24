using Microsoft.Playwright;
using Xunit;
namespace Kopi.Tests;
public sealed class SiteTests {
    [Fact] public async Task DownloadPageWorksAtDesktopAndMobileWidths() {
        using var pw=await Playwright.CreateAsync();await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});
        var page=await browser.NewPageAsync();var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        await page.SetContentAsync(await File.ReadAllTextAsync(Path.Combine(root,"site/index.html")));
        await page.AddStyleTagAsync(new(){Content=await File.ReadAllTextAsync(Path.Combine(root,"site/style.css"))});
        foreach(var width in new[]{1280,390}) {
            await page.SetViewportSizeAsync(width,900);
            Assert.True(await page.IsVisibleAsync("h1"));Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
            Assert.Equal("https://github.com/MLouchini/Kopi/releases/latest/download/Kopi-Setup.exe",await page.GetAttributeAsync("a.button","href"));
            Directory.CreateDirectory(Path.Combine(root,"artifacts"));await page.ScreenshotAsync(new(){Path=Path.Combine(root,$"artifacts/site-{width}.png"),FullPage=true});
        }
    }
}
