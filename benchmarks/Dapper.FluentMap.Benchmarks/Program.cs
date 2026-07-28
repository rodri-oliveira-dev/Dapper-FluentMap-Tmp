using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Dapper;
using Dapper.FluentMap.Mapping;
using Microsoft.Data.Sqlite;

namespace Dapper.FluentMap.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithArtifactsPath(Path.Combine(".tmp", "benchmarks", "BenchmarkDotNet.Artifacts"));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}

[MemoryDiagnoser]
[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MaterializationSteadyStateBenchmarks
{
    private const int RowCount = 1000;

    private SqliteConnection _connection = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        SQLitePCL.Batteries_V2.Init();
        ResetPublicFluentState();

        FluentMapper.Initialize(configuration =>
        {
            configuration.AddGeneratedMappings();
        });

        _connection = OpenPopulatedConnection();

        DapperPure();
        DapperWithFluentMapRootMapping();
        QueryMappedSimple();
        QueryMappedSimpleRuntimeFallback();
        QueryMappedImmutableConstructor();
        QueryMappedNestedObject();
        QueryMappedNestedObjectRuntimeFallback();
        QueryMappedValueObject();
        QueryMappedValueObjectRuntimeFallback();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
        ResetPublicFluentState();
    }

    [Benchmark(Baseline = true)]
    public int DapperPure()
    {
        return _connection.Query<PureCustomer>(
                "SELECT Id, Name, Age, Balance, CreatedAt FROM BenchmarkRows;")
            .AsList()
            .Count;
    }

    [Benchmark]
    public int DapperWithFluentMapRootMapping()
    {
        return _connection.Query<RootMappedCustomer>(
                "SELECT Id AS person_id, Name AS full_name, Age AS customer_age, Balance AS account_balance, CreatedAt AS created_at FROM BenchmarkRows;")
            .AsList()
            .Count;
    }

    [Benchmark]
    public int QueryMappedSimple()
    {
        return _connection.QueryMapped<QueryMappedSimpleCustomer>(
                "SELECT Id AS customer_id, Name AS full_name, Age AS customer_age, Balance AS account_balance, CreatedAt AS created_at FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedSimpleRuntimeFallback()
    {
        return _connection.QueryMapped<QueryMappedSimpleCustomer>(
                "SELECT Name AS full_name, Id AS customer_id, Age AS customer_age, Balance AS account_balance, CreatedAt AS created_at FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedImmutableConstructor()
    {
        return _connection.QueryMapped<ImmutableCustomer>(
                "SELECT Id AS customer_id, Name AS full_name, Age AS customer_age, Balance AS account_balance, CreatedAt AS created_at FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedNestedObject()
    {
        return _connection.QueryMapped<NestedCustomer>(
                "SELECT Id AS customer_id, Name AS full_name, City AS city, PostalCode AS postal_code, Country AS country FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedNestedObjectRuntimeFallback()
    {
        return _connection.QueryMapped<NestedCustomer>(
                "SELECT City AS city, Id AS customer_id, Name AS full_name, PostalCode AS postal_code, Country AS country FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedValueObject()
    {
        return _connection.QueryMapped<ValueObjectCustomer>(
                "SELECT Id AS customer_id, Cpf AS cpf, Balance AS amount, Currency AS currency FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedValueObjectRuntimeFallback()
    {
        return _connection.QueryMapped<ValueObjectCustomer>(
                "SELECT Cpf AS cpf, Id AS customer_id, Balance AS amount, Currency AS currency FROM BenchmarkRows;")
            .Count();
    }

    private static SqliteConnection OpenPopulatedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        CreateRows(connection);
        return connection;
    }

    private static void CreateRows(IDbConnection connection)
    {
        connection.Execute(
            @"CREATE TABLE BenchmarkRows (
                Id INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Balance REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                City TEXT NOT NULL,
                PostalCode TEXT NOT NULL,
                Country TEXT NOT NULL,
                Cpf TEXT NOT NULL,
                Currency TEXT NOT NULL
            );");

        connection.Execute(
            @"WITH RECURSIVE numbers(Value) AS (
                SELECT 1
                UNION ALL
                SELECT Value + 1 FROM numbers WHERE Value < @RowCount
            )
            INSERT INTO BenchmarkRows (
                Id,
                Name,
                Age,
                Balance,
                CreatedAt,
                City,
                PostalCode,
                Country,
                Cpf,
                Currency
            )
            SELECT
                Value,
                'Customer ' || Value,
                18 + (Value % 50),
                1000.25 + Value,
                strftime('%Y-%m-%dT%H:%M:%f', '2020-01-01', '+' || Value || ' minutes'),
                'City ' || (Value % 17),
                printf('%05d', Value),
                'BR',
                printf('%011d', Value),
                'BRL'
            FROM numbers;",
            new { RowCount });
    }

    private static void ResetPublicFluentState()
    {
        FluentMapper.EntityMaps.Clear();
        FluentMapper.TypeConventions.Clear();

        foreach (var type in BenchmarkTypes.AllBenchmarkTypes)
        {
            SqlMapper.SetTypeMap(type, null);
        }
    }
}

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 8, warmupCount: 0, iterationCount: 1)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MaterializationColdStartBenchmarks
{
    [Benchmark(Baseline = true)]
    public int DapperPureColdStart()
    {
        SQLitePCL.Batteries_V2.Init();

        using var connection = MaterializationBenchmarkDatabase.OpenPopulatedConnection();
        return connection.Query<ColdPureCustomer>(
                "SELECT Id, Name, Age, Balance, CreatedAt FROM BenchmarkRows;")
            .AsList()
            .Count;
    }

    [Benchmark]
    public int FluentMapRootMappingColdStart()
    {
        SQLitePCL.Batteries_V2.Init();
        ResetColdPublicState();

        FluentMapper.Initialize(configuration => configuration.AddMap(new ColdRootMappedCustomerMap()));

        using var connection = MaterializationBenchmarkDatabase.OpenPopulatedConnection();
        return connection.Query<ColdRootMappedCustomer>(
                "SELECT Id AS person_id, Name AS full_name, Age AS customer_age, Balance AS account_balance, CreatedAt AS created_at FROM BenchmarkRows;")
            .AsList()
            .Count;
    }

    [Benchmark]
    public int QueryMappedNestedColdStart()
    {
        SQLitePCL.Batteries_V2.Init();
        ResetColdPublicState();

        FluentMapper.Initialize(configuration => configuration.AddMap(new ColdNestedCustomerMap()));

        using var connection = MaterializationBenchmarkDatabase.OpenPopulatedConnection();
        return connection.QueryMapped<ColdNestedCustomer>(
                "SELECT Id AS customer_id, Name AS full_name, City AS city, PostalCode AS postal_code, Country AS country FROM BenchmarkRows;")
            .Count();
    }

    [Benchmark]
    public int QueryMappedValueObjectColdStart()
    {
        SQLitePCL.Batteries_V2.Init();
        ResetColdPublicState();

        FluentMapper.Initialize(configuration => configuration.AddMap(new ColdValueObjectCustomerMap()));

        using var connection = MaterializationBenchmarkDatabase.OpenPopulatedConnection();
        return connection.QueryMapped<ColdValueObjectCustomer>(
                "SELECT Id AS customer_id, Cpf AS cpf, Balance AS amount, Currency AS currency FROM BenchmarkRows;")
            .Count();
    }

    private static void ResetColdPublicState()
    {
        FluentMapper.EntityMaps.Clear();
        FluentMapper.TypeConventions.Clear();

        foreach (var type in BenchmarkTypes.AllBenchmarkTypes)
        {
            SqlMapper.SetTypeMap(type, null);
        }
    }
}

internal static class MaterializationBenchmarkDatabase
{
    internal static SqliteConnection OpenPopulatedConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        CreateRows(connection);
        return connection;
    }

    private static void CreateRows(IDbConnection connection)
    {
        connection.Execute(
            @"CREATE TABLE BenchmarkRows (
                Id INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Balance REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                City TEXT NOT NULL,
                PostalCode TEXT NOT NULL,
                Country TEXT NOT NULL,
                Cpf TEXT NOT NULL,
                Currency TEXT NOT NULL
            );");

        connection.Execute(
            @"WITH RECURSIVE numbers(Value) AS (
                SELECT 1
                UNION ALL
                SELECT Value + 1 FROM numbers WHERE Value < 1000
            )
            INSERT INTO BenchmarkRows (
                Id,
                Name,
                Age,
                Balance,
                CreatedAt,
                City,
                PostalCode,
                Country,
                Cpf,
                Currency
            )
            SELECT
                Value,
                'Customer ' || Value,
                18 + (Value % 50),
                1000.25 + Value,
                strftime('%Y-%m-%dT%H:%M:%f', '2020-01-01', '+' || Value || ' minutes'),
                'City ' || (Value % 17),
                printf('%05d', Value),
                'BR',
                printf('%011d', Value),
                'BRL'
            FROM numbers;");
    }
}

internal static class BenchmarkTypes
{
    internal static readonly Type[] AllMappedTypes =
    {
        typeof(RootMappedCustomer),
        typeof(QueryMappedSimpleCustomer),
        typeof(ImmutableCustomer),
        typeof(NestedCustomer),
        typeof(ValueObjectCustomer)
    };

    internal static readonly Type[] AllColdTypes =
    {
        typeof(ColdRootMappedCustomer),
        typeof(ColdNestedCustomer),
        typeof(ColdValueObjectCustomer)
    };

    internal static readonly Type[] AllBenchmarkTypes = AllMappedTypes
        .Concat(AllColdTypes)
        .ToArray();
}

public sealed class PureCustomer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class RootMappedCustomer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class RootMappedCustomerMap : EntityMap<RootMappedCustomer>
{
    public RootMappedCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("person_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Age).ToColumn("customer_age");
        Map(customer => customer.Balance).ToColumn("account_balance");
        Map(customer => customer.CreatedAt).ToColumn("created_at");
    }
}

public sealed class QueryMappedSimpleCustomer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class QueryMappedSimpleCustomerMap : EntityMap<QueryMappedSimpleCustomer>
{
    public QueryMappedSimpleCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Age).ToColumn("customer_age");
        Map(customer => customer.Balance).ToColumn("account_balance");
        Map(customer => customer.CreatedAt).ToColumn("created_at");
    }
}

public sealed class ImmutableCustomer
{
    public ImmutableCustomer(int id, string fullName, int age, decimal balance, DateTime createdAt)
    {
        Id = id;
        FullName = fullName;
        Age = age;
        Balance = balance;
        CreatedAt = createdAt;
    }

    public int Id { get; }

    public string FullName { get; }

    public int Age { get; }

    public decimal Balance { get; }

    public DateTime CreatedAt { get; }
}

public sealed class ImmutableCustomerMap : EntityMap<ImmutableCustomer>
{
    public ImmutableCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Age).ToColumn("customer_age");
        Map(customer => customer.Balance).ToColumn("account_balance");
        Map(customer => customer.CreatedAt).ToColumn("created_at");
    }
}

public sealed class NestedCustomer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public NestedAddress Address { get; set; } = null!;
}

public sealed class NestedAddress
{
    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

public sealed class NestedCustomerMap : EntityMap<NestedCustomer>
{
    public NestedCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Address.City).ToColumn("city");
        Map(customer => customer.Address.PostalCode).ToColumn("postal_code");
        Map(customer => customer.Address.Country).ToColumn("country");
    }
}

public sealed class ValueObjectCustomer
{
    public ValueObjectCustomer(int id, BenchmarkCpf cpf, BenchmarkMoney balance)
    {
        Id = id;
        Cpf = cpf;
        Balance = balance;
    }

    public int Id { get; }

    public BenchmarkCpf Cpf { get; }

    public BenchmarkMoney Balance { get; }
}

public sealed class BenchmarkCpf
{
    public BenchmarkCpf(string number)
    {
        Number = number;
    }

    public string Number { get; }
}

public sealed class BenchmarkMoney
{
    public BenchmarkMoney(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }
}

public sealed class ValueObjectCustomerMap : EntityMap<ValueObjectCustomer>
{
    public ValueObjectCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Cpf.Number).ToColumn("cpf");
        Map(customer => customer.Balance.Amount).ToColumn("amount");
        Map(customer => customer.Balance.Currency).ToColumn("currency");
    }
}

public sealed class ColdPureCustomer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class ColdRootMappedCustomer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class ColdRootMappedCustomerMap : EntityMap<ColdRootMappedCustomer>
{
    public ColdRootMappedCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("person_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Age).ToColumn("customer_age");
        Map(customer => customer.Balance).ToColumn("account_balance");
        Map(customer => customer.CreatedAt).ToColumn("created_at");
    }
}

public sealed class ColdNestedCustomer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public ColdNestedAddress Address { get; set; } = null!;
}

public sealed class ColdNestedAddress
{
    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

public sealed class ColdNestedCustomerMap : EntityMap<ColdNestedCustomer>
{
    public ColdNestedCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
        Map(customer => customer.Address.City).ToColumn("city");
        Map(customer => customer.Address.PostalCode).ToColumn("postal_code");
        Map(customer => customer.Address.Country).ToColumn("country");
    }
}

public sealed class ColdValueObjectCustomer
{
    public ColdValueObjectCustomer(int id, ColdBenchmarkCpf cpf, ColdBenchmarkMoney balance)
    {
        Id = id;
        Cpf = cpf;
        Balance = balance;
    }

    public int Id { get; }

    public ColdBenchmarkCpf Cpf { get; }

    public ColdBenchmarkMoney Balance { get; }
}

public sealed class ColdBenchmarkCpf
{
    public ColdBenchmarkCpf(string number)
    {
        Number = number;
    }

    public string Number { get; }
}

public sealed class ColdBenchmarkMoney
{
    public ColdBenchmarkMoney(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }
}

public sealed class ColdValueObjectCustomerMap : EntityMap<ColdValueObjectCustomer>
{
    public ColdValueObjectCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Cpf.Number).ToColumn("cpf");
        Map(customer => customer.Balance.Amount).ToColumn("amount");
        Map(customer => customer.Balance.Currency).ToColumn("currency");
    }
}
