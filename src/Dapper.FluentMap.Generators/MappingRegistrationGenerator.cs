using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Dapper.FluentMap.Generators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class MappingRegistrationGenerator : IIncrementalGenerator
    {
        public const string InvalidGenericMapRegistrationDiagnosticId = "DFM005";
        public const string SkippedGeneratedMapDiagnosticId = "DFM006";
        public const string DuplicateGeneratedEntityMapDiagnosticId = "DFM007";

        private const string Category = "Dapper.FluentMap.Configuration";
        private const string MappingNamespace = "Dapper.FluentMap.Mapping";
        private const string GeneratedCodeHintName = "DapperFluentMapGeneratedRegistration.g.cs";

        private static readonly DiagnosticDescriptor InvalidGenericMapRegistrationRule = new DiagnosticDescriptor(
            InvalidGenericMapRegistrationDiagnosticId,
            "Generic map registration type is invalid",
            "Entity map type '{0}' must implement exactly one closed IEntityMap<TEntity> interface targeting a class type",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Generated registration can only register map types that implement exactly one closed IEntityMap<TEntity> interface whose entity type is a class.");

        private static readonly DiagnosticDescriptor SkippedGeneratedMapRule = new DiagnosticDescriptor(
            SkippedGeneratedMapDiagnosticId,
            "Entity map type is skipped by generated registration",
            "Entity map type '{0}' is not included in generated registration: {1}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Only concrete, closed and accessible entity map types with a public parameterless constructor can be included in generated registration.");

        private static readonly DiagnosticDescriptor DuplicateGeneratedEntityMapRule = new DiagnosticDescriptor(
            DuplicateGeneratedEntityMapDiagnosticId,
            "Multiple generated entity maps target the same entity",
            "Entity '{0}' has multiple generated entity maps: '{1}' and '{2}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Generated registration must not register more than one entity map for the same entity.");

        private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                  SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var mapCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => IsCandidateClassDeclaration(node),
                    (syntaxContext, cancellationToken) => CreateMapCandidate(syntaxContext, cancellationToken))
                .Where(candidate => candidate != null)
                .Collect();

            context.RegisterSourceOutput(
                mapCandidates,
                (sourceProductionContext, candidates) => Execute(sourceProductionContext, candidates));
        }

        private static bool IsCandidateClassDeclaration(SyntaxNode node)
        {
            var classDeclaration = node as ClassDeclarationSyntax;
            return classDeclaration?.BaseList != null;
        }

        private static MapCandidate CreateMapCandidate(
            GeneratorSyntaxContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var mapType = context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            if (mapType == null)
            {
                return null;
            }

            var entityMapInterfaces = mapType.AllInterfaces
                .Where(type => IsEntityMapInterface(type))
                .ToList();

            if (entityMapInterfaces.Count == 0)
            {
                return null;
            }

            var location = classDeclaration.Identifier.GetLocation();
            var mapDisplayName = FormatSymbol(mapType);
            var mapTypeName = mapType.ToDisplayString(FullyQualifiedTypeFormat);

            if (entityMapInterfaces.Count != 1 ||
                entityMapInterfaces[0].TypeArguments[0].TypeKind != TypeKind.Class)
            {
                return MapCandidate.InvalidRegistration(mapDisplayName, location);
            }

            var entityType = (INamedTypeSymbol)entityMapInterfaces[0].TypeArguments[0];
            if (mapType.IsAbstract)
            {
                return MapCandidate.Skipped(mapDisplayName, location, "the map type is abstract");
            }

            if (mapType.TypeParameters.Length != 0 || ContainsGenericParameters(mapType))
            {
                return MapCandidate.Skipped(mapDisplayName, location, "the map type is an open generic type");
            }

            if (!IsAccessibleFromGeneratedCode(mapType))
            {
                return MapCandidate.Skipped(mapDisplayName, location, "the map type is not accessible from generated code");
            }

            if (!HasPublicParameterlessConstructor(mapType))
            {
                return MapCandidate.Skipped(mapDisplayName, location, "the map type does not have a public parameterless constructor");
            }

            return MapCandidate.Valid(
                mapDisplayName,
                mapTypeName,
                entityType.ToDisplayString(FullyQualifiedTypeFormat),
                GetInheritanceDepth(entityType),
                location);
        }

        private static void Execute(
            SourceProductionContext context,
            ImmutableArray<MapCandidate> candidates)
        {
            var distinctCandidates = candidates
                .GroupBy(candidate => candidate.MapDisplayName, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            foreach (var candidate in distinctCandidates)
            {
                ReportCandidateDiagnostic(context, candidate);
            }

            var validMaps = distinctCandidates
                .Where(candidate => candidate.Kind == MapCandidateKind.Valid)
                .OrderBy(candidate => candidate.EntityInheritanceDepth)
                .ThenBy(candidate => candidate.EntityTypeName, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.MapTypeName, StringComparer.Ordinal)
                .ToList();

            var duplicateEntityTypeNames = ReportDuplicateEntityMaps(context, validMaps);
            var generatedMaps = validMaps
                .Where(candidate => !duplicateEntityTypeNames.Contains(candidate.EntityTypeName))
                .ToList();

            context.AddSource(GeneratedCodeHintName, SourceText.From(CreateGeneratedSource(generatedMaps), Encoding.UTF8));
        }

        private static void ReportCandidateDiagnostic(SourceProductionContext context, MapCandidate candidate)
        {
            if (candidate.Kind == MapCandidateKind.InvalidRegistration)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidGenericMapRegistrationRule,
                    candidate.Location,
                    candidate.MapDisplayName));
                return;
            }

            if (candidate.Kind == MapCandidateKind.Skipped)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SkippedGeneratedMapRule,
                    candidate.Location,
                    candidate.MapDisplayName,
                    candidate.SkipReason));
            }
        }

        private static ISet<string> ReportDuplicateEntityMaps(
            SourceProductionContext context,
            IList<MapCandidate> validMaps)
        {
            var duplicateEntityTypeNames = new HashSet<string>(StringComparer.Ordinal);
            var groups = validMaps
                .GroupBy(candidate => candidate.EntityTypeName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1);

            foreach (var group in groups)
            {
                var orderedGroup = group
                    .OrderBy(candidate => candidate.MapTypeName, StringComparer.Ordinal)
                    .ToList();
                var first = orderedGroup[0];
                duplicateEntityTypeNames.Add(first.EntityTypeName);

                foreach (var duplicate in orderedGroup.Skip(1))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DuplicateGeneratedEntityMapRule,
                        duplicate.Location,
                        duplicate.EntityTypeName,
                        first.MapTypeName,
                        duplicate.MapTypeName));
                }
            }

            return duplicateEntityTypeNames;
        }

        private static string CreateGeneratedSource(IList<MapCandidate> maps)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("namespace Dapper.FluentMap");
            builder.AppendLine("{");
            builder.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"Dapper.FluentMap.Generators\", \"2.0.0\")]");
            builder.AppendLine("    internal static class DapperFluentMapGeneratedRegistration");
            builder.AppendLine("    {");
            builder.AppendLine("        public static global::Dapper.FluentMap.Configuration.FluentMapConfiguration AddGeneratedMappings(");
            builder.AppendLine("            this global::Dapper.FluentMap.Configuration.FluentMapConfiguration configuration)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (configuration == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(configuration));");
            builder.AppendLine("            }");
            builder.AppendLine();

            if (maps.Count == 0)
            {
                builder.AppendLine("            return configuration;");
            }
            else
            {
                builder.AppendLine("            return configuration");
                for (var index = 0; index < maps.Count; index++)
                {
                    var terminator = index == maps.Count - 1 ? ";" : string.Empty;
                    builder.Append("                .AddMap<");
                    builder.Append(maps[index].MapTypeName);
                    builder.Append(">()");
                    builder.AppendLine(terminator);
                }
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static bool IsEntityMapInterface(INamedTypeSymbol type)
        {
            return type.OriginalDefinition.MetadataName == "IEntityMap`1" &&
                   type.OriginalDefinition.ContainingNamespace.ToDisplayString() == MappingNamespace;
        }

        private static bool ContainsGenericParameters(INamedTypeSymbol type)
        {
            if (type.IsGenericType && type.TypeArguments.Any(argument => argument.Kind == SymbolKind.TypeParameter))
            {
                return true;
            }

            for (var containingType = type.ContainingType; containingType != null; containingType = containingType.ContainingType)
            {
                if (containingType.TypeParameters.Length != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.ContainingType)
            {
                if (current.DeclaredAccessibility != Accessibility.Public &&
                    current.DeclaredAccessibility != Accessibility.Internal)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        {
            return type.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                constructor.DeclaredAccessibility == Accessibility.Public);
        }

        private static int GetInheritanceDepth(INamedTypeSymbol type)
        {
            var depth = 0;
            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                depth++;
            }

            return depth;
        }

        private static string FormatSymbol(ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        private sealed class MapCandidate
        {
            private MapCandidate(
                MapCandidateKind kind,
                string mapDisplayName,
                string mapTypeName,
                string entityTypeName,
                int entityInheritanceDepth,
                Location location,
                string skipReason)
            {
                Kind = kind;
                MapDisplayName = mapDisplayName;
                MapTypeName = mapTypeName;
                EntityTypeName = entityTypeName;
                EntityInheritanceDepth = entityInheritanceDepth;
                Location = location;
                SkipReason = skipReason;
            }

            internal MapCandidateKind Kind { get; }

            internal string MapDisplayName { get; }

            internal string MapTypeName { get; }

            internal string EntityTypeName { get; }

            internal int EntityInheritanceDepth { get; }

            internal Location Location { get; }

            internal string SkipReason { get; }

            internal static MapCandidate Valid(
                string mapDisplayName,
                string mapTypeName,
                string entityTypeName,
                int entityInheritanceDepth,
                Location location)
            {
                return new MapCandidate(
                    MapCandidateKind.Valid,
                    mapDisplayName,
                    mapTypeName,
                    entityTypeName,
                    entityInheritanceDepth,
                    location,
                    null);
            }

            internal static MapCandidate InvalidRegistration(string mapDisplayName, Location location)
            {
                return new MapCandidate(
                    MapCandidateKind.InvalidRegistration,
                    mapDisplayName,
                    null,
                    null,
                    0,
                    location,
                    null);
            }

            internal static MapCandidate Skipped(string mapDisplayName, Location location, string reason)
            {
                return new MapCandidate(
                    MapCandidateKind.Skipped,
                    mapDisplayName,
                    null,
                    null,
                    0,
                    location,
                    reason);
            }
        }

        private enum MapCandidateKind
        {
            Valid,
            InvalidRegistration,
            Skipped
        }
    }
}
