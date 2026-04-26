using Xunit;

namespace HVTravel.Web.Tests;

public class EncodingRegressionTests
{
    private static readonly string[] SuspiciousMarkers =
    [
        "\u00C3",
        "\u00C2",
        "\u00E1\u00BB",
        "\u00E2\u201A",
        "\u00E2\u20AC\u00A2"
    ];

    [Fact]
    public void SelectedUiFiles_DoNotContain_MojibakeMarkers()
    {
        var scannedFiles = new[]
        {
            Path.Combine("HV-Travel.Web", "Areas", "Admin", "Views", "Customers", "Index.cshtml"),
            Path.Combine("HV-Travel.Web", "Areas", "Admin", "Views", "Payments", "Index.cshtml"),
            Path.Combine("HV-Travel.Web", "Areas", "Admin", "Views", "Reviews", "Index.cshtml"),
            Path.Combine("HV-Travel.Web", "Views", "Home", "About.cshtml"),
            Path.Combine("HV-Travel.Web", "Views", "Home", "Contact.cshtml")
        };

        var excludedFiles = new[]
        {
            Path.Combine("HV-Travel.Domain", "Utils", "TextEncodingRepair.cs"),
            Path.Combine("HV-Travel.Web.Tests", "MeilisearchSystemSearchTests.cs"),
            Path.Combine("HV-Travel.Web.Tests", "RouteIntelligencePhase3Tests.cs")
        };

        foreach (var excludedFile in excludedFiles)
        {
            Assert.DoesNotContain(excludedFile, scannedFiles, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var relativePath in scannedFiles)
        {
            var content = TestPaths.ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            foreach (var marker in SuspiciousMarkers)
            {
                Assert.DoesNotContain(
                    marker,
                    content,
                    StringComparison.Ordinal);
            }
        }
    }
}
