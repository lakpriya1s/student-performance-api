// Data/TaskContext.cs
using Microsoft.EntityFrameworkCore;

public class TaskContext : DbContext
{
    public TaskContext(DbContextOptions<TaskContext> options) : base(options) { }
    public DbSet<TaskEntry> Tasks { get; set; }
    public DbSet<SubjectEntry> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectEntry>().ToTable("SubjectEntry");
        modelBuilder.Entity<TaskEntry>().ToTable("TaskEntry");
    }
}