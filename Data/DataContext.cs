using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BotBot.Data
{
    public class DataContext :DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder Builder)
        {
            Builder.UseSqlServer("Server=(localdb)\\mssqllocaldb; Database = TelegramBot; Trusted_Connection=True");
        }
    }
}
