# MabiGuildHelper

## EF Core Entity Migration
Commnet out all ``.AddSingleton<>()``, ``AddScoped<>`` in Program.cs and run the following command in Package Manager Console.
After edit entity:
```
Add-Migration <MigrationName>
```