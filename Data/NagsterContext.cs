using Microsoft.EntityFrameworkCore;
using NagsterApi.Models;

namespace NagsterApi.Data;

public class NagsterContext(DbContextOptions<NagsterContext> options) : DbContext(options)
{
    public DbSet<NagTask> Tasks => Set<NagTask>();
}
