using LDTPlan.Models;
using LDTPlan.Utils;
using Microsoft.EntityFrameworkCore;

namespace LDTPlan.Data;

public class AppDbContext : DbContext
{
    public DbSet<PatientInfo> PatientInfos => Set<PatientInfo>();
    public DbSet<MainDisease> MainDiseases => Set<MainDisease>();
    public DbSet<SheetName> SheetNames => Set<SheetName>();
    public DbSet<PlanTemplate> Templates => Set<PlanTemplate>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={AppConfig.DbPath}");
}
