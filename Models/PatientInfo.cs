using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LDTPlan.Data;
using Microsoft.EntityFrameworkCore;

namespace LDTPlan.Models;

[Table("patient_info")]
[Index(nameof(PatientId), Name = DbInitializer.PatientIdIndexName)]
public class PatientInfo
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("patient_id")] public int? PatientId { get; set; }
    [Column("patient_name")] public string? PatientName { get; set; }
    [Column("kana")] public string? Kana { get; set; }
    [Column("gender")] public string? Gender { get; set; }
    [Column("birthdate")] public DateTime? Birthdate { get; set; }
    [Column("issue_date")] public DateTime? IssueDate { get; set; }
    [Column("issue_date_age")] public int? IssueDateAge { get; set; }
    [Column("doctor_id")] public int? DoctorId { get; set; }
    [Column("doctor_name")] public string? DoctorName { get; set; }
    [Column("department")] public string? Department { get; set; }
    [Column("department_id")] public int? DepartmentId { get; set; }
    [Column("main_diagnosis")] public string? MainDiagnosis { get; set; }
    [Column("creation_count")] public int? CreationCount { get; set; }
    [Column("target_weight")] public double? TargetWeight { get; set; }
    [Column("sheet_name")] public string? SheetName { get; set; }
    [Column("target_bp")] public string? TargetBp { get; set; }
    [Column("target_hba1c")] public string? TargetHba1c { get; set; }
    [Column("goal1")] public string? Goal1 { get; set; }
    [Column("goal2")] public string? Goal2 { get; set; }
    [Column("target_achievement")] public string? TargetAchievement { get; set; }
    [Column("diet1")] public string? Diet1 { get; set; }
    [Column("diet2")] public string? Diet2 { get; set; }
    [Column("diet3")] public string? Diet3 { get; set; }
    [Column("diet4")] public string? Diet4 { get; set; }
    [Column("diet_comment")] public string? DietComment { get; set; }
    [Column("exercise_prescription")] public string? ExercisePrescription { get; set; }
    [Column("exercise_time")] public string? ExerciseTime { get; set; }
    [Column("exercise_frequency")] public string? ExerciseFrequency { get; set; }
    [Column("exercise_intensity")] public string? ExerciseIntensity { get; set; }
    [Column("daily_activity")] public string? DailyActivity { get; set; }
    [Column("exercise_comment")] public string? ExerciseComment { get; set; }
    [Column("nonsmoker")] public bool? Nonsmoker { get; set; }
    [Column("smoking_cessation")] public bool? SmokingCessation { get; set; }
    [Column("other1")] public string? Other1 { get; set; }
    [Column("other2")] public string? Other2 { get; set; }
    [Column("ophthalmology")] public bool? Ophthalmology { get; set; }
    [Column("dental")] public bool? Dental { get; set; }
    [Column("cancer_screening")] public bool? CancerScreening { get; set; }
}
