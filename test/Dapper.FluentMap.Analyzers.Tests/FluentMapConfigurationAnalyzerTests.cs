using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper.FluentMap;
using Dapper.FluentMap.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Dapper.FluentMap.Analyzers.Tests
{
    public sealed class FluentMapConfigurationAnalyzerTests
    {
        [Fact]
        public async Task InvalidMapExpressionShouldReportDfm001()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public string Name { get; set; }

    public string GetName() => Name;
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(c => c.GetName()).ToColumn(""customer_name"");
    }
}";

            var diagnostic = await GetSingleDiagnosticAsync(source, FluentMapConfigurationAnalyzer.InvalidMapExpressionDiagnosticId);

            AssertDiagnostic(diagnostic, DiagnosticSeverity.Error, "Map expression 'c.GetName()' is invalid");
            AssertDiagnosticLineContains(source, diagnostic, "Map(c => c.GetName()).ToColumn");
        }

        [Fact]
        public async Task DuplicateMemberPathShouldReportDfm002()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(c => c.Id).ToColumn(""customer_id"");
        Map(c => c.Id).ToColumn(""other_id"");
    }
}";

            var diagnostic = await GetSingleDiagnosticAsync(source, FluentMapConfigurationAnalyzer.DuplicateMemberPathDiagnosticId);

            AssertDiagnostic(diagnostic, DiagnosticSeverity.Error, "Property path 'Id' is mapped more than once");
            AssertDiagnosticLineContains(source, diagnostic, "Map(c => c.Id).ToColumn(\"other_id\")");
        }

        [Fact]
        public async Task DuplicateColumnShouldReportDfm003()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(c => c.Id).ToColumn(""shared_column"");
        Map(c => c.Name).ToColumn(""shared_column"");
    }
}";

            var diagnostic = await GetSingleDiagnosticAsync(source, FluentMapConfigurationAnalyzer.DuplicateColumnDiagnosticId);

            AssertDiagnostic(diagnostic, DiagnosticSeverity.Error, "Column 'shared_column' is mapped by more than one property path");
            AssertDiagnosticLineContains(source, diagnostic, "Map(c => c.Name).ToColumn(\"shared_column\")");
        }

        [Fact]
        public async Task InvalidIncludeBaseShouldReportDfm004()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public class CustomerBase
{
    public int Id { get; set; }
}

public sealed class Customer : CustomerBase
{
    public string Name { get; set; }
}

public sealed class OtherCustomer
{
    public int Id { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        IncludeBase<OtherCustomer>();
    }
}";

            var diagnostic = await GetSingleDiagnosticAsync(source, FluentMapConfigurationAnalyzer.InvalidIncludeBaseDiagnosticId);

            AssertDiagnostic(diagnostic, DiagnosticSeverity.Error, "Type 'OtherCustomer' cannot be included as a base mapping for entity 'Customer'");
            AssertDiagnosticLineContains(source, diagnostic, "IncludeBase<OtherCustomer>()");
        }

        [Fact]
        public async Task InvalidGenericMapRegistrationShouldReportDfm005()
        {
            var source = @"
using System.Collections.Generic;
using Dapper.FluentMap.Configuration;
using Dapper.FluentMap.Mapping;

public sealed class NonGenericMap : IEntityMap
{
    public IList<IPropertyMap> PropertyMaps { get; } = new List<IPropertyMap>();
}

public sealed class Startup
{
    public void Configure(FluentMapConfiguration configuration)
    {
        configuration.AddMap<NonGenericMap>();
    }
}";

            var diagnostic = await GetSingleDiagnosticAsync(source, FluentMapConfigurationAnalyzer.InvalidGenericMapRegistrationDiagnosticId);

            AssertDiagnostic(diagnostic, DiagnosticSeverity.Error, "Entity map type 'NonGenericMap' must implement exactly one closed IEntityMap<TEntity> interface");
            AssertDiagnosticLineContains(source, diagnostic, "configuration.AddMap<NonGenericMap>()");
        }

        [Fact]
        public async Task ValidMappingConfigurationShouldNotReportDiagnostics()
        {
            var source = @"
using Dapper.FluentMap.Configuration;
using Dapper.FluentMap.Mapping;

public sealed record ConstructorCustomer(int Id, string Name);

public class CustomerBase
{
    public int Id { get; set; }
}

public sealed class Customer : CustomerBase
{
    public string Name { get; set; }

    public Rank Rank { get; set; }

    public Seniority Seniority { get; set; }
}

public sealed class Rank
{
    public int Level { get; set; }
}

public sealed class Seniority
{
    public int Level { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        IncludeBase<CustomerBase>();
        Map(c => c.Name).ToColumn(""customer_name"");
        Map(c => c.Rank.Level).ToColumn(""rank_level"");
        Map(c => c.Seniority.Level).ToColumn(""seniority_level"");
    }
}

public sealed class CustomerBaseMap : EntityMap<CustomerBase>
{
    public CustomerBaseMap()
    {
        Map(c => c.Id).ToColumn(""customer_id"");
    }
}

public sealed class ConstructorCustomerMap : EntityMap<ConstructorCustomer>
{
    public ConstructorCustomerMap()
    {
        Map(c => c.Id).ToColumn(""constructor_customer_id"");
    }
}

public sealed class Startup
{
    public void Configure(FluentMapConfiguration configuration)
    {
        configuration
            .AddMap<CustomerMap>()
            .AddMap<CustomerBaseMap>()
            .AddMap<ConstructorCustomerMap>();
    }
}";

            var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task CaseSensitiveColumnNamesWithDifferentCasingShouldNotReportDfm003()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(c => c.Id).ToColumn(""Customer"");
        Map(c => c.Name).ToColumn(""customer"");
    }
}";

            var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

            Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == FluentMapConfigurationAnalyzer.DuplicateColumnDiagnosticId);
        }

        private static async Task<Diagnostic> GetSingleDiagnosticAsync(string source, string diagnosticId)
        {
            var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
            return Assert.Single(diagnostics, diagnostic => diagnostic.Id == diagnosticId);
        }

        private static async Task<IReadOnlyList<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                path: "Test0.cs");

            var references = GetMetadataReferences();
            var compilation = CSharpCompilation.Create(
                "AnalyzerTest",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var compilerErrors = compilation
                .GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString())
                .ToList();

            Assert.Empty(compilerErrors);

            var analyzer = new FluentMapConfigurationAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            return diagnostics
                .Where(diagnostic => diagnostic.Id.StartsWith("DFM", StringComparison.Ordinal))
                .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
                .ToList();
        }

        private static IReadOnlyList<MetadataReference> GetMetadataReferences()
        {
            var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));

            var explicitAssemblies = new[]
            {
                typeof(FluentMapper).Assembly.Location,
                typeof(Dapper.SqlMapper).Assembly.Location
            }
            .Select(path => MetadataReference.CreateFromFile(path));

            return trustedPlatformAssemblies
                .Concat(explicitAssemblies)
                .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static void AssertDiagnostic(Diagnostic diagnostic, DiagnosticSeverity severity, string messageFragment)
        {
            Assert.Equal(severity, diagnostic.Severity);
            Assert.Contains(messageFragment, diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        private static void AssertDiagnosticLineContains(string source, Diagnostic diagnostic, string expectedLineFragment)
        {
            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
            var sourceLine = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[line];

            Assert.Contains(expectedLineFragment, sourceLine, StringComparison.Ordinal);
        }
    }
}
