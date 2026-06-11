using DialogService.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace DialogService.Database
{
    public class DialogDbContext(DbContextOptions<DialogDbContext> options) : DbContext(options)
    {
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<ChatUser> ChatUsers => Set<ChatUser>();
        public DbSet<MessageEntity> Messages => Set<MessageEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("app_dialogs");

            modelBuilder.Entity<Chat>(e =>
            {
                e.ToTable("chats");
                e.HasKey(c => c.Chat_id);
                e.Property(c => c.Chat_name).HasMaxLength(50);
                e.Property(c => c.Creation_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(c => c.Last_update_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<ChatUser>(e =>
            {
                e.ToTable("chat_users");
                e.HasKey(cu => new { cu.Chat_id, cu.User_id });
                e.Property(cu => cu.Creation_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<MessageEntity>(e =>
            {
                e.ToTable("messages");
                e.HasKey(m => new { m.Message_id, m.Chat_id });
                e.Property(m => m.Message).HasMaxLength(2000);
                e.Property(m => m.Creation_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
