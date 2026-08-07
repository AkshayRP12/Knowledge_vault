using KnowledgeVault.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeVault.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<ArticleTag>().HasKey(at => new { at.ArticleId, at.TagId });

            mb.Entity<ArticleTag>()
                .HasOne(at => at.Article).WithMany(a => a.ArticleTags).HasForeignKey(at => at.ArticleId);
            mb.Entity<ArticleTag>()
                .HasOne(at => at.Tag).WithMany(t => t.ArticleTags).HasForeignKey(at => at.TagId);

            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
            mb.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
            mb.Entity<Like>().HasIndex(l => new { l.UserId, l.ArticleId }).IsUnique();
            mb.Entity<Bookmark>().HasIndex(b => new { b.UserId, b.ArticleId }).IsUnique();

            SeedData(mb);
        }

        private static void SeedData(ModelBuilder mb)
        {
            mb.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Azure", Description = "Microsoft Azure cloud services" },
                new Category { Id = 2, Name = "React", Description = "React.js frontend development" },
                new Category { Id = 3, Name = "SQL", Description = "Database and SQL resources" },
                new Category { Id = 4, Name = "HR", Description = "Human Resources policies and guides" },
                new Category { Id = 5, Name = ".NET", Description = "ASP.NET Core and C# development" }
            );

            // Valid BCrypt hashes for Admin@123 and Employee@123
            string adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            string employeeHash = BCrypt.Net.BCrypt.HashPassword("Employee@123");

            mb.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@vault.com",
                    PasswordHash = adminHash,
                    Role = "Admin",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    Username = "employee",
                    Email = "employee@vault.com",
                    PasswordHash = employeeHash,
                    Role = "Employee",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            mb.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "Azure" },
                new Tag { Id = 2, Name = "React" },
                new Tag { Id = 3, Name = "SQL" },
                new Tag { Id = 4, Name = "HR" },
                new Tag { Id = 5, Name = ".NET" },
                new Tag { Id = 6, Name = "Docker" },
                new Tag { Id = 7, Name = "API" },
                new Tag { Id = 8, Name = "Security" }
            );

            mb.Entity<Article>().HasData(
                new Article
                {
                    Id = 1,
                    Title = "Getting Started with Azure App Service",
                    Content = "Azure App Service is a fully managed platform for building, deploying, and scaling web apps.\n\nIt supports multiple programming languages including .NET, Java, Node.js, Python, and PHP.\n\nYou can deploy directly from GitHub, Azure DevOps, or via ZIP deployment. The service handles OS patching, capacity provisioning, and load balancing automatically.\n\nKey features include:\n- Custom domains and SSL certificates\n- Auto-scaling based on traffic\n- Deployment slots for staging environments\n- Built-in authentication with Azure AD",
                    Status = "Approved",
                    AuthorId = 1,
                    CategoryId = 1,
                    CreatedAt = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new Article
                {
                    Id = 2,
                    Title = "React Hooks: A Complete Guide",
                    Content = "React Hooks were introduced in React 16.8 and completely changed how we write React components.\n\nuseState allows functional components to have state. useEffect replaces lifecycle methods like componentDidMount and componentDidUpdate.\n\nCustom hooks let you extract reusable logic from components. For example, a useFetch hook can handle all data fetching logic cleanly.\n\nuseContext is used to consume context values without wrapping in a Consumer component. useReducer is great for complex state management as an alternative to useState.",
                    Status = "Approved",
                    AuthorId = 2,
                    CategoryId = 2,
                    CreatedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
                },
                new Article
                {
                    Id = 3,
                    Title = "SQL Server Performance Optimization Tips",
                    Content = "Database performance is critical for any enterprise application. Here are the most impactful SQL Server optimizations.\n\nIndexing is the single most important factor. Always index columns used in WHERE, JOIN, and ORDER BY clauses. Use the Database Engine Tuning Advisor to get index recommendations.\n\nAvoid SELECT * - always specify only the columns you need. This reduces I/O and network traffic significantly.\n\nUse stored procedures for frequently executed queries. They are compiled and cached by SQL Server, providing better performance than ad-hoc queries.",
                    Status = "Approved",
                    AuthorId = 1,
                    CategoryId = 3,
                    CreatedAt = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            mb.Entity<ArticleTag>().HasData(
                new ArticleTag { ArticleId = 1, TagId = 1 },
                new ArticleTag { ArticleId = 2, TagId = 2 },
                new ArticleTag { ArticleId = 3, TagId = 3 }
            );
        }
    }
}
