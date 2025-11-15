using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace OtelSemConvAnalyzer.Tests;

internal static class TestHelper
{
    // OtelAttributes source for tests
    public const string OtelAttributesSource = @"
namespace OpenTelemetry.SemanticConventions
{
    public static class OtelAttributes
    {
        public const string MYAPP_ACTIVITY_LOG_ID = ""myapp.activity_log.id"";
        public const string MYAPP_ACTIVITY_LOG_ENTRY_COUNT = ""myapp.activity_log.entry.count"";
        public const string MYAPP_ACTIVITY_LOG_EXPERIMENTAL_ID = ""myapp.activity_log.experimental_id"";
        public const string ANDROID_STATE = ""android.state"";
        public const string APP_JANK_PERIOD = ""app.jank.period"";
        public const string ASPNETCORE_REQUEST_IS_UNHANDLED = ""aspnetcore.request.is_unhandled"";
        public const string CLIENT_ADDRESS = ""client.address"";
        public const string CLIENT_PORT = ""client.port"";
    }
}";

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<OtelSemanticConventionAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80, // Use .NET 8.0 which has Activity.SetTag
        };

        // Add OtelAttributes source
        test.TestState.Sources.Add(OtelAttributesSource);

        // Add metadata JSON as AdditionalFile
        test.TestState.AdditionalFiles.Add(("otel-attributes-metadata.json", System.IO.File.ReadAllText("test-metadata.json")));

        // Add expected diagnostics
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
