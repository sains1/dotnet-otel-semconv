.PHONY: help generate clean install test-validate

# Default target
help:
	@echo "OpenTelemetry Semantic Conventions Generator"
	@echo ""
	@echo "Available targets:"
	@echo "  make generate        - Generate semantic conventions from YAML and copy to src"
	@echo "  make test-validate   - Run tests with telemetry validation"
	@echo "  make clean           - Remove generated files"
	@echo "  make install         - Install/update weaver CLI"
	@echo "  make help            - Show this help message"

# Install or update weaver CLI
install:
	@echo "Installing OpenTelemetry Weaver CLI..."
	@curl --proto '=https' --tlsv1.2 -LsSf https://github.com/open-telemetry/weaver/releases/latest/download/weaver-installer.sh | sh
	@echo "✓ Weaver installed successfully"

# Clean generated files
clean:
	@echo "Cleaning generated files..."
	@rm -rf output/
	@rm -f resolved-registry.json
	@echo "✓ Clean complete"

# Generate semantic conventions
generate: clean
	@echo "Generating OpenTelemetry semantic conventions..."
	@echo ""

	@echo "Step 1/4: Copying custom registry to semantic-conventions/model/..."
	@cp custom-registry/*.yaml semantic-conventions/model/
	@echo "✓ Custom registry files copied"
	@echo ""

	@echo "Step 2/4: Generating C# code from semantic conventions..."
	@echo "(Using registry: ./semantic-conventions/model)"
	@weaver registry generate \
		--registry ./semantic-conventions/model \
		--templates templates/registry \
		csharp \
		output
	@echo "✓ Code generated"
	@echo ""

	@echo "Step 3/4: Copying generated file to sample/OtelSemConvAnalyzer.Sample/..."
	@mkdir -p ../sample/OtelSemConvAnalyzer.Sample
	@cp output/OtelAttributes.cs ../sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs
	@echo "✓ File copied to ../sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs"
	@echo ""

	@echo "Step 4/4: Verifying generated file..."
	@wc -l ../sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs
	@grep -c "MYAPP_" ../sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs || true
	@echo ""

	@echo "✓ Semantic conventions generated successfully!"
	@echo ""
	@echo "Next steps:"
	@echo "  1. Review the generated file: sample/OtelSemConvAnalyzer.Sample/OtelAttributes.cs"
	@echo "  2. Build your project to verify compilation"
	@echo "  3. Commit the changes to git"