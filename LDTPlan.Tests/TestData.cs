using LDTPlan.Models;
using LDTPlan.Services;

namespace LDTPlan.Tests;

public static class TestData
{
    /// <summary>全列に値を入れた計画書（列の複写漏れを検出できるよう null を残さない）</summary>
    public static PatientInfo FullPatientInfo(int patientId = 123) => new()
    {
        PatientId = patientId,
        PatientName = "山田太郎",
        Kana = "ヤマダタロウ",
        Gender = "男性",
        Birthdate = new DateTime(1960, 3, 15),
        IssueDate = new DateTime(2026, 4, 1),
        IssueDateAge = 66,
        DoctorId = 10,
        DoctorName = "佐藤医師",
        Department = "内科",
        DepartmentId = 3,
        MainDiagnosis = "高血圧症",
        CreationCount = 1,
        TargetWeight = 65.5,
        SheetName = "1_血圧130-80以下",
        TargetBp = "130/80",
        TargetHba1c = "7.0",
        Goal1 = "達成目標, \"引用符\"とカンマを含む",
        Goal2 = "行動目標",
        TargetAchievement = "概ね達成",
        Diet1 = "食事量を適正にする",
        Diet2 = "塩分量を適正にする",
        Diet3 = "ゆっくり食べる",
        Diet4 = "間食を減らす",
        DietComment = "食事コメント",
        ExercisePrescription = "ウォーキング",
        ExerciseTime = "30分",
        ExerciseFrequency = "毎日",
        ExerciseIntensity = "息が弾む程度",
        DailyActivity = "5000歩",
        ExerciseComment = "運動コメント",
        Nonsmoker = true,
        SmokingCessation = false,
        Other1 = "その他1",
        Other2 = "その他2",
        Ophthalmology = true,
        Dental = false,
        CancerScreening = true,
    };

    /// <summary>FullPatientInfo と同じ基本情報を持つ pat.csv の患者</summary>
    public static PatientCsvRecord FullPatientCsvRecord(int patientId = 123) => new()
    {
        PatientId = patientId,
        Name = "山田太郎",
        Kana = "ヤマダタロウ",
        GenderCode = 1,
        Birthdate = new DateTime(1960, 3, 15),
        DoctorId = 10,
        DoctorName = "佐藤医師",
        DepartmentId = 3,
        Department = "内科",
    };

    /// <summary>全列に値を入れたテンプレート</summary>
    public static PlanTemplate FullPlanTemplate() => new()
    {
        Id = 1,
        MainDisease = "高血圧症",
        SheetName = "1_血圧130-80以下",
        TargetBp = "130/80",
        TargetHba1c = "7.0",
        Goal1 = "達成目標",
        Goal2 = "行動目標",
        Diet1 = "食事量を適正にする",
        Diet2 = "塩分量を適正にする",
        Diet3 = "ゆっくり食べる",
        Diet4 = "間食を減らす",
        ExercisePrescription = "ウォーキング",
        ExerciseTime = "30分",
        ExerciseFrequency = "毎日",
        ExerciseIntensity = "息が弾む程度",
        DailyActivity = "5000歩",
        Other1 = "その他1",
        Other2 = "その他2",
    };
}
