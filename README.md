# OpenTelemetry Semantic Conventions Roslyn Analyzer for .NET

A Roslyn analyzer that enforces OpenTelemetry semantic conventions at compile-time for .NET projects. This analyzer validates attribute names and types when using `Activity.SetTag()`, `Activity.AddTag()`, and `Span.SetAttribute()` methods.

## Diagnostic Rules

| Rule ID    | Description            | Default Severity |
| ---------- | ---------------------- | ---------------- |
| `OTEL0001` | Invalid attribute name | Error            |
| `OTEL0002` | Type mismatch          | Error            |
| `OTEL0003` | Experimental attribute | Info             |
| `OTEL0004` | Deprecated attribute   | Warning          |

## Getting Started

### 1. Install Nuget package

Install the analyzer via NuGet:

```bash
dotnet add package sains1.OtelSemConvAnalyzer
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="sains1.OtelSemConvAnalyzer" Version="1.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### 2. Generate semantic conventions output

We use [weaver](https://github.com/open-telemetry/weaver) for code generation to output our semantic convention files.

In [./otel-semconv-generator/](./otel-semconv-generator/) is our example weaver setup.

- 2.1 Clone the contents of `./otel-semconv-generator/` to your project

- 2.2 Within `./otel-semconv-generator/` clone the semantic conventions repo

```
cd ./otel-semconv-generator/
git clone https://github.com/open-telemetry/semantic-conventions
```

- 2.3 Run the code generation

```
make generate
```

This generates:

- `OtelAttributes.cs` - Constants for use in your code
- `otel-attributes-metadata.json` - Metadata for the analyzer

`OtellAttributes` should be copied into your project and used as follows:

```cs
using var activity = Otel.ActivitySource.StartActivity();
activity?.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, 123);
```

`otel-attributes-metadata.json` should be copied into your project and is used by the analyzer

### 3. Configure the analyzer metadata file

Copy the metadata file into your project and add the following:

```csproj

  <ItemGroup>
    <!-- Include semantic conventions metadata for analyzer -->
    <AdditionalFiles Include="otel-attributes-metadata.json" />
  </ItemGroup>
```

### 4. Optionally configure the analyzer severity within your .editorconfig

```.editorconfig
# OpenTelemetry Semantic Convention Analyzer Rules

# OTEL0001: Invalid attribute name
# Severity: error (default), warning, suggestion, silent, none
dotnet_diagnostic.OTEL0001.severity = error

# OTEL0002: Type mismatch
# Severity: error (default), warning, suggestion, silent, none
dotnet_diagnostic.OTEL0002.severity = error

# OTEL0003: Experimental attribute usage
# Severity: suggestion (default), error, warning, silent, none
dotnet_diagnostic.OTEL0003.severity = suggestion

# OTEL0004: Deprecated attribute usage
# Severity: warning (default), error, suggestion, silent, none
dotnet_diagnostic.OTEL0004.severity = warning
```

After a build you should see any warnings/errors directly in your IDE and in the build output:

![error example](./docs/Screenshot%202025-11-15%20104913.png)

---

The `otel-attributes-metadata.json` file is generated from your semantic conventions YAML using the weaver tool (see [Custom Semantic Conventions](#custom-semantic-conventions)).

## Quick Start (Development)

### 1. Install Prerequisites

```bash
# Install weaver CLI
cd otel-semconv-generator && make install

# Clone semantic conventions repo
git clone https://github.com/open-telemetry/semantic-conventions
```

### 2. Generate Semantic Conventions

```bash
cd otel-semconv-generator && make generate
```

This generates:

- `OtelAttributes.cs` - Constants for use in your code
- `otel-attributes-metadata.json` - Metadata for the analyzer

### 3. Build the Analyzer

```bash
dotnet build otel-semconv-dotnet.sln
```

### 4. Add Analyzer to Your Project

```xml
<ItemGroup>
  <!-- Reference the analyzer -->
  <ProjectReference Include="path/to/OtelSemConvAnalyzer/OtelSemConvAnalyzer.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"/>

  <!-- Include metadata file for analyzer (required) -->
  <AdditionalFiles Include="otel-attributes-metadata.json" />
</ItemGroup>
```

**Important:** The `otel-attributes-metadata.json` file must be included as an `AdditionalFile` for the analyzer to work. Copy this file to your project root after running `make generate`.

## Example Usage

```csharp
using System.Diagnostics;
using OpenTelemetry.SemanticConventions;

var activity = new Activity("my-operation");

// ✅ Valid: Correct attribute name and type
activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ID, "log-123");

// ❌ Error OTEL0001: Invalid attribute name
activity.SetTag("invalid.attr", "value");

// ❌ Error OTEL0002: Type mismatch (expects int, got string)
activity.SetTag(OtelAttributes.MYAPP_ACTIVITY_LOG_ENTRY_COUNT, "123");
```

## Resources

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Weaver Documentation](https://github.com/open-telemetry/weaver)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
