using AIS.Models;
using Microsoft.EntityFrameworkCore;

namespace AIS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .HasMaxLength(64)
                .UseCollation("NOCASE");

            entity.Property(user => user.FullName)
                .HasMaxLength(128);

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.HasMany(user => user.Shifts)
                .WithOne(shift => shift.User)
                .HasForeignKey(shift => shift.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(shift => shift.Id);

            entity.Property(shift => shift.StartTime).IsRequired();
            entity.Property(shift => shift.IsActive).HasDefaultValue(false);

            entity.HasIndex(shift => new { shift.UserId, shift.StartTime });

            entity.ToTable(tableBuilder =>
                tableBuilder.HasCheckConstraint(
                    "CK_Shifts_EndTime_After_StartTime",
                    "\"EndTime\" IS NULL OR \"EndTime\" >= \"StartTime\""));
        });
    }
}
