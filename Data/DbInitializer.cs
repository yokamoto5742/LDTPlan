using LDTPlan.Models;
using LDTPlan.Utils;

namespace LDTPlan.Data;

public static class DbInitializer
{
    /// <summary>テーブル作成と初期データ投入</summary>
    public static void Initialize()
    {
        // 既存DBは作成・初期データ投入済みのため、起動を速くする目的でDBアクセスを省略する
        if (File.Exists(AppConfig.DbPath))
            return;

        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        SeedMainDiseases(db);
        SeedSheetNames(db);
        SeedTemplates(db);
    }

    private static void SeedMainDiseases(AppDbContext db)
    {
        if (db.MainDiseases.Any())
            return;

        db.MainDiseases.AddRange(
            new MainDisease { Id = 1, Name = "高血圧症" },
            new MainDisease { Id = 2, Name = "脂質異常症" },
            new MainDisease { Id = 3, Name = "糖尿病" });
        db.SaveChanges();
    }

    private static void SeedSheetNames(AppDbContext db)
    {
        if (db.SheetNames.Any())
            return;

        db.SheetNames.AddRange(
            new SheetName { MainDiseaseId = 1, Name = "1_血圧130-80以下" },
            new SheetName { MainDiseaseId = 1, Name = "2_血圧140-90以下" },
            new SheetName { MainDiseaseId = 1, Name = "3_血圧140-90以下_歩行" },
            new SheetName { MainDiseaseId = 2, Name = "1_LDL120以下" },
            new SheetName { MainDiseaseId = 2, Name = "2_LDL100以下" },
            new SheetName { MainDiseaseId = 2, Name = "3_LDL70以下" },
            new SheetName { MainDiseaseId = 3, Name = "1_HbA1c７％" },
            new SheetName { MainDiseaseId = 3, Name = "2_HbA1c６％" },
            new SheetName { MainDiseaseId = 3, Name = "3_HbA1c８％" });
        db.SaveChanges();
    }

    private static void SeedTemplates(AppDbContext db)
    {
        if (db.Templates.Any())
            return;

        db.Templates.AddRange(
            new PlanTemplate
            {
                MainDisease = "高血圧症", SheetName = "1_血圧130-80以下",
                TargetBp = "130/80", TargetHba1c = "",
                Goal1 = "家庭血圧が測定でき、朝と就寝前のいずれかで130/80mmHg以下",
                Goal2 = "塩分を控えた食事と運動習慣を目標にする",
                Diet1 = "塩分量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "睡眠の確保1日7時間", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "高血圧症", SheetName = "2_血圧140-90以下",
                TargetBp = "140/90", TargetHba1c = "",
                Goal1 = "家庭血圧が測定でき、朝と就寝前のいずれかで140/90mmHg以下",
                Goal2 = "塩分を控えた食事と運動習慣を目標にする",
                Diet1 = "塩分量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ストレッチ体操", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "ストレッチ運動を主に行う",
                Other1 = "睡眠の確保1日7時間", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "高血圧症", SheetName = "3_血圧140-90以下_歩行",
                TargetBp = "140/90", TargetHba1c = "",
                Goal1 = "家庭血圧が測定でき、朝と就寝前のいずれかで140/90mmHg以下",
                Goal2 = "塩分を控えた食事と運動習慣を目標にする",
                Diet1 = "塩分量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "6000歩",
                Other1 = "睡眠の確保1日7時間", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "脂質異常症", SheetName = "1_LDL120以下",
                TargetBp = "", TargetHba1c = "",
                Goal1 = "LDLコレステロール＜120/TG＜150/HDL≧40",
                Goal2 = "毎日の有酸素運動と食習慣の改善",
                Diet1 = "食事量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "飲酒の制限、肥満度の改善", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "脂質異常症", SheetName = "2_LDL100以下",
                TargetBp = "", TargetHba1c = "",
                Goal1 = "LDLコレステロール＜100/TG＜150/HDL≧40",
                Goal2 = "毎日の有酸素運動と食習慣の改善",
                Diet1 = "食事量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "飲酒の制限、肥満度の改善", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "脂質異常症", SheetName = "3_LDL70以下",
                TargetBp = "", TargetHba1c = "",
                Goal1 = "LDLコレステロール＜100/TG＜150/HDL>40",
                Goal2 = "毎日の有酸素運動と食習慣の改善",
                Diet1 = "脂肪の多い食品や甘い物を控える", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "飲酒の制限、肥満度の改善", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "糖尿病", SheetName = "1_HbA1c７％",
                TargetBp = "130/80", TargetHba1c = "7",
                Goal1 = "HbA1ｃ７％/体重を当初の－３Kgとする",
                Goal2 = "5000歩の歩行/間食の制限/糖質の制限",
                Diet1 = "食事量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に5日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "睡眠の確保1日7時間", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "糖尿病", SheetName = "2_HbA1c６％",
                TargetBp = "130/80", TargetHba1c = "6",
                Goal1 = "HbA1ｃを正常化/HbA1ｃ6％",
                Goal2 = "1日5000歩以上の歩行/間食の制限/糖質の制限",
                Diet1 = "食事量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ウォーキング", ExerciseTime = "30分",
                ExerciseFrequency = "週に5日", ExerciseIntensity = "少し汗をかく程度",
                DailyActivity = "5000歩",
                Other1 = "睡眠の確保1日7時間", Other2 = "毎日の歩数の測定"
            },
            new PlanTemplate
            {
                MainDisease = "糖尿病", SheetName = "3_HbA1c８％",
                TargetBp = "140/90", TargetHba1c = "8",
                Goal1 = "HbA1ｃを低血糖に注意して下げる",
                Goal2 = "ストレッチを中心とした運動/間食の制限/糖質の制限",
                Diet1 = "食事量を適正にする", Diet2 = "食物繊維の摂取量を増やす",
                Diet3 = "ゆっくり食べる", Diet4 = "間食を減らす",
                ExercisePrescription = "ストレッチ体操", ExerciseTime = "10分",
                ExerciseFrequency = "週に2日", ExerciseIntensity = "息切れしない程度",
                DailyActivity = "ストレッチ運動を主に行う",
                Other1 = "睡眠の確保1日7時間", Other2 = "家庭での血圧の測定"
            });
        db.SaveChanges();
    }
}
