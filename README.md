# MabiGuildHelper

## EF Core Entity Migration
Commnet out ``.AddSingleton<Bot>()`` in Program.cs and run the following command in Package Manager Console.
After edit entity:
```
Add-Migration <MigrationName>
```

Apply entity change:
```
Update-Database
```