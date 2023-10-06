using Microsoft.EntityFrameworkCore;
using WebStoreApiEF.Models;

namespace WebStoreApiEF.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options):base(options)
        {

        }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<Product> Products { get; set; }
    }
}
