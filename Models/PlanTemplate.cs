using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LDTPlan.Models;

[Table("templates")]
public class PlanTemplate
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("main_disease")] public string? MainDisease { get; set; }
    [Column("sheet_name")] public string? SheetName { get; set; }
    [Column("target_bp")] public string? TargetBp { get; set; }
    [Column("target_hba1c")] public string? TargetHba1c { get; set; }
    [Column("goal1")] public string? Goal1 { get; set; }
    [Column("goal2")] public string? Goal2 { get; set; }
    [Column("diet1")] public string? Diet1 { get; set; }
    [Column("diet2")] public string? Diet2 { get; set; }
    [Column("diet3")] public string? Diet3 { get; set; }
    [Column("diet4")] public string? Diet4 { get; set; }
    [Column("exercise_prescription")] public string? ExercisePrescription { get; set; }
    [Column("exercise_time")] public string? ExerciseTime { get; set; }
    [Column("exercise_frequency")] public string? ExerciseFrequency { get; set; }
    [Column("exercise_intensity")] public string? ExerciseIntensity { get; set; }
    [Column("daily_activity")] public string? DailyActivity { get; set; }
    [Column("other1")] public string? Other1 { get; set; }
    [Column("other2")] public string? Other2 { get; set; }
}
