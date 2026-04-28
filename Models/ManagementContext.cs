using Microsoft.EntityFrameworkCore;

namespace Management.Models
{
    public class ManagementContext : DbContext
    {
        public ManagementContext(DbContextOptions<ManagementContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<Management.Models.HR> HRs { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure decimal precision for Employee.Salary
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasPrecision(18, 2);

            // Configure decimal precision for Payroll properties
            modelBuilder.Entity<Payroll>()
                .Property(p => p.BasicSalary)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Payroll>()
                .Property(p => p.Bonus)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Payroll>()
                .Property(p => p.Deductions)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Payroll>()
                .Property(p => p.NetSalary)
                .HasPrecision(18, 2);

            // Configure relationships if needed
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId);

            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId);

            modelBuilder.Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId);

            // Configure self-referencing relationship for Employee Supervisor
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Supervisor)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid cycles

            // Configure Notification relationship
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.SetNull); // If user is deleted, set RecipientId to null
        }
    }
}