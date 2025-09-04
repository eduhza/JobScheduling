using Microsoft.EntityFrameworkCore;

namespace JobScheduling.API;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
}
