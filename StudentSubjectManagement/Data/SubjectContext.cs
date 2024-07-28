// Data/SubjectContext.cs
using Microsoft.EntityFrameworkCore;

public class SubjectContext : DbContext
{
    public SubjectContext(DbContextOptions<SubjectContext> options) : base(options) { }
    public DbSet<SubjectEntry> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectEntry>().ToTable("SubjectEntry");
    }
}