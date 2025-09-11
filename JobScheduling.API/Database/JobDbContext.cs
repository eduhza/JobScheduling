using Microsoft.EntityFrameworkCore;

namespace JobScheduling.API.Database;

public class JobDbContext(DbContextOptions<JobDbContext> options) : DbContext(options)
{
}

//public class JobDbContextFactory : IDesignTimeDbContextFactory<JobDbContext>
//{
//    public JobDbContext CreateDbContext(string[] args)
//    {
//        var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//            .AddEnvironmentVariables()
//            .Build();

//        var optionsBuilder = new DbContextOptionsBuilder<JobDbContext>();
//        optionsBuilder.UseNpgsql(configuration.GetConnectionString("TaskDbConnection"));
//        return new JobDbContext(optionsBuilder.Options);
//    }
//}