using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace cosmos_ef_udf
{
  class Program
  {
    static async Task Main(string[] args)
    {
      string primaryKey;
      try
      {
        Console.WriteLine("Downloading the primary key from https://localhost:8081/_explorer/quickstart.html…");
        var html = await new HttpClient().GetStringAsync("https://localhost:8081/_explorer/quickstart.html");
        primaryKey = Regex.Match(html, "Primary Key</p>\\s+<input .* value=\"(?<primaryKey>.*)\"").Groups["primaryKey"].Value;
        Console.WriteLine("The primary key has been downloaded.");
      }
      catch
      {
        Console.WriteLine("Failed to download the primary key. Make sure to install and run the Cosmos emulator.");
        Console.WriteLine("The primary key gets downloaded from https://localhost:8081/_explorer/quickstart.html");
        return;
      }

      Guid userId;
      using (var appDbContext = new AppDbContext(primaryKey))
      {
        await appDbContext.Database.EnsureDeletedAsync();
        await appDbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("The database has been reset.");
        var user = new User
        {
          FirstName = "Tomas",
          LastName = "Hubelbauer",
          Cars = new List<Car>() {
                new Car {
                    Make = "Tesla",
                    Model = "3",
                    Trips = new List<Trip>(),
                }
            },
        };

        await appDbContext.Users.AddAsync(user);
        await appDbContext.SaveChangesAsync();
        userId = user.Id;
        Console.WriteLine("The database has been seeded. See at https://localhost:8081/_explorer/index.html");
      }

      var client = new DocumentClient(new Uri("https://localhost:8081"), primaryKey);

      // TODO: Find a better way to recreate the UDF each time, `Replace…` seem to be too complex for now
      try
      {
        await client.DeleteUserDefinedFunctionAsync(UriFactory.CreateUserDefinedFunctionUri(nameof(cosmos_ef_udf), nameof(AppDbContext), "test"));
      }
      catch (System.Exception)
      {
        Console.WriteLine("UDF didn't exist yet");
      }

      await client.CreateUserDefinedFunctionAsync(UriFactory.CreateDocumentCollectionUri(nameof(cosmos_ef_udf), nameof(AppDbContext)), new UserDefinedFunction
      {
        Id = "test",
        Body = "function test() { return 'test'; }",
      });

      var users = client.CreateDocumentQuery<dynamic>(
        UriFactory.CreateDocumentCollectionUri(nameof(cosmos_ef_udf), nameof(AppDbContext)),
        $"SELECT {{ item: collection, test: udf.test() }} FROM AppDbContext collection");

      foreach (var result in users)
      {
        Console.WriteLine(result);
      }

      using (var appDbContext = new AppDbContext(primaryKey))
      {
        Console.WriteLine(userId);
        var user = await appDbContext.Users.Include(u => u.Cars).SingleAsync(u => u.Id == userId);
        Console.WriteLine(user);
      }
    }
  }

  public class AppDbContext : DbContext
  {
    private string primaryKey;

    public AppDbContext(string primaryKey)
    {
      this.primaryKey = primaryKey;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Trip> Trips { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseCosmos("https://localhost:8081", this.primaryKey, nameof(cosmos_ef_udf));
    }
  }

  public class User
  {
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Car> Cars { get; set; }
  }

  public class Car
  {
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public ICollection<Trip> Trips { get; set; }
  }

  public class Trip
  {
    public Guid Id { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; }
    public int DistanceInKilometers { get; set; }
  }
}
