using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace OtelSemConvAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OtelSemanticConventionAnalyzer : DiagnosticAnalyzer
{
    // Diagnostic IDs
    public const string InvalidAttributeId = "OTEL0001";
    public const string TypeMismatchId = "OTEL0002";
    public const string ExperimentalAttributeId = "OTEL0003";
    public const string DeprecatedAttributeId = "OTEL0004";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor InvalidAttributeRule = new(
        id: InvalidAttributeId,
        title: "Invalid OpenTelemetry attribute name",
        messageFormat: "Attribute '{0}' is not defined in the semantic conventions registry",
        category: "OpenTelemetry",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TypeMismatchRule = new(
        id: TypeMismatchId,
        title: "OpenTelemetry attribute type mismatch",
        messageFormat: "Attribute '{0}' expects type '{1}' but received '{2}'",
        category: "OpenTelemetry",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ExperimentalAttributeRule = new(
        id: ExperimentalAttributeId,
        title: "Experimental OpenTelemetry attribute",
        messageFormat: "Attribute '{0}' is experimental and may change in future versions",
        category: "OpenTelemetry",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DeprecatedAttributeRule = new(
        id: DeprecatedAttributeId,
        title: "Deprecated OpenTelemetry attribute",
        messageFormat: "Attribute '{0}' is deprecated: {1}",
        category: "OpenTelemetry",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        InvalidAttributeRule,
        TypeMismatchRule,
        ExperimentalAttributeRule,
        DeprecatedAttributeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Load metadata once per compilation for better performance
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Load and cache metadata from AdditionalFiles
            var registry = AttributeMetadataLoader.LoadFromAdditionalFiles(compilationContext.Options.AdditionalFiles);
            if (registry == null)
                return; // No metadata file, skip validation

            // Register operation action with cached registry
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, registry),
                OperationKind.Invocation);
        });
    }

    private void AnalyzeInvocation(OperationAnalysisContext context, AttributeRegistry registry)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Check if this is a target method (SetTag, AddTag, SetAttribute)
        if (!IsTargetMethod(method))
            return;

        // Must have at least 2 arguments (key and value)
        if (invocation.Arguments.Length < 2)
            return;

        var keyArgumentOperation = invocation.Arguments[0];
        var valueArgumentOperation = invocation.Arguments[1];
        var keyArgument = keyArgumentOperation.Value;
        var valueArgument = valueArgumentOperation.Value;

        // Try to resolve the attribute name
        var attributeName = ResolveAttributeName(keyArgument);
        if (attributeName == null)
            return; // Can't analyze dynamic/variable keys

        // Look up the attribute in metadata
        if (!registry.ByName.TryGetValue(attributeName, out var metadata))
        {
            // Unknown attribute - highlight the key argument
            var diagnostic = Diagnostic.Create(
                InvalidAttributeRule,
                GetArgumentExpressionLocation(keyArgumentOperation),
                attributeName);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check for experimental
        if (metadata.Stability == "experimental" || metadata.Stability == "development")
        {
            var diagnostic = Diagnostic.Create(
                ExperimentalAttributeRule,
                GetArgumentExpressionLocation(keyArgumentOperation),
                attributeName);
            context.ReportDiagnostic(diagnostic);
        }

        // Check for deprecated
        if (metadata.Deprecated != null)
        {
            var diagnostic = Diagnostic.Create(
                DeprecatedAttributeRule,
                GetArgumentExpressionLocation(keyArgumentOperation),
                attributeName,
                metadata.Deprecated);
            context.ReportDiagnostic(diagnostic);
        }

        // Check type compatibility
        // Unwrap conversion operations (e.g., boxing to object) to get the actual type
        var actualValueOperation = UnwrapConversion(valueArgument);
        var valueType = actualValueOperation.Type;

        if (valueType != null && !IsTypeCompatible(metadata.Type, valueType))
        {
            var diagnostic = Diagnostic.Create(
                TypeMismatchRule,
                GetArgumentExpressionLocation(valueArgumentOperation),
                attributeName,
                metadata.Type,
                valueType.ToDisplayString());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static Location GetArgumentExpressionLocation(IArgumentOperation argument)
    {
        // ArgumentSyntax contains the expression we want to highlight
        if (argument.Syntax is ArgumentSyntax argSyntax)
        {
            return argSyntax.Expression.GetLocation();
        }

        // Fallback to the argument's own syntax location
        return argument.Syntax.GetLocation();
    }

    private static bool IsTargetMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType.ToDisplayString();
        var methodName = method.Name;

        // System.Diagnostics.Activity methods
        if (containingType == "System.Diagnostics.Activity")
        {
            return methodName is "SetTag" or "AddTag";
        }

        // OpenTelemetry Span/TelemetrySpan methods
        if (containingType.StartsWith("OpenTelemetry.Trace"))
        {
            return methodName == "SetAttribute";
        }

        return false;
    }

    private static string? ResolveAttributeName(IOperation keyArgument)
    {
        // Case 1: Literal string
        if (keyArgument is ILiteralOperation literal &&
            literal.ConstantValue.HasValue &&
            literal.ConstantValue.Value is string literalValue)
        {
            return literalValue;
        }

        // Case 2: Field reference (constant)
        if (keyArgument is IFieldReferenceOperation fieldRef)
        {
            var field = fieldRef.Field;
            if (field.IsConst && field.ConstantValue is string constValue)
            {
                return constValue;
            }
        }

        // Case 3: Unknown pattern - skip analysis
        return null;
    }

    private static IOperation UnwrapConversion(IOperation operation)
    {
        // Unwrap conversion operations (like boxing to object) to get the actual underlying type
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }
        return operation;
    }

    private static bool IsTypeCompatible(string expectedType, ITypeSymbol actualType)
    {
        return expectedType switch
        {
            "string" => IsStringType(actualType),
            "int" => IsIntegerType(actualType),
            "double" => IsFloatingPointType(actualType),
            "boolean" => actualType.SpecialType == SpecialType.System_Boolean,
            "string[]" => IsStringArrayType(actualType),
            "int[]" => IsIntArrayType(actualType),
            "double[]" => IsDoubleArrayType(actualType),
            "boolean[]" => IsBoolArrayType(actualType),
            _ => true // Unknown types pass (might be template types like template[string])
        };
    }

    private static bool IsStringType(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_String;
    }

    private static bool IsIntegerType(ITypeSymbol type)
    {
        // Allow int, long, short, byte (numeric conversions per user request)
        return type.SpecialType is
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Int16 or
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_UInt16;
    }

    private static bool IsFloatingPointType(ITypeSymbol type)
    {
        // Allow double and float
        return type.SpecialType is
            SpecialType.System_Double or
            SpecialType.System_Single;
    }

    private static bool IsStringArrayType(ITypeSymbol type)
    {
        if (type is not IArrayTypeSymbol arrayType)
            return false;
        return arrayType.ElementType.SpecialType == SpecialType.System_String;
    }

    private static bool IsIntArrayType(ITypeSymbol type)
    {
        if (type is not IArrayTypeSymbol arrayType)
            return false;
        return IsIntegerType(arrayType.ElementType);
    }

    private static bool IsDoubleArrayType(ITypeSymbol type)
    {
        if (type is not IArrayTypeSymbol arrayType)
            return false;
        return IsFloatingPointType(arrayType.ElementType);
    }

    private static bool IsBoolArrayType(ITypeSymbol type)
    {
        if (type is not IArrayTypeSymbol arrayType)
            return false;
        return arrayType.ElementType.SpecialType == SpecialType.System_Boolean;
    }
}
