# Dapper.FluentMap.Generators

Build-time source generator for Dapper.FluentMap mapping registration.

The generator discovers eligible `IEntityMap<TEntity>` implementations declared in the current compilation and emits an `AddGeneratedMappings()` extension method that registers them through the existing `AddMap<TMap>()` API.

```bash
dotnet add package Dapper.FluentMap.Generators
```

```csharp
using Dapper.FluentMap;

FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

Generated registration avoids reflection-based assembly scanning for maps in the current compilation. It does not scan referenced assemblies, execute map constructors during generation, generate database materializers or replace `FluentMapper.Validate()`.
