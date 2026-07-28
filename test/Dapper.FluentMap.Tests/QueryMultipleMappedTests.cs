using System;
using System.Data;
using System.Linq;
using Dapper;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.Materialization;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.FluentMap.Tests
{
    public class QueryMultipleMappedTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void QueryMultipleMappedShouldCreateReaderAndReadFirstResultSet()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new MappedCustomerMap()));

                using (var connection = OpenConnection())
                using (var multi = connection.QueryMultipleMapped(
                    "SELECT 1 AS customer_id, 'Ada' AS customer_name;"))
                {
                    var customers = multi.ReadMapped<MappedCustomer>().ToList();

                    Assert.Collection(
                        customers,
                        customer =>
                        {
                            Assert.Equal(1, customer.Id);
                            Assert.Equal("Ada", customer.Name);
                        });
                    Assert.True(multi.IsConsumed);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        public void ReadMappedShouldReadSequentialResultSets()
        {
            PreTest(typeof(MappedCustomer), typeof(MappedOrder));

            try
            {
                FluentMapper.Initialize(configuration =>
                {
                    configuration.AddMap(new MappedCustomerMap());
                    configuration.AddMap(new MappedOrderMap());
                });

                using (var reader = CreateReader(
                    CreateTable(
                        new[] { "customer_id", "customer_name" },
                        new object[] { 1, "Ada" },
                        new object[] { 2, "Grace" }),
                    CreateTable(
                        new[] { "order_id", "total" },
                        new object[] { 10, 12.5m })))
                using (var multi = new MappedGridReader(reader))
                {
                    var customers = multi.ReadMapped<MappedCustomer>().ToList();
                    var orders = multi.ReadMapped<MappedOrder>().ToList();

                    Assert.Collection(
                        customers,
                        first =>
                        {
                            Assert.Equal(1, first.Id);
                            Assert.Equal("Ada", first.Name);
                        },
                        second =>
                        {
                            Assert.Equal(2, second.Id);
                            Assert.Equal("Grace", second.Name);
                        });
                    Assert.Collection(
                        orders,
                        order =>
                        {
                            Assert.Equal(10, order.Id);
                            Assert.Equal(12.5m, order.Total);
                        });
                    Assert.True(multi.IsConsumed);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer), typeof(MappedOrder));
            }
        }

        [Fact]
        public void ReadMappedShouldUseProfileForCurrentResultSet()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddProfile<LegacyCustomerMap>());

                using (var reader = CreateReader(CreateTable(
                    new[] { "legacy_id", "legal_name" },
                    new object[] { 7, "Legacy Ltd." })))
                using (var multi = new MappedGridReader(reader))
                {
                    var customer = multi.ReadMapped<MappedCustomer, LegacyProfile>().Single();

                    Assert.Equal(7, customer.Id);
                    Assert.Equal("Legacy Ltd.", customer.Name);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        public void ReadMappedShouldReturnEmptyCollectionForEmptyResultSet()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new MappedCustomerMap()));

                using (var reader = CreateReader(CreateTable(new[] { "customer_id", "customer_name" })))
                using (var multi = new MappedGridReader(reader))
                {
                    var customers = multi.ReadMapped<MappedCustomer>().ToList();

                    Assert.Empty(customers);
                    Assert.True(multi.IsConsumed);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        public void ReadMappedShouldThrowAfterFinalResultSet()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new MappedCustomerMap()));

                using (var reader = CreateReader(CreateTable(
                    new[] { "customer_id", "customer_name" },
                    new object[] { 1, "Ada" })))
                using (var multi = new MappedGridReader(reader))
                {
                    Assert.Single(multi.ReadMapped<MappedCustomer>());

                    var exception = Assert.Throws<InvalidOperationException>(
                        () => multi.ReadMapped<MappedCustomer>());

                    Assert.Contains("no remaining result sets", exception.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        public void ReadMappedShouldThrowAfterDispose()
        {
            using (var reader = CreateReader(CreateTable(new[] { "Id" }, new object[] { 1 })))
            {
                var multi = new MappedGridReader(reader);

                multi.Dispose();

                Assert.Throws<ObjectDisposedException>(() => multi.ReadMapped<DefaultEntity>());
            }
        }

        [Fact]
        public void DisposeAfterPartialConsumptionShouldCloseReader()
        {
            PreTest(typeof(MappedCustomer), typeof(MappedOrder));

            try
            {
                FluentMapper.Initialize(configuration =>
                {
                    configuration.AddMap(new MappedCustomerMap());
                    configuration.AddMap(new MappedOrderMap());
                });

                var reader = CreateReader(
                    CreateTable(
                        new[] { "customer_id", "customer_name" },
                        new object[] { 1, "Ada" }),
                    CreateTable(
                        new[] { "order_id", "total" },
                        new object[] { 10, 12.5m }));
                var multi = new MappedGridReader(reader);

                Assert.Single(multi.ReadMapped<MappedCustomer>());

                multi.Dispose();

                Assert.True(reader.IsClosed);
                Assert.True(multi.IsConsumed);
                Assert.Throws<ObjectDisposedException>(() => multi.ReadMapped<MappedOrder>());
            }
            finally
            {
                PreTest(typeof(MappedCustomer), typeof(MappedOrder));
            }
        }

        [Fact]
        public void ReadMappedShouldDisposeReaderWhenMaterializationThrows()
        {
            PreTest(typeof(ThrowingCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new ThrowingCustomerMap()));

                var reader = CreateReader(CreateTable(
                    new[] { "customer_id", "cpf" },
                    new object[] { 1, string.Empty }));
                var multi = new MappedGridReader(reader);

                var exception = Assert.Throws<FluentMapConfigurationException>(
                    () => multi.ReadMapped<ThrowingCustomer>());

                Assert.IsType<ArgumentException>(exception.InnerException);
                Assert.True(reader.IsClosed);
                Assert.True(multi.IsConsumed);
                Assert.Throws<ObjectDisposedException>(() => multi.ReadMapped<ThrowingCustomer>());
            }
            finally
            {
                PreTest(typeof(ThrowingCustomer));
            }
        }

        [Fact]
        public void ReadMappedShouldUseGeneratedMaterializerWhenRegistered()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration =>
                {
                    configuration.AddMap(new MappedCustomerMap());
                    configuration.AddGeneratedMaterializer(
                        new[]
                        {
                            GeneratedMaterializerColumn.Map("customer_id", nameof(MappedCustomer.Id)),
                            GeneratedMaterializerColumn.Map("customer_name", nameof(MappedCustomer.Name))
                        },
                        record => new MappedCustomer
                        {
                            Id = Convert.ToInt32(record.GetValue(0)),
                            Name = "generated:" + Convert.ToString(record.GetValue(1))
                        });
                });

                using (var reader = CreateReader(CreateTable(
                    new[] { "customer_id", "customer_name" },
                    new object[] { 3, "Ada" })))
                using (var multi = new MappedGridReader(reader))
                {
                    var customer = multi.ReadMapped<MappedCustomer>().Single();

                    Assert.Equal(3, customer.Id);
                    Assert.Equal("generated:Ada", customer.Name);
                    Assert.Equal(0, FluentMapper.Registry.MaterializationPlanCacheEntryCount);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void QueryMultipleMappedShouldPassCommandParameters()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new MappedCustomerMap()));

                using (var connection = OpenConnection())
                using (var multi = connection.QueryMultipleMapped(
                    "SELECT @id AS customer_id, @name AS customer_name;",
                    new { id = 5, name = "Katherine" }))
                {
                    var customer = multi.ReadMapped<MappedCustomer>().Single();

                    Assert.Equal(5, customer.Id);
                    Assert.Equal("Katherine", customer.Name);
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void QueryMultipleMappedShouldPropagateTransaction()
        {
            PreTest(typeof(MappedCustomer));

            try
            {
                FluentMapper.Initialize(configuration => configuration.AddMap(new MappedCustomerMap()));

                using (var connection = OpenConnection())
                {
                    connection.Execute("CREATE TABLE customers (customer_id INTEGER NOT NULL, customer_name TEXT NOT NULL);");

                    using (var transaction = connection.BeginTransaction())
                    {
                        connection.Execute(
                            "INSERT INTO customers (customer_id, customer_name) VALUES (9, 'Transaction');",
                            transaction: transaction);

                        using (var multi = connection.QueryMultipleMapped(
                            "SELECT customer_id, customer_name FROM customers WHERE customer_id = @id;",
                            new { id = 9 },
                            transaction))
                        {
                            var customer = multi.ReadMapped<MappedCustomer>().Single();

                            Assert.Equal(9, customer.Id);
                            Assert.Equal("Transaction", customer.Name);
                        }

                        transaction.Rollback();
                    }
                }
            }
            finally
            {
                PreTest(typeof(MappedCustomer));
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void QueryMultipleMappedShouldCloseConnectionItOpened()
        {
            using (var connection = new SqliteConnection("Data Source=:memory:"))
            {
                var multi = connection.QueryMultipleMapped("SELECT 1 AS Id;");

                Assert.Equal(ConnectionState.Open, connection.State);

                multi.Dispose();

                Assert.Equal(ConnectionState.Closed, connection.State);
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void QueryMultipleMappedShouldKeepOpenConnectionOpenAfterDispose()
        {
            using (var connection = OpenConnection())
            {
                using (var multi = connection.QueryMultipleMapped("SELECT 1 AS Id;"))
                {
                    Assert.Equal(ConnectionState.Open, connection.State);
                }

                Assert.Equal(ConnectionState.Open, connection.State);
            }
        }

        private static DataTableReader CreateReader(params DataTable[] tables)
        {
            return new DataTableReader(tables);
        }

        private static DataTable CreateTable(string[] columns, params object[][] rows)
        {
            var table = new DataTable();
            foreach (var column in columns)
            {
                table.Columns.Add(column, typeof(object));
            }

            foreach (var row in rows)
            {
                table.Rows.Add(row);
            }

            return table;
        }

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        private static void PreTest(params Type[] types)
        {
            FluentMapper.Reset(types);
        }

        private sealed class LegacyProfile : IMappingProfile
        {
        }

        private sealed class DefaultEntity
        {
            public int Id { get; set; }
        }

        private sealed class MappedCustomer
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private sealed class MappedCustomerMap : EntityMap<MappedCustomer>
        {
            public MappedCustomerMap()
            {
                Map(customer => customer.Id).ToColumn("customer_id");
                Map(customer => customer.Name).ToColumn("customer_name");
            }
        }

        private sealed class LegacyCustomerMap : EntityMap<MappedCustomer>, IProfileMap<LegacyProfile>
        {
            public LegacyCustomerMap()
            {
                Map(customer => customer.Id).ToColumn("legacy_id");
                Map(customer => customer.Name).ToColumn("legal_name");
            }
        }

        private sealed class MappedOrder
        {
            public int Id { get; set; }

            public decimal Total { get; set; }
        }

        private sealed class MappedOrderMap : EntityMap<MappedOrder>
        {
            public MappedOrderMap()
            {
                Map(order => order.Id).ToColumn("order_id");
                Map(order => order.Total).ToColumn("total");
            }
        }

        private sealed class ThrowingCustomer
        {
            public ThrowingCustomer(int id, ThrowingCpf cpf)
            {
                Id = id;
                Cpf = cpf;
            }

            public int Id { get; }

            public ThrowingCpf Cpf { get; }
        }

        private sealed class ThrowingCustomerMap : EntityMap<ThrowingCustomer>
        {
            public ThrowingCustomerMap()
            {
                Map(customer => customer.Id).ToColumn("customer_id");
                Map(customer => customer.Cpf.Number).ToColumn("cpf");
            }
        }

        private sealed class ThrowingCpf
        {
            public ThrowingCpf(string number)
            {
                if (string.IsNullOrWhiteSpace(number))
                {
                    throw new ArgumentException("CPF cannot be empty.", nameof(number));
                }

                Number = number;
            }

            public string Number { get; }
        }
    }
}
