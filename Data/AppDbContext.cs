using System.Globalization;
using LDTPlan.Models;
using LDTPlan.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LDTPlan.Data;

public class AppDbContext : DbContext
{
    public DbSet<PatientInfo> PatientInfos => Set<PatientInfo>();
    public DbSet<MainDisease> MainDiseases => Set<MainDisease>();
    public DbSet<SheetName> SheetNames => Set<SheetName>();
    public DbSet<PlanTemplate> Templates => Set<PlanTemplate>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={AppConfig.DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Python版(SQLAlchemyのDate型)が読めるよう、日付列は時刻なしの yyyy-MM-dd で保存する
        var dateConverter = new ValueConverter<DateTime, string>(
            v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            v => DateTime.Parse(v, CultureInfo.InvariantCulture));

        modelBuilder.Entity<PatientInfo>().Property(p => p.Birthdate).HasConversion(dateConverter);
        modelBuilder.Entity<PatientInfo>().Property(p => p.IssueDate).HasConversion(dateConverter);
    }
}
