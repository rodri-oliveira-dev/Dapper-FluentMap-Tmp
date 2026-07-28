using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Dapper.FluentMap;
using Dapper.FluentMap.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Dapper.FluentMap.Generators.Tests
{
    public sealed class MappingRegistrationGeneratorTests
    {
        [Fact]
        public void ZeroMappingsShouldGenerateNoOpRegistration()
        {
            var source = @"
using Dapper.FluentMap;

public sealed class Startup
{
    public void Configure()
    {
        FluentMapper.Initialize(configuration => configuration.AddGeneratedMappings());
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains("return configuration;", result.GeneratedSource, StringComparison.Ordinal);
            Assert.DoesNotContain(".AddMap<", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void OneMappingShouldGenerateExplicitAddMapCall()
        {
            var source = @"
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}

public sealed class Startup
{
    public void Configure()
    {
        FluentMapper.Initialize(configuration => configuration.AddGeneratedMappings());
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains(".AddMap<global::CustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddGeneratedMaterializer<global::Customer>(", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("GeneratedMaterializerColumn.Map(\"customer_id\", \"Id\")", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("entity.Id = Read<int>(record, 0);", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void RenamedColumnsShouldGenerateFlatMaterializerDescriptor()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
        Map(customer => customer.FullName).ToColumn(""full_name"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains("GeneratedMaterializerColumn.Map(\"customer_id\", \"Id\")", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("GeneratedMaterializerColumn.Map(\"full_name\", \"FullName\")", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("entity.FullName = Read<string>(record, 1);", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void ConstructorMappingShouldGenerateFlatConstructorMaterializer()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public Customer(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public int Id { get; }

    public string FullName { get; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
        Map(customer => customer.FullName).ToColumn(""full_name"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains("var id = Read<int>(record, 0);", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("var fullName = Read<string>(record, 1);", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("return new global::Customer(id, fullName);", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("FluentMapConfigurationException", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void NullableValuesShouldUseGeneratedReadHelper()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int? Age { get; set; }

    public string Note { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Age).ToColumn(""age"");
        Map(customer => customer.Note).ToColumn(""note"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains("entity.Age = Read<int?>(record, 0);", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains("return default(T);", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void MultipleMappingsShouldBeGeneratedInDeterministicOrder()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Order
{
    public int Id { get; set; }
}

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class ZOrderMap : EntityMap<Order>
{
    public ZOrderMap()
    {
        Map(order => order.Id).ToColumn(""order_id"");
    }
}

public sealed class ACustomerMap : EntityMap<Customer>
{
    public ACustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.True(
                result.GeneratedSource.IndexOf("ACustomerMap", StringComparison.Ordinal) <
                result.GeneratedSource.IndexOf("ZOrderMap", StringComparison.Ordinal));
        }

        [Fact]
        public void InternalMappingShouldBeSupported()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

internal sealed class InternalCustomer
{
    public int Id { get; set; }
}

internal sealed class InternalCustomerMap : EntityMap<InternalCustomer>
{
    public InternalCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains(".AddMap<global::InternalCustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void AbstractMappingShouldReportSkippedDiagnostic()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public abstract class CustomerMapBase : EntityMap<Customer>
{
}";

            var result = RunGenerator(source);
            var diagnostic = Assert.Single(result.DfmDiagnostics);

            Assert.Equal(MappingRegistrationGenerator.SkippedGeneratedMapDiagnosticId, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.Contains("abstract", diagnostic.GetMessage(), StringComparison.Ordinal);
            Assert.DoesNotContain(".AddMap<global::CustomerMapBase>()", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void OpenGenericMappingShouldReportSkippedDiagnostic()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class GenericCustomerMap<T> : EntityMap<Customer>
{
    public GenericCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}";

            var result = RunGenerator(source);
            var diagnostic = Assert.Single(result.DfmDiagnostics);

            Assert.Equal(MappingRegistrationGenerator.SkippedGeneratedMapDiagnosticId, diagnostic.Id);
            Assert.Contains("open generic", diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        [Fact]
        public void DuplicateEntityMappingsShouldReportDiagnostic()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class FirstCustomerMap : EntityMap<Customer>
{
    public FirstCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}

public sealed class SecondCustomerMap : EntityMap<Customer>
{
    public SecondCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""other_id"");
    }
}";

            var result = RunGenerator(source, assertCompiles: false);
            var diagnostic = Assert.Single(result.DfmDiagnostics);

            Assert.Equal(MappingRegistrationGenerator.DuplicateGeneratedEntityMapDiagnosticId, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Contains("multiple generated entity maps", diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        [Fact]
        public void ProfileMappingShouldGenerateAddProfileCall()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class LegacyProfile : IMappingProfile
{
}

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}

public sealed class LegacyCustomerMap : EntityMap<Customer>, IProfileMap<LegacyProfile>
{
    public LegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""legacy_id"");
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains(".AddMap<global::CustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddProfile<global::LegacyCustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddGeneratedMaterializer<global::Customer>(", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddGeneratedMaterializer<global::Customer, global::LegacyProfile>(", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void UnsupportedNestedMappingShouldReportFallbackDiagnostic()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public Address Address { get; set; }
}

public sealed class Address
{
    public string City { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Address.City).ToColumn(""city"");
    }
}";

            var result = RunGenerator(source);
            var diagnostic = Assert.Single(result.DfmDiagnostics);

            Assert.Equal(MappingRegistrationGenerator.SkippedGeneratedMaterializerDiagnosticId, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.Contains("nested member paths", diagnostic.GetMessage(), StringComparison.Ordinal);
            Assert.Contains(".AddMap<global::CustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.DoesNotContain(".AddGeneratedMaterializer<global::Customer>(", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void DuplicateProfileMappingsShouldReportDiagnostic()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

public sealed class LegacyProfile : IMappingProfile
{
}

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class FirstLegacyCustomerMap : EntityMap<Customer>, IProfileMap<LegacyProfile>
{
    public FirstLegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""legacy_id"");
    }
}

public sealed class SecondLegacyCustomerMap : EntityMap<Customer>, IProfileMap<LegacyProfile>
{
    public SecondLegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""other_legacy_id"");
    }
}";

            var result = RunGenerator(source, assertCompiles: false);
            var diagnostic = Assert.Single(result.DfmDiagnostics);

            Assert.Equal(MappingRegistrationGenerator.DuplicateGeneratedProfileMapDiagnosticId, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Contains("multiple generated maps for profile", diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        [Fact]
        public void DistinctNamespacesShouldGenerateFullyQualifiedNames()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

namespace Sales
{
    public sealed class Customer
    {
        public int Id { get; set; }
    }

    public sealed class CustomerMap : EntityMap<Customer>
    {
        public CustomerMap()
        {
            Map(customer => customer.Id).ToColumn(""sales_customer_id"");
        }
    }
}

namespace Support
{
    public sealed class Ticket
    {
        public int Id { get; set; }
    }

    public sealed class TicketMap : EntityMap<Ticket>
    {
        public TicketMap()
        {
            Map(ticket => ticket.Id).ToColumn(""ticket_id"");
        }
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains(".AddMap<global::Sales.CustomerMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddMap<global::Support.TicketMap>()", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void SameMapClassNameInDifferentNamespacesShouldGenerateBothMappings()
        {
            var source = @"
using Dapper.FluentMap.Mapping;

namespace Sales
{
    public sealed class Customer
    {
        public int Id { get; set; }
    }

    public sealed class EntityMap : Dapper.FluentMap.Mapping.EntityMap<Customer>
    {
        public EntityMap()
        {
            Map(customer => customer.Id).ToColumn(""sales_customer_id"");
        }
    }
}

namespace Support
{
    public sealed class Ticket
    {
        public int Id { get; set; }
    }

    public sealed class EntityMap : Dapper.FluentMap.Mapping.EntityMap<Ticket>
    {
        public EntityMap()
        {
            Map(ticket => ticket.Id).ToColumn(""ticket_id"");
        }
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.DfmDiagnostics);
            Assert.Contains(".AddMap<global::Sales.EntityMap>()", result.GeneratedSource, StringComparison.Ordinal);
            Assert.Contains(".AddMap<global::Support.EntityMap>()", result.GeneratedSource, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedOutputShouldBeDeterministic()
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
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}";

            var first = RunGenerator(source);
            var second = RunGenerator(source);

            Assert.Equal(first.GeneratedSource, second.GeneratedSource);
        }

        [Fact]
        public void IncrementalGeneratorShouldProduceStableOutputAcrossRepeatedRuns()
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
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}";
            var compilation = CreateCompilation(source);
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new MappingRegistrationGenerator());

            driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
            var first = GetGeneratedSource(driver);

            driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
            var second = GetGeneratedSource(driver);

            Assert.Equal(first, second);
        }

        [Fact]
        public void GeneratedRegistrationSourceShouldCompileWithConsumerCode()
        {
            var source = @"
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn(""customer_id"");
    }
}

public sealed class Startup
{
    public void Configure()
    {
        FluentMapper.Initialize(configuration => configuration.AddGeneratedMappings());
    }
}";

            var result = RunGenerator(source);

            Assert.Empty(result.CompilerErrors);
            Assert.Contains("AddGeneratedMappings", result.GeneratedSource, StringComparison.Ordinal);
        }

        private static GeneratorTestResult RunGenerator(string source, bool assertCompiles = true)
        {
            var compilation = CreateCompilation(source);
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new MappingRegistrationGenerator());

            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var generatorDiagnostics,
                TestContext.Current.CancellationToken);

            var compilerErrors = outputCompilation
                .GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString())
                .ToList();

            if (assertCompiles)
            {
                Assert.Empty(compilerErrors);
            }

            return new GeneratorTestResult(
                GetGeneratedSource(driver),
                generatorDiagnostics
                    .Where(diagnostic => diagnostic.Id.StartsWith("DFM", StringComparison.Ordinal))
                    .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
                    .ToList(),
                compilerErrors);
        }

        private static CSharpCompilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                path: "Test0.cs");

            return CSharpCompilation.Create(
                "GeneratorTest",
                new[] { syntaxTree },
                GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static string GetGeneratedSource(GeneratorDriver driver)
        {
            var runResult = driver.GetRunResult();
            var generatorResult = Assert.Single(runResult.Results);
            var generatedSource = Assert.Single(
                generatorResult.GeneratedSources,
                source => source.HintName == "DapperFluentMapGeneratedRegistration.g.cs");

            return generatedSource.SourceText.ToString();
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

        private sealed class GeneratorTestResult
        {
            internal GeneratorTestResult(
                string generatedSource,
                IReadOnlyList<Diagnostic> dfmDiagnostics,
                IReadOnlyList<string> compilerErrors)
            {
                GeneratedSource = generatedSource;
                DfmDiagnostics = dfmDiagnostics;
                CompilerErrors = compilerErrors;
            }

            internal string GeneratedSource { get; }

            internal IReadOnlyList<Diagnostic> DfmDiagnostics { get; }

            internal IReadOnlyList<string> CompilerErrors { get; }
        }
    }
}
