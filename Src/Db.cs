using Microsoft.EntityFrameworkCore;

namespace LiBackgammon
{
    public sealed class Db : DbContext
    {
        public static string ConnectionString { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(ConnectionString);

        public DbSet<Game> Games { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Style> Styles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<UserSession> Sessions { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
