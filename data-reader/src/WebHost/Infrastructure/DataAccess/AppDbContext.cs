using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace WebHost.Infrastructure.DataAccess
{
    public class AppDbContext : DbContext
    {

        public DbSet<User> Users { get; set; }

        public DbSet<Product> Products { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }


    public class User
    {

        string _password = string.Empty;
        [Key]
        public int UserId { get; set; }


        [EmailAddress]
        public string Email { get; set; } = null!;


        public string Password { get => _password; set { _password = GetHashString(_password); } } 


        public List<Product> Products { get; set; } = new List<Product>();



        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
        
        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }


    }


    public class Product
    {

        [Key]
        public int ProductId { get; set; }

        
        public string Url { get; set; } = null!;


        public string ProductName { get; set; } = null!;

        
        public bool IsLoaded { get; set; }


        public int LoadedById { get; set; }

        
        [ForeignKey(nameof(LoadedById))]
        public User User { get; set; } = null!;
    }


}
