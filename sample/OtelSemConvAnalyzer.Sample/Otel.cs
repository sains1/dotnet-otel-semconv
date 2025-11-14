using System.Diagnostics;

namespace OtelSemConvAnalyzer.Sample;

public static class Otel
{
    public static readonly ActivitySource ActivitySource = new("OtelSemConvAnalyzer.Sample");
}