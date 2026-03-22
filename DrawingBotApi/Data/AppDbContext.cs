using Microsoft.EntityFrameworkCore;
using DrawingBotApi.Models;

namespace DrawingBotApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Drawing> Drawings { get; set; }
    }
}