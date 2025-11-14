using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

namespace OtelSemConvAnalyzer.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    public void ToStars()
    {
        using var activity = Otel.ActivitySource.StartActivity();
        
        // 1. set attribute on Activity.Current
        Activity.Current?.SetTag("not_allowed", 1);
        
        // 2. set attribute on Activity from ActivitySource
        activity?.SetTag("not_allowed", 1);
        
        // 3. use wrong type on a known attribute
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, "bad type");
    }
}