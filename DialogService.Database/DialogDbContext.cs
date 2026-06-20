using DialogService.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace DialogService.Database
{
    public class DialogDbContext(DbContextOptions options) : DbContext(options)
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
                e.HasIndex(cu => cu.User_id).HasDatabaseName("chat_users_userid_idx");
            });

            modelBuilder.Entity<MessageEntity>(e =>
            {
                e.ToTable("messages");
                e.HasKey(m => m.Message_id);
                e.Property(m => m.Message).HasMaxLength(2000);
                e.Property(m => m.User_name).HasMaxLength(50);
                e.Property(m => m.Creation_datetime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(m => m.Status).HasDefaultValue(0);
                e.HasIndex(m => m.Chat_id).HasDatabaseName("messages_chatid_idx");
            });
        }
    }
}
