using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace TrainingRequestApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<TrainingRequest> TrainingRequests { get; set; }
        public DbSet<TrainingParticipant> TrainingParticipants { get; set; }
        public DbSet<TrainingRequestCost> TrainingRequestCosts { get; set; }
    }
}
