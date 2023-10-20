using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Db.Entity;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Db
{
    public class AppDbContext : DbContext
    {
        public static readonly string ConnectionStringName = "MabiDb";

        public DbSet<Guild> Guilds { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions option) : base(option) { }
    }
}
