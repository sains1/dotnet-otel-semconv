using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

namespace OtelSemConvAnalyzer.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    public void DemonstrateAnalyzer()
    {
        using var activity = Otel.ActivitySource.StartActivity();

        // ====== INVALID ATTRIBUTE ERRORS (OTEL0001) ======

        // ERROR: Unknown attribute name
        activity?.SetTag("invalid.attribute.name", "value");

        // ERROR: Typo in attribute name
        activity?.SetTag("myapp.activity_log.id", "123"); // underscore instead of dot

        // ERROR: Using Activity.Current
        Activity.Current?.SetTag("unknown.attr", 1);

        // ====== TYPE MISMATCH ERRORS (OTEL0002) ======

        // ERROR: Expecting int, got string
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, "123");

        // ERROR: Expecting string, got int
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ID, 123);

        // ERROR: Expecting double, got string
        activity?.SetTag(OtelAttributes.APP_JANK_PERIOD, "1.5");

        // ====== EXPERIMENTAL ATTRIBUTE WARNINGS (OTEL0003) ======

        // INFO: Experimental attribute (stability: experimental)
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_EXPERIMENTAL_ID, "exp-123");

        // ====== DEPRECATED ATTRIBUTE WARNINGS (OTEL0004) ======

        // WARNING: Deprecated attribute
        activity?.SetTag(OtelAttributes.ANDROID_STATE, "created");

        // ====== VALID USAGE (No warnings) ======

        // ✓ Correct type: string
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ID, "log-550e8400");

        // ✓ Correct type: int
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, 42);

        // ✓ Correct type with numeric conversion: long for int
        activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, 100L);

        // ✓ Correct type: double
        activity?.SetTag(OtelAttributes.APP_JANK_PERIOD, 1.5);

        // ✓ Correct type with float to double conversion
        activity?.SetTag(OtelAttributes.APP_JANK_PERIOD, 2.5f);

        // ✓ Correct type: boolean
        activity?.SetTag(OtelAttributes.ASPNETCORE_REQUEST_IS_UNHANDLED, true);

        // ✓ Using string literal (works if attribute exists)
        activity?.SetTag("myapp.activity_log.id", "log-123");

        // ✓ Standard OTel attributes
        activity?.SetTag(OtelAttributes.CLIENT_ADDRESS, "192.168.1.1");
        activity?.SetTag(OtelAttributes.CLIENT_PORT, 8080);
    }
}