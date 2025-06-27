using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using truyenchu.Areas.Identity.Models.UserStory;
using truyenchu.Areas.ViewStory.Models.ReadingHistory;
using truyenchu.Models;

namespace truyenchu.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions option) : base(option) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<StoryCategory>(builder =>
            {
                builder.HasKey(x => new { x.StoryId, x.CategoryId });
            });

            builder.Entity<Story>(builder =>
                {
                    builder.HasIndex(x => x.StorySlug)
                            .IsUnique();
                });

            builder.Entity<Category>(builder =>
            {
                builder.HasIndex(x => x.CategorySlug)
                        .IsUnique();
            });

            builder.Entity<Author>(builder =>
            {
                builder.HasIndex(x => x.AuthorSlug)
                        .IsUnique();
            });
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryPhoto> StoryPhotos { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<StoryCategory> StoryCategories { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserStory> UserStories { get; set; }
        public DbSet<ReadingHistory> ReadingHistory { get; set; }
    }
}