using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace OtelSemConvAnalyzer.Tests;

public class OtelSemanticConventionAnalyzerTests
{
    #region OTEL0001: Invalid Attribute Name

    [Fact]
    public async Task InvalidAttribute_ReportsError()
    {
        var code = @"
using System.Diagnostics;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(""invalid.attribute.name"", 123);
    }
}";

        var expected = DiagnosticResult
            .CompilerError(OtelSemanticConventionAnalyzer.InvalidAttributeId)
            .WithSpan(9, 25, 9, 49)
            .WithArguments("invalid.attribute.name");

        await TestHelper.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task InvalidAttribute_WithActivityCurrent_ReportsError()
    {
        var code = @"
using System.Diagnostics;

class Test
{
    void Method()
    {
        Activity.Current?.SetTag(""unknown.attr"", 1);
    }
}";

        var expected = DiagnosticResult
            .CompilerError(OtelSemanticConventionAnalyzer.InvalidAttributeId)
            .WithSpan(8, 34, 8, 48)
            .WithArguments("unknown.attr");

        await TestHelper.VerifyAnalyzerAsync(code, expected);
    }

    #endregion

    #region OTEL0002: Type Mismatch

    [Fact]
    public async Task TypeMismatch_IntExpected_StringProvided_ReportsError()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, ""123"");
    }
}";

        var expected = DiagnosticResult
            .CompilerError(OtelSemanticConventionAnalyzer.TypeMismatchId)
            .WithSpan(10, 72, 10, 77)
            .WithArguments("myapp.activity_log.entry.count", "int", "string");

        await TestHelper.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task TypeMismatch_StringExpected_IntProvided_ReportsError()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ID, 123);
    }
}";

        var expected = DiagnosticResult
            .CompilerError(OtelSemanticConventionAnalyzer.TypeMismatchId)
            .WithSpan(10, 63, 10, 66)
            .WithArguments("myapp.activity_log.id", "string", "int");

        await TestHelper.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task TypeMismatch_DoubleExpected_StringProvided_ReportsError()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.APP_JANK_PERIOD, ""1.5"");
    }
}";

        var experimental = new DiagnosticResult(OtelSemanticConventionAnalyzer.ExperimentalAttributeId, DiagnosticSeverity.Info)
            .WithSpan(10, 25, 10, 55)
            .WithArguments("app.jank.period");

        var typeMismatch = DiagnosticResult
            .CompilerError(OtelSemanticConventionAnalyzer.TypeMismatchId)
            .WithSpan(10, 57, 10, 62)
            .WithArguments("app.jank.period", "double", "string");

        await TestHelper.VerifyAnalyzerAsync(code, experimental, typeMismatch);
    }

    #endregion

    #region OTEL0003: Experimental Attribute

    [Fact]
    public async Task ExperimentalAttribute_ReportsInfo()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_EXPERIMENTAL_ID, ""exp-123"");
    }
}";

        var expected = new DiagnosticResult(OtelSemanticConventionAnalyzer.ExperimentalAttributeId, DiagnosticSeverity.Info)
            .WithSpan(10, 25, 10, 74)
            .WithArguments("myapp.activity_log.experimental_id");

        await TestHelper.VerifyAnalyzerAsync(code, expected);
    }

    #endregion

    #region OTEL0004: Deprecated Attribute

    [Fact]
    public async Task DeprecatedAttribute_ReportsWarning()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.ANDROID_STATE, ""created"");
    }
}";

        var experimental = new DiagnosticResult(OtelSemanticConventionAnalyzer.ExperimentalAttributeId, DiagnosticSeverity.Info)
            .WithSpan(10, 25, 10, 53)
            .WithArguments("android.state");

        var deprecated = DiagnosticResult
            .CompilerWarning(OtelSemanticConventionAnalyzer.DeprecatedAttributeId)
            .WithSpan(10, 25, 10, 53)
            .WithArguments("android.state", "Replaced by android.app.state");

        await TestHelper.VerifyAnalyzerAsync(code, experimental, deprecated);
    }

    #endregion

    #region Valid Usage (No Diagnostics)

    [Fact]
    public async Task ValidUsage_StringAttribute_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ID, ""log-123"");
    }
}";

        await TestHelper.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ValidUsage_IntAttribute_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, 42);
    }
}";

        await TestHelper.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ValidUsage_LongForInt_NumericConversion_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, 100L);
    }
}";

        await TestHelper.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ValidUsage_FloatForDouble_NumericConversion_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.APP_JANK_PERIOD, 1.5f);
    }
}";

        // APP_JANK_PERIOD has development stability, so it will report experimental
        var experimental = new DiagnosticResult(OtelSemanticConventionAnalyzer.ExperimentalAttributeId, DiagnosticSeverity.Info)
            .WithSpan(10, 25, 10, 55)
            .WithArguments("app.jank.period");

        await TestHelper.VerifyAnalyzerAsync(code, experimental);
    }

    [Fact]
    public async Task ValidUsage_BooleanAttribute_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(OtelAttributes.ASPNETCORE_REQUEST_IS_UNHANDLED, true);
    }
}";

        await TestHelper.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ValidUsage_StringLiteral_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.SetTag(""myapp.activity_log.id"", ""log-123"");
    }
}";

        await TestHelper.VerifyAnalyzerAsync(code);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task VariableKey_SkipsAnalysis_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        string key = ""myapp.activity_log.id"";
        activity.SetTag(key, ""log-123"");
    }
}";

        // Should not analyze variable keys (too complex)
        await TestHelper.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NonTargetMethod_SkipsAnalysis_NoDiagnostic()
    {
        var code = @"
using System.Diagnostics;

class Test
{
    void Method()
    {
        var activity = new Activity(""test"");
        activity.AddEvent(new ActivityEvent(""event""));
    }
}";

        // AddEvent is not a target method for attribute checking
        await TestHelper.VerifyAnalyzerAsync(code);
    }

    #endregion
}
