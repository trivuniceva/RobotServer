using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using RobotServer.Models;

namespace RobotServer.Data
{
    public class RobotDbContext : DbContext
    {
        public RobotDbContext() : base("name=RobotDbContext") { }

        public DbSet<OperationLog> OperationLogs { get; set; }
    }

}
