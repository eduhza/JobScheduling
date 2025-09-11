using Microsoft.EntityFrameworkCore;

namespace JobScheduling.API.Database;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
}

//public class MyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext>
//{
//    public MyDbContext CreateDbContext(string[] args)
//    {
//        var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//            .AddEnvironmentVariables()
//            .Build();

//        var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
//        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
//        return new MyDbContext(optionsBuilder.Options);
//    }
//}
