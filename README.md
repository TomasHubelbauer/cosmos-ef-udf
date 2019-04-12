# CosmosDB EF UDF

- `dotnet new console`
- `dotnet add package Microsoft.EntityFrameworkCore.Cosmos`
- `Program.cs`: create `AppDbContext`, `User`, `Car`, `Trip`
- Start the CosmosDB emulator and ensure it runs on `https://localhost:8081`
- Switch to the latest C# language version so that we can use `async Main`: `<LangVersion>latest</LangVersion>`
- https://localhost:8081/_explorer/index.html, select the DB and click New UDF OR:
- `dotnet add package Microsoft.Azure.DocumentDB.Core`
- https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-use-stored-procedures-triggers-udfs#udfs
- https://github.com/aspnet/EntityFrameworkCore/issues/15338
