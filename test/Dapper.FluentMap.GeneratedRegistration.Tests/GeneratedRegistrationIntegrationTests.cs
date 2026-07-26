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
                    var named = connection.QuerySingle<GeneratedNamingCustomer>(
                        "SELECT '2026-07-26T10:30:00' AS created_at;");
                    var profiled = connection.QueryMappedSingle<GeneratedProfileCustomer, GeneratedLegacyProfile>(
                        "SELECT 11 AS legacy_id, 'Profiled' AS legacy_name;");

                    Assert.Equal(7, customer.Id);
                    Assert.Equal("Ada", customer.Name);
                    Assert.Equal(8, internalCustomer.Id);
                    Assert.Equal(9, derived.Id);
                    Assert.Equal("Lovelace", derived.Name);
                    Assert.Equal(10, immutable.Id);
                    Assert.Equal("Grace", immutable.Name);
                    Assert.Equal(new DateTime(2026, 7, 26, 10, 30, 0), named.CreatedAt);
                    Assert.Equal(11, profiled.Id);
                    Assert.Equal("Profiled", profiled.Name);
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
                typeof(GeneratedNamingCustomer),
                typeof(GeneratedProfileCustomer));
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
}
