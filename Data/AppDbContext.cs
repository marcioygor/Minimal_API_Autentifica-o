using Microsoft.EntityFrameworkCore;
using Minimal_API.Models;


namespace Minimal_API.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Todo> Todos { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("DataSource=app.db; Cache=Shared");

    }
}