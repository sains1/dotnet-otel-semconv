# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository demonstrates OpenTelemetry semantic conventions in .NET using:
- **Roslyn Analyzer** (primary focus): Enforces use of semantic conventions at compile-time
- **Weaver**: Code generation tool for semantic conventions from YAML definitions

## Roslyn Analyzer Architecture

The analyzer project (`src/OtelSemConvAnalyzer/`) targets `netstandard2.0` with key properties:
- `EnforceExtendedAnalyzerRules: true`
- `IsRoslynComponent: true`
- Uses Microsoft.CodeAnalysis.CSharp 4.14.0

### Analyzer Components

**DiagnosticAnalyzer types:**
- Syntax-based analyzers (`RegisterSyntaxNodeAction`): Traverse syntax tree without semantic model
- Semantic-based analyzers (`RegisterOperationAction`): Require compilation, access semantic model for type info

**Required patterns:**
- Call `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`
- Call `context.EnableConcurrentExecution()`
- Define `DiagnosticDescriptor` with ID (format: PREFIX+NUMBER, e.g. AB0001), title, message, category, severity
- List all rules in `SupportedDiagnostics`

**CodeFixProvider:**
- Link to analyzer via `FixableDiagnosticIds`
- Use `[ExportCodeFixProvider]` and `[Shared]` attributes
- Return `FixAllProvider` or null
- Register code actions in `RegisterCodeFixesAsync`

### Key References

Resources are localized via `Resources.resx` → `Resources.Designer.cs`

Sample project references analyzer with:
```xml
<ProjectReference Include="..\..\src\OtelSemConvAnalyzer\OtelSemConvAnalyzer.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
```

## Building & Testing

**Build solution:**
```bash
dotnet build otel-semconv-dotnet.sln
```

**Run all tests:**
```bash
dotnet test
```

**Run specific test:**
```bash
dotnet test --filter FullyQualifiedName~ClassName.TestMethodName
```

**Test project uses:**
- xUnit
- Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit
- Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit
- Target framework: net10.0

**Test pattern:**
```csharp
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<YourAnalyzer>;

var expected = Verifier.Diagnostic()
    .WithLocation(line, column)
    .WithArguments("arg");
await Verifier.VerifyAnalyzerAsync(sourceCode, expected);
```

## Weaver Code Generation

Weaver generates semantic convention code from YAML definitions.

**Install weaver:**
```bash
cd otel-semconv-generator && make install
```

**Generate conventions:**
```bash
cd otel-semconv-generator && make generate
```

This:
1. Copies `custom-registry/*.yaml` to `semantic-conventions/model/`
2. Runs `weaver registry generate` using templates in `templates/registry/`
3. Outputs to `output/OtelAttributes.cs`
4. Copies to `sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs`

**Template location:** `otel-semconv-generator/templates/registry/csharp/`

## Project Structure

```
src/OtelSemConvAnalyzer/          # Analyzer implementation
tests/OtelSemConvAnalyzer.Tests/  # Analyzer tests
sample/OtelSemConvAnalyzer.Sample/ # Sample project with analyzer referenced
otel-semconv-generator/           # Weaver templates and configs
  custom-registry/                # Custom YAML semconv definitions
  templates/registry/csharp/      # Jinja2 templates for code gen
  semantic-conventions/           # Cloned OTel semconv repo
```
