using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotBot.Data
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string UserName {  get; set; }
        public string? GroupName { get; set; } = null;
        public bool WaitToChangeGroup { get; set; } = false;
    }
}