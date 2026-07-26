using System;
using System.Linq;
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.Naming;

#if AOT_SMOKE_SCANNING
const string scenario = "scanning";
FluentMapper.Initialize(configuration => configuration.AddMapsFromAssemblyContaining<CustomerMap>());

AssertMappedMember<Customer>("customer_id", nameof(Customer.Id));
#else
const string scenario = "explicit";
FluentMapper.Initialize(configuration =>
{
    configuration.AddMap<CustomerMap>();
    configuration.AddMap<ImmutableCustomerMap>();
    configuration.UseNamingPolicy(NamingPolicy.SnakeCase).ForEntity<NamingCustomer>();
});

AssertMappedMember<Customer>("customer_id", nameof(Customer.Id));
AssertMappedMember<NamingCustomer>("created_at", nameof(NamingCustomer.CreatedAt));
AssertConstructorMapping();
AssertExplain();
#endif

Console.WriteLine(scenario + ":ok");

static void AssertMappedMember<TEntity>(string columnName, string propertyName)
{
    var member = SqlMapper.GetTypeMap(typeof(TEntity)).GetMember(columnName);
    if (member?.Property?.Name != propertyName)
    {
        throw new InvalidOperationException(
            $"Column '{columnName}' was not mapped to property '{propertyName}'.");
    }
}

#if !AOT_SMOKE_SCANNING
static void AssertConstructorMapping()
{
    var typeMap = SqlMapper.GetTypeMap(typeof(ImmutableCustomer));
    var constructor = typeMap.FindConstructor(
        new[] { "customer_id", "name" },
        new[] { typeof(int), typeof(string) });

    if (constructor == null)
    {
        throw new InvalidOperationException("Constructor mapping was not resolved.");
    }

    var member = typeMap.GetConstructorParameter(constructor, "customer_id");
    if (member?.Parameter?.Name != "id")
    {
        throw new InvalidOperationException("Constructor parameter mapping was not resolved.");
    }
}

static void AssertExplain()
{
    var explanation = FluentMapper.Explain<Customer>();
    if (!explanation.Members.Any(member =>
            member.MemberPath == nameof(Customer.Id) &&
            member.ColumnName == "customer_id"))
    {
        throw new InvalidOperationException("Explain did not include the explicit mapping.");
    }
}
#endif

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
    }
}

public sealed class NamingCustomer
{
    public DateTime CreatedAt { get; set; }
}

public sealed class ImmutableCustomer
{
    public ImmutableCustomer(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }
}

public sealed class ImmutableCustomerMap : EntityMap<ImmutableCustomer>
{
    public ImmutableCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Name).ToColumn("name");
    }
}
