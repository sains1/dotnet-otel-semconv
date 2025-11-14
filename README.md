# otel-semconv-dotnet

Example of using Semantic conventions in a .NET project

Including:

- Using weaver for developing semantic conventions, code generation, and for CI checks
- A Roslyn analyzer for enforcing use of semantic conventions

## Getting Started

Install weaver:

```
cd ./otel-semconv-generator && \
make install
```

Clone semantic conventions repo:

```
cd ./otel-semconv-generator && \
git clone https://github.com/open-telemetry/semantic-conventions
```

Re-generate the Attributes

```
cd ./otel-semconv-generator && \
make generate
```
