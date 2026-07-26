# Dapper.FluentMap.Generators

Build-time source generator for Dapper.FluentMap mapping registration.

The generator discovers eligible `IEntityMap<TEntity>` implementations declared in the current compilation and emits an `AddGeneratedMappings()` extension method that registers them through the existing `AddMap<TMap>()` API.
