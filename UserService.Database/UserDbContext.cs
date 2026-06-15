using Microsoft.EntityFrameworkCore;
using UserService.Database.Entities;

namespace UserService.Database
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<FriendEntity> Friends => Set<FriendEntity>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<FeedOutboxEntity> FeedOutbox => Set<FeedOutboxEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("app_users");

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(u => u.User_id);
                e.Property(u => u.User_id).ValueGeneratedNever();
                e.Property(u => u.First_name).HasMaxLength(30);
                e.Property(u => u.Second_name).HasMaxLength(30);
                e.Property(u => u.Birthdate).HasMaxLength(11);
                e.Property(u => u.Biography).HasMaxLength(1000);
                e.Property(u => u.City).HasMaxLength(255);
                e.Property(u => u.Password).HasMaxLength(255);
                e.Property(u => u.Login).HasMaxLength(50);
                e.Property(u => u.Who_can_message).HasDefaultValue(0);
                e.HasIndex(u => u.Login).IsUnique().HasDatabaseName("users_login_idx");
                e.HasIndex(u => new { u.First_name, u.Second_name })
                    .HasDatabaseName("users_fname_sname_idx")
                    .HasMethod("btree");
            });

            modelBuilder.Entity<FriendEntity>(e =>
            {
                e.ToTable("friends");
                e.HasKey(f => new { f.User_id, f.Friend_id });
                e.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.User_id)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(f => f.Friend)
                    .WithMany()
                    .HasForeignKey(f => f.Friend_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Post>(e =>
            {
                e.ToTable("posts");
                e.HasKey(p => p.Post_id);
                e.Property(p => p.Text).HasColumnName("post").HasMaxLength(2000);
                e.Property(p => p.Creation_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasIndex(p => p.User_id).HasDatabaseName("posts_userid_idx");
                e.Ignore(p => p.AuthorFirstName);
                e.Ignore(p => p.AuthorSecondName);
            });

            modelBuilder.Entity<FeedOutboxEntity>(e =>
            {
                e.ToTable("feed_outbox");
                e.HasKey(o => o.Id);
                e.Property(o => o.Id).ValueGeneratedOnAdd();
                e.Property(o => o.Created_at).HasDefaultValueSql("NOW()");
            });
        }
    }
}
