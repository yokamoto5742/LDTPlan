namespace LDTPlan;

/// <summary>UI表示文字列とドロップダウン選択肢の定数</summary>
public static class Constants
{
    public const string AppVersion = "1.1.2";
    public const string AppLastUpdated = "2026-10-12";

    // ウィンドウ・見出し
    public const string AppTitle = "生活習慣病療養計画書アプリ";
    public const string HistoryHeading = "計画書一覧";
    public const string HistoryHint = "計画書を左クリックすると編集画面が開きます";
    public const string SoapCloseNote = "(SOAP画面を閉じるとアプリは終了します)";
    public const string CreateViewTitle = "新規作成";
    public const string EditViewTitle = "編集";
    public const string TemplateViewTitle = "テンプレート";

    // ボタン
    public const string ButtonCreate = "新規作成";
    public const string ButtonCopy = "前回計画コピー";
    public const string ButtonTemplateEdit = "テンプレート編集";
    public const string ButtonClose = "閉じる";
    public const string ButtonSettings = "設定";
    public const string ButtonManual = "操作マニュアル";
    public const string ButtonCreateAndPrint = "新規登録して印刷";
    public const string ButtonSaveNew = "新規登録";
    public const string ButtonBack = "戻る";
    public const string ButtonSave = "保存";
    public const string ButtonPrint = "印刷";
    public const string ButtonDelete = "削除";
    public const string ButtonSaveTemplate = "テンプレート保存";
    public const string ButtonCsvExport = "CSV出力";
    public const string ButtonCsvImport = "CSV取込";

    // フィールドラベル
    public const string LabelPatientId = "患者ID";
    public const string LabelIssueDate = "発行日";
    public const string LabelName = "氏名";
    public const string LabelKana = "カナ";
    public const string LabelGender = "性別";
    public const string LabelBirthdate = "生年月日";
    public const string LabelDoctorId = "医師ID";
    public const string LabelDoctorName = "医師名";
    public const string LabelDepartmentId = "診療科ID";
    public const string LabelDepartment = "診療科";
    public const string LabelMainDiagnosis = "主病名";
    public const string LabelSheetName = "シート名";
    public const string LabelCreationCount = "作成回数";
    public const string LabelCreationCountSuffix = "回目";
    public const string LabelGoal1 = "①達成目標：患者と相談した目標";
    public const string LabelGoal2 = "②行動目標：患者と相談した目標";
    public const string LabelTargetAchievement = "目標達成状況(2回目以降)";
    public const string LabelTargetWeight = "目標体重";
    public const string LabelTargetBp = "目標血圧";
    public const string LabelTargetHba1c = "目標HbA1c";
    public const string LabelDiet1 = "食事1";
    public const string LabelDiet2 = "食事2";
    public const string LabelDiet3 = "食事3";
    public const string LabelDiet4 = "食事4";
    public const string LabelDietComment = "食事フリーコメント";
    public const string LabelExercisePrescription = "運動処方";
    public const string LabelExerciseTime = "時間";
    public const string LabelExerciseFrequency = "頻度";
    public const string LabelExerciseIntensity = "強度";
    public const string LabelDailyActivity = "日常生活の活動量";
    public const string LabelExerciseComment = "運動フリーコメント";
    public const string LabelNonsmoker = "非喫煙者である";
    public const string LabelSmokingCessation = "禁煙の実施方法等を指示";
    public const string LabelOther1 = "その他1";
    public const string LabelOther2 = "その他2";
    public const string LabelOphthalmology = "眼科";
    public const string LabelDental = "歯科";
    public const string LabelCancerScreening = "がん検診";

    // 履歴テーブル列
    public const string ColumnIssueDate = "発行日";
    public const string ColumnDepartment = "診療科";
    public const string ColumnDoctor = "医師";
    public const string ColumnMainDiagnosis = "主病名";
    public const string ColumnSheetName = "シート名";
    public const string ColumnCreationCount = "回数";

    // メッセージ
    public const string MsgWindowTooSmall = "ウィンドウサイズが小さすぎます。幅を1000ピクセル以上にしてください。";
    public const string MsgSelectMainDiagnosis = "主病名を選択してください";
    public const string MsgSelectSheetName = "シート名を選択してください";
    public const string MsgSelectMainDiagnosisAndSheet = "主病名とシート名を選択してください";
    public const string MsgDataSaved = "データが保存されました";
    public const string MsgDataCopied = "データがコピーされました";
    public const string MsgDataSavedAndPrinted = "データを保存して計画書を作成しました";
    public const string MsgTemplateSaved = "テンプレートが保存されました";
    public const string MsgNoDataSelected = "削除するデータが選択されていません";
    public const string MsgManualPathError = "操作マニュアルのパスを確認してください";
    public const string MsgManualOpenErrorFormat = "操作マニュアルを開けませんでした: {0}";
    public const string MsgPatientNotFoundFormat = "患者ID {0} が見つかりません。";
    public const string MsgCsvImported = "CSVファイルからデータがインポートされました";
    public const string MsgCsvImportInvalidFile = "インポートエラー:このファイルはインポートできません";
    public const string MsgCsvImportErrorFormat = "インポート中にエラーが発生しました: {0}";
    public const string MsgCsvExportedFormat = "データがCSVファイル '{0}' にエクスポートされました";
    public const string MsgCsvExportErrorFormat = "エクスポート中にエラーが発生しました: {0}";
    public const string MsgConfigNotFoundFormat = "設定ファイルが見つかりません: {0}";
    public const string MsgInvalidInputFormat = "入力値が不正です: {0}";

    public const string GenderMale = "男性";
    public const string GenderFemale = "女性";

    // ドロップダウン選択肢
    public static readonly string[] TargetAchievementItems =
        ["概ね達成", "概ね70%達成", "概ね50%達成", "未達成", "(空欄)"];

    public static readonly string[] DietItems =
    [
        "食事量を適正にする", "塩分量を適正にする", "水分摂取量を増やす", "食物繊維の摂取量を増やす",
        "ゆっくり食べる", "間食を減らす", "アルコールを控える", "脂肪の多い食品や甘い物を控える",
        "揚げ物や炒め物などを減らす", "1日3食を規則正しくとる", "今回は指導の必要なし", "(空欄)"
    ];

    public static readonly string[] ExercisePrescriptionItems =
        ["ウォーキング", "ストレッチ体操", "筋力トレーニング", "自転車", "畑仕事", "今回は指導の必要なし", "(空欄)"];

    public static readonly string[] ExerciseTimeItems = ["10分", "20分", "30分", "60分", "(空欄)"];

    public static readonly string[] ExerciseFrequencyItems = ["毎日", "週に5日", "週に3日", "週に2日", "(空欄)"];

    public static readonly string[] ExerciseIntensityItems =
        ["息が弾む程度", "ニコニコペース", "少し汗をかく程度", "息切れしない程度", "(空欄)"];

    public static readonly string[] DailyActivityItems =
        ["3000歩", "5000歩", "6000歩", "8000歩", "10000歩", "ストレッチ運動を主に行う", "(空欄)"];
}
