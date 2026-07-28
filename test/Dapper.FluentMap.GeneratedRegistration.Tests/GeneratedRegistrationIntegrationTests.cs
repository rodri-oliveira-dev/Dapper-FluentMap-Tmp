using System;
using Dapper;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.Naming;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.FluentMap.GeneratedRegistration.Tests
{
    public sealed class GeneratedRegistrationIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void GeneratedRegistrationShouldWorkWithDapperAndExistingMappingFeatures()
        {
            ResetMapper();

            try
            {
                FluentMapper.Initialize(configuration =>
                {
                    configuration.AddGeneratedMappings();
                    configuration.UseNamingPolicy(NamingPolicy.SnakeCase).ForEntity<GeneratedNamingCustomer>();
                });

                using (var connection = OpenConnection())
                {
                    var customer = connection.QuerySingle<GeneratedCustomer>(
                        "SELECT 7 AS customer_id, 'Ada' AS Name;");
                    var internalCustomer = connection.QuerySingle<GeneratedInternalCustomer>(
                        "SELECT 8 AS internal_id;");
                    var derived = connection.QuerySingle<GeneratedDerivedCustomer>(
                        "SELECT 9 AS base_id, 'Lovelace' AS derived_name;");
                    var immutable = connection.QuerySingle<GeneratedImmutableCustomer>(
                        "SELECT 10 AS immutable_id, 'Grace' AS name;");
                    var queryMappedImmutable = connection.QueryMappedSingle<GeneratedImmutableCustomer>(
                        "SELECT 12 AS immutable_id, 'Generated Constructor' AS name;");
                    var nullable = connection.QueryMappedSingle<GeneratedNullableCustomer>(
                        "SELECT NULL AS age, NULL AS note;");
                    var named = connection.QuerySingle<GeneratedNamingCustomer>(
                        "SELECT '2026-07-26T10:30:00' AS created_at;");
                    var profiled = connection.QueryMappedSingle<GeneratedProfileCustomer, GeneratedLegacyProfile>(
                        "SELECT 11 AS legacy_id, 'Profiled' AS legacy_name;");
                    var nested = connection.QueryMappedSingle<GeneratedNestedCustomer>(
                        "SELECT 13 AS customer_id, 'Sao Paulo' AS city;");
                    var nullableNested = connection.QueryMappedSingle<GeneratedNestedCustomer>(
                        "SELECT 14 AS customer_id, NULL AS city;");
                    var valueObject = connection.QueryMappedSingle<GeneratedValueObjectCustomer>(
                        "SELECT 15 AS customer_id, '12345678909' AS cpf;");
                    var nullableValueObject = connection.QueryMappedSingle<GeneratedValueObjectCustomer>(
                        "SELECT 16 AS customer_id, NULL AS cpf;");
                    var sameTerminal = connection.QueryMappedSingle<GeneratedSameTerminalCustomer>(
                        "SELECT 5 AS rank_level, 9 AS seniority_level;");
                    var profiledNested = connection.QueryMappedSingle<GeneratedProfileNestedCustomer, GeneratedLegacyProfile>(
                        "SELECT 'Profile City' AS legacy_city;");

                    Assert.Equal(7, customer.Id);
                    Assert.Equal("Ada", customer.Name);
                    Assert.Equal(8, internalCustomer.Id);
                    Assert.Equal(9, derived.Id);
                    Assert.Equal("Lovelace", derived.Name);
                    Assert.Equal(10, immutable.Id);
                    Assert.Equal("Grace", immutable.Name);
                    Assert.Equal(12, queryMappedImmutable.Id);
                    Assert.Equal("Generated Constructor", queryMappedImmutable.Name);
                    Assert.Null(nullable.Age);
                    Assert.Null(nullable.Note);
                    Assert.Equal(new DateTime(2026, 7, 26, 10, 30, 0), named.CreatedAt);
                    Assert.Equal(11, profiled.Id);
                    Assert.Equal("Profiled", profiled.Name);
                    Assert.Equal(13, nested.Id);
                    Assert.NotNull(nested.Address);
                    Assert.Equal("Sao Paulo", nested.Address.City);
                    Assert.Equal(14, nullableNested.Id);
                    Assert.Null(nullableNested.Address);
                    Assert.Equal(15, valueObject.Id);
                    Assert.Equal("12345678909", valueObject.Cpf.Number);
                    Assert.Equal(16, nullableValueObject.Id);
                    Assert.Null(nullableValueObject.Cpf);
                    Assert.Equal(5, sameTerminal.Rank.Level);
                    Assert.Equal(9, sameTerminal.Seniority.Level);
                    Assert.Equal("Profile City", profiledNested.Address.City);
                    Assert.Equal(0, FluentMapper.Registry.MaterializationPlanCacheEntryCount);

                    var fallback = connection.QueryMappedSingle<GeneratedCustomer>(
                        "SELECT 'Fallback' AS Name;");

                    Assert.Equal(0, fallback.Id);
                    Assert.Equal("Fallback", fallback.Name);
                    Assert.Equal(1, FluentMapper.Registry.MaterializationPlanCacheEntryCount);
                }
            }
            finally
            {
                ResetMapper();
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GeneratedQueryMappedShouldMatchRuntimeFallbackForEquivalentComplexShapes()
        {
            ResetMapper();

            try
            {
                GeneratedImmutableCustomer generatedImmutable;
                GeneratedNestedCustomer generatedNested;
                GeneratedValueObjectCustomer generatedValueObject;
                GeneratedSameTerminalCustomer generatedSameTerminal;
                GeneratedProfileNestedCustomer generatedProfileNested;

                FluentMapper.Initialize(configuration => configuration.AddGeneratedMappings());

                using (var connection = OpenConnection())
                {
                    generatedImmutable = connection.QueryMappedSingle<GeneratedImmutableCustomer>(
                        "SELECT 21 AS immutable_id, 'Generated Constructor' AS name;");
                    generatedNested = connection.QueryMappedSingle<GeneratedNestedCustomer>(
                        "SELECT 22 AS customer_id, 'Sao Paulo' AS city;");
                    generatedValueObject = connection.QueryMappedSingle<GeneratedValueObjectCustomer>(
                        "SELECT 23 AS customer_id, '12345678909' AS cpf;");
                    generatedSameTerminal = connection.QueryMappedSingle<GeneratedSameTerminalCustomer>(
                        "SELECT 3 AS rank_level, 8 AS seniority_level;");
                    generatedProfileNested = connection.QueryMappedSingle<GeneratedProfileNestedCustomer, GeneratedLegacyProfile>(
                        "SELECT 'Profile City' AS legacy_city;");

                    Assert.Equal(0, FluentMapper.Registry.MaterializationPlanCacheEntryCount);
                }

                ResetMapper();

                FluentMapper.Initialize(configuration =>
                {
                    configuration.AddMap<GeneratedImmutableCustomerMap>();
                    configuration.AddMap<GeneratedNestedCustomerMap>();
                    configuration.AddMap<GeneratedValueObjectCustomerMap>();
                    configuration.AddMap<GeneratedSameTerminalCustomerMap>();
                    configuration.AddProfile<GeneratedLegacyProfileNestedCustomerMap>();
                });

                using (var connection = OpenConnection())
                {
                    var runtimeImmutable = connection.QueryMappedSingle<GeneratedImmutableCustomer>(
                        "SELECT 21 AS immutable_id, 'Generated Constructor' AS name;");
                    var runtimeNested = connection.QueryMappedSingle<GeneratedNestedCustomer>(
                        "SELECT 22 AS customer_id, 'Sao Paulo' AS city;");
                    var runtimeValueObject = connection.QueryMappedSingle<GeneratedValueObjectCustomer>(
                        "SELECT 23 AS customer_id, '12345678909' AS cpf;");
                    var runtimeSameTerminal = connection.QueryMappedSingle<GeneratedSameTerminalCustomer>(
                        "SELECT 3 AS rank_level, 8 AS seniority_level;");
                    var runtimeProfileNested = connection.QueryMappedSingle<GeneratedProfileNestedCustomer, GeneratedLegacyProfile>(
                        "SELECT 'Profile City' AS legacy_city;");

                    Assert.Equal(generatedImmutable.Id, runtimeImmutable.Id);
                    Assert.Equal(generatedImmutable.Name, runtimeImmutable.Name);
                    Assert.Equal(generatedNested.Id, runtimeNested.Id);
                    Assert.Equal(generatedNested.Address.City, runtimeNested.Address.City);
                    Assert.Equal(generatedValueObject.Id, runtimeValueObject.Id);
                    Assert.Equal(generatedValueObject.Cpf.Number, runtimeValueObject.Cpf.Number);
                    Assert.Equal(generatedSameTerminal.Rank.Level, runtimeSameTerminal.Rank.Level);
                    Assert.Equal(generatedSameTerminal.Seniority.Level, runtimeSameTerminal.Seniority.Level);
                    Assert.Equal(generatedProfileNested.Address.City, runtimeProfileNested.Address.City);
                    Assert.Equal(5, FluentMapper.Registry.MaterializationPlanCacheEntryCount);
                }
            }
            finally
            {
                ResetMapper();
            }
        }

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        private static void ResetMapper()
        {
            FluentMapper.Reset(
                typeof(GeneratedCustomer),
                typeof(GeneratedInternalCustomer),
                typeof(GeneratedBaseCustomer),
                typeof(GeneratedDerivedCustomer),
                typeof(GeneratedImmutableCustomer),
                typeof(GeneratedNullableCustomer),
                typeof(GeneratedNamingCustomer),
                typeof(GeneratedProfileCustomer),
                typeof(GeneratedNestedCustomer),
                typeof(GeneratedValueObjectCustomer),
                typeof(GeneratedSameTerminalCustomer),
                typeof(GeneratedProfileNestedCustomer));
        }
    }

    public sealed class GeneratedCustomer
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public sealed class GeneratedCustomerMap : EntityMap<GeneratedCustomer>
    {
        public GeneratedCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("customer_id");
        }
    }

    internal sealed class GeneratedInternalCustomer
    {
        public int Id { get; set; }
    }

    internal sealed class GeneratedInternalCustomerMap : EntityMap<GeneratedInternalCustomer>
    {
        public GeneratedInternalCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("internal_id");
        }
    }

    public class GeneratedBaseCustomer
    {
        public int Id { get; set; }
    }

    public sealed class GeneratedDerivedCustomer : GeneratedBaseCustomer
    {
        public string Name { get; set; }
    }

    public sealed class GeneratedBaseCustomerMap : EntityMap<GeneratedBaseCustomer>
    {
        public GeneratedBaseCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("base_id");
        }
    }

    public sealed class GeneratedDerivedCustomerMap : EntityMap<GeneratedDerivedCustomer>
    {
        public GeneratedDerivedCustomerMap()
        {
            IncludeBase<GeneratedBaseCustomer>();
            Map(customer => customer.Name).ToColumn("derived_name");
        }
    }

    public sealed class GeneratedImmutableCustomer
    {
        public GeneratedImmutableCustomer(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    public sealed class GeneratedImmutableCustomerMap : EntityMap<GeneratedImmutableCustomer>
    {
        public GeneratedImmutableCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("immutable_id");
            Map(customer => customer.Name).ToColumn("name");
        }
    }

    public sealed class GeneratedNullableCustomer
    {
        public int? Age { get; set; }

        public string Note { get; set; }
    }

    public sealed class GeneratedNullableCustomerMap : EntityMap<GeneratedNullableCustomer>
    {
        public GeneratedNullableCustomerMap()
        {
            Map(customer => customer.Age).ToColumn("age");
            Map(customer => customer.Note).ToColumn("note");
        }
    }

    public sealed class GeneratedNamingCustomer
    {
        public DateTime CreatedAt { get; set; }
    }

    public sealed class GeneratedLegacyProfile : IMappingProfile
    {
    }

    public sealed class GeneratedProfileCustomer
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public sealed class GeneratedLegacyProfileCustomerMap : EntityMap<GeneratedProfileCustomer>, IProfileMap<GeneratedLegacyProfile>
    {
        public GeneratedLegacyProfileCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("legacy_id");
            Map(customer => customer.Name).ToColumn("legacy_name");
        }
    }

    public sealed class GeneratedNestedCustomer
    {
        public int Id { get; set; }

        public GeneratedAddress Address { get; set; }
    }

    public sealed class GeneratedAddress
    {
        public string City { get; set; }
    }

    public sealed class GeneratedNestedCustomerMap : EntityMap<GeneratedNestedCustomer>
    {
        public GeneratedNestedCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("customer_id");
            Map(customer => customer.Address.City).ToColumn("city");
        }
    }

    public sealed class GeneratedValueObjectCustomer
    {
        public GeneratedValueObjectCustomer(int id, GeneratedCpf cpf)
        {
            Id = id;
            Cpf = cpf;
        }

        public int Id { get; }

        public GeneratedCpf Cpf { get; }
    }

    public sealed class GeneratedCpf
    {
        public GeneratedCpf(string number)
        {
            Number = number;
        }

        public string Number { get; }
    }

    public sealed class GeneratedValueObjectCustomerMap : EntityMap<GeneratedValueObjectCustomer>
    {
        public GeneratedValueObjectCustomerMap()
        {
            Map(customer => customer.Id).ToColumn("customer_id");
            Map(customer => customer.Cpf.Number).ToColumn("cpf");
        }
    }

    public sealed class GeneratedSameTerminalCustomer
    {
        public GeneratedSameTerminalCustomer(GeneratedRank rank, GeneratedSeniority seniority)
        {
            Rank = rank;
            Seniority = seniority;
        }

        public GeneratedRank Rank { get; }

        public GeneratedSeniority Seniority { get; }
    }

    public sealed class GeneratedRank
    {
        public GeneratedRank(int level)
        {
            Level = level;
        }

        public int Level { get; }
    }

    public sealed class GeneratedSeniority
    {
        public GeneratedSeniority(int level)
        {
            Level = level;
        }

        public int Level { get; }
    }

    public sealed class GeneratedSameTerminalCustomerMap : EntityMap<GeneratedSameTerminalCustomer>
    {
        public GeneratedSameTerminalCustomerMap()
        {
            Map(customer => customer.Rank.Level).ToColumn("rank_level");
            Map(customer => customer.Seniority.Level).ToColumn("seniority_level");
        }
    }

    public sealed class GeneratedProfileNestedCustomer
    {
        public GeneratedAddress Address { get; set; }
    }

    public sealed class GeneratedLegacyProfileNestedCustomerMap :
        EntityMap<GeneratedProfileNestedCustomer>,
        IProfileMap<GeneratedLegacyProfile>
    {
        public GeneratedLegacyProfileNestedCustomerMap()
        {
            Map(customer => customer.Address.City).ToColumn("legacy_city");
        }
    }
}
