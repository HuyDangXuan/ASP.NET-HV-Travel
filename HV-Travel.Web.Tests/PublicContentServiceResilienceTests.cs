namespace HV_Travel.Web.Tests;

public class PublicContentServiceResilienceTests
{
    [Fact]
    public void PublicContentService_UsesNullSafeMarkerIteration_ForMojibakeDetection()
    {
        var path = GetRepoPath(@"HV-Travel.Web\Services\PublicContentService.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("MojibakeMarkers ?? Array.Empty<string>()", content);
        Assert.DoesNotContain("return MojibakeMarkers.Any(", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}