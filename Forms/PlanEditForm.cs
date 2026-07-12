using System.Globalization;
using LDTPlan.Data;
using LDTPlan.Models;
using LDTPlan.Services;
using LDTPlan.Utils;
using static LDTPlan.Forms.UiFactory;

namespace LDTPlan.Forms;

public enum PlanFormMode
{
    Create,
    Edit,
    Template,
}

/// <summary>メイン画面で表示中の患者基本情報（編集保存・新規作成で使用）</summary>
public class PatientBasicInfo
{
    public string PatientIdText { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kana { get; init; } = "";
    public string Gender { get; init; } = "";
    public string BirthdateText { get; init; } = "";
    public string DoctorIdText { get; init; } = "";
    public string DoctorName { get; init; } = "";
    public string DepartmentIdText { get; init; } = "";
    public string Department { get; init; } = "";
}

/// <summary>計画書の新規作成・編集・テンプレート編集画面</summary>
public class PlanEditForm : Form
{
    private readonly PlanFormMode _mode;
    private readonly PatientBasicInfo _basicInfo;
    private readonly List<PatientCsvRecord> _csvPatients;
    private readonly int? _recordId;

    private readonly ComboBox _cmbMainDiagnosis = CreateCombo(200, PatientService.LoadMainDiseases());
    private readonly ComboBox _cmbSheetName = CreateCombo(300, PatientService.LoadSheetNames());
    private readonly DateTimePicker _dtpIssueDate = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy/MM/dd",
        Width = 130,
        Margin = new Padding(3, 6, 3, 3),
    };

    private readonly TextBox _txtGoal1 = CreateTextBox(700);
    private readonly TextBox _txtGoal2 = CreateTextBox(700);
    private readonly ComboBox _cmbTargetAchievement = CreateCombo(220, Constants.TargetAchievementItems);
    private readonly TextBox _txtTargetWeight = CreateTextBox(80);
    private readonly TextBox _txtTargetBp = CreateTextBox(100);
    private readonly TextBox _txtTargetHba1c = CreateTextBox(80);
    private readonly ComboBox _cmbDiet1 = CreateCombo(230, Constants.DietItems);
    private readonly ComboBox _cmbDiet2 = CreateCombo(230, Constants.DietItems);
    private readonly ComboBox _cmbDiet3 = CreateCombo(230, Constants.DietItems);
    private readonly ComboBox _cmbDiet4 = CreateCombo(230, Constants.DietItems);
    private readonly TextBox _txtDietComment = CreateTextBox(700);
    private readonly ComboBox _cmbExercisePrescription = CreateCombo(180, Constants.ExercisePrescriptionItems);
    private readonly ComboBox _cmbExerciseTime = CreateCombo(100, Constants.ExerciseTimeItems);
    private readonly ComboBox _cmbExerciseFrequency = CreateCombo(100, Constants.ExerciseFrequencyItems);
    private readonly ComboBox _cmbExerciseIntensity = CreateCombo(150, Constants.ExerciseIntensityItems);
    private readonly ComboBox _cmbDailyActivity = CreateCombo(190, Constants.DailyActivityItems);
    private readonly TextBox _txtExerciseComment = CreateTextBox(700);
    private readonly CheckBox _chkNonsmoker = CreateCheckBox(Constants.LabelNonsmoker);
    private readonly CheckBox _chkSmokingCessation = CreateCheckBox(Constants.LabelSmokingCessation);
    private readonly TextBox _txtOther1 = CreateTextBox(300);
    private readonly TextBox _txtOther2 = CreateTextBox(300);
    private readonly CheckBox _chkOphthalmology = CreateCheckBox(Constants.LabelOphthalmology);
    private readonly CheckBox _chkDental = CreateCheckBox(Constants.LabelDental);
    private readonly CheckBox _chkCancerScreening = CreateCheckBox(Constants.LabelCancerScreening);

    public PlanEditForm(
        PlanFormMode mode,
        PatientBasicInfo basicInfo,
        List<PatientCsvRecord> csvPatients,
        int? recordId = null)
    {
        _mode = mode;
        _basicInfo = basicInfo;
        _csvPatients = csvPatients;
        _recordId = recordId;

        BuildUi();
        WireEvents();

        switch (_mode)
        {
            case PlanFormMode.Create:
                _dtpIssueDate.Value = DateTime.Today;
                break;
            case PlanFormMode.Edit:
                LoadRecord();
                break;
            case PlanFormMode.Template:
                ApplyTemplate();
                break;
        }
    }

    private void BuildUi()
    {
        Text = ModeTitle();
        ClientSize = new Size(1250, 720);
        StartPosition = FormStartPosition.CenterParent;
        AutoScroll = true;
        MinimizeBox = false;

        var headingFont = new Font(Font.FontFamily, AppConfig.GetInt("UI", "heading_font_size", 16) * 0.75f, FontStyle.Bold);
        var titleLabel = new Label
        {
            Text = ModeTitle(),
            Font = headingFont,
            AutoSize = true,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(5),
            Margin = new Padding(3, 3, 10, 3),
        };

        var headerRow = CreateRow(
            titleLabel,
            CreateLabel(Constants.LabelMainDiagnosis), _cmbMainDiagnosis,
            CreateLabel(Constants.LabelSheetName), _cmbSheetName);
        if (_mode != PlanFormMode.Template)
        {
            headerRow.Controls.Add(CreateLabel(Constants.LabelIssueDate));
            headerRow.Controls.Add(_dtpIssueDate);
        }

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10),
        };
        layout.Controls.Add(headerRow);
        layout.Controls.Add(CreateRow(CreateLabel(Constants.LabelGoal1), _txtGoal1));
        layout.Controls.Add(CreateRow(CreateLabel(Constants.LabelGoal2), _txtGoal2));
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelTargetAchievement), _cmbTargetAchievement,
            CreateLabel(Constants.LabelTargetWeight), _txtTargetWeight, CreateLabel("kg"),
            CreateLabel(Constants.LabelTargetBp), _txtTargetBp, CreateLabel("mmHg"),
            CreateLabel(Constants.LabelTargetHba1c), _txtTargetHba1c, CreateLabel("%")));
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelDiet1), _cmbDiet1,
            CreateLabel(Constants.LabelDiet2), _cmbDiet2,
            CreateLabel(Constants.LabelDiet3), _cmbDiet3,
            CreateLabel(Constants.LabelDiet4), _cmbDiet4));
        layout.Controls.Add(CreateRow(CreateLabel(Constants.LabelDietComment), _txtDietComment));
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelExercisePrescription), _cmbExercisePrescription,
            CreateLabel(Constants.LabelExerciseTime), _cmbExerciseTime,
            CreateLabel(Constants.LabelExerciseFrequency), _cmbExerciseFrequency,
            CreateLabel(Constants.LabelExerciseIntensity), _cmbExerciseIntensity,
            CreateLabel(Constants.LabelDailyActivity), _cmbDailyActivity));
        layout.Controls.Add(CreateRow(CreateLabel(Constants.LabelExerciseComment), _txtExerciseComment));
        layout.Controls.Add(CreateRow(_chkNonsmoker, _chkSmokingCessation));
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelOther1), _txtOther1,
            CreateLabel(Constants.LabelOther2), _txtOther2));
        layout.Controls.Add(CreateRow(_chkOphthalmology, _chkDental, _chkCancerScreening));
        layout.Controls.Add(BuildButtonRow());

        Controls.Add(layout);
    }

    private string ModeTitle() => _mode switch
    {
        PlanFormMode.Create => Constants.CreateViewTitle,
        PlanFormMode.Edit => Constants.EditViewTitle,
        _ => Constants.TemplateViewTitle,
    };

    private FlowLayoutPanel BuildButtonRow() => _mode switch
    {
        PlanFormMode.Create => CreateRow(
            CreateButton(Constants.ButtonCreateAndPrint, (_, _) => SaveNewPlan(print: true)),
            CreateButton(Constants.ButtonSaveNew, (_, _) => SaveNewPlan(print: false)),
            CreateButton(Constants.ButtonBack, (_, _) => Close())),
        PlanFormMode.Edit => CreateRow(
            CreateButton(Constants.ButtonSave, (_, _) => SaveData()),
            CreateButton(Constants.ButtonPrint, (_, _) => PrintPlan()),
            CreateButton(Constants.ButtonDelete, (_, _) => DeleteData()),
            CreateButton(Constants.ButtonBack, (_, _) => Close())),
        _ => CreateRow(
            CreateButton(Constants.ButtonSaveTemplate, (_, _) => SaveTemplate()),
            CreateButton(Constants.ButtonBack, (_, _) => Close())),
    };

    private void WireEvents()
    {
        _cmbMainDiagnosis.SelectionChangeCommitted += (_, _) => OnMainDiagnosisChanged();
        _cmbSheetName.SelectionChangeCommitted += (_, _) => ApplyTemplate();

        _chkNonsmoker.CheckedChanged += (_, _) =>
        {
            if (_chkNonsmoker.Checked)
                _chkSmokingCessation.Checked = false;
        };
        _chkSmokingCessation.CheckedChanged += (_, _) =>
        {
            if (_chkSmokingCessation.Checked)
                _chkNonsmoker.Checked = false;
        };

        // 選択後に次の項目へフォーカスを移す
        SetFocusChain(_cmbTargetAchievement, _cmbDiet1);
        SetFocusChain(_cmbDiet1, _cmbDiet2);
        SetFocusChain(_cmbDiet2, _cmbDiet3);
        SetFocusChain(_cmbDiet3, _cmbDiet4);
        SetFocusChain(_cmbDiet4, _cmbExercisePrescription);
        SetFocusChain(_cmbExercisePrescription, _cmbExerciseTime);
        SetFocusChain(_cmbExerciseTime, _cmbExerciseFrequency);
        SetFocusChain(_cmbExerciseFrequency, _cmbExerciseIntensity);
        SetFocusChain(_cmbExerciseIntensity, _cmbDailyActivity);
    }

    private static void SetFocusChain(ComboBox source, ComboBox next)
        => source.SelectionChangeCommitted += (_, _) => next.Focus();

    private void OnMainDiagnosisChanged()
    {
        var mainDiseaseId = PatientService.FindMainDiseaseId(GetComboValue(_cmbMainDiagnosis));
        ReplaceComboItems(_cmbSheetName, PatientService.LoadSheetNames(mainDiseaseId));
    }

    /// <summary>主病名・シート名に対応するテンプレートをフィールドに適用</summary>
    private void ApplyTemplate()
    {
        var mainDisease = GetComboValue(_cmbMainDiagnosis);
        var sheetName = GetComboValue(_cmbSheetName);
        if (mainDisease == "" || sheetName == "")
            return;

        using var db = new AppDbContext();
        var template = db.Templates.FirstOrDefault(
            t => t.MainDisease == mainDisease && t.SheetName == sheetName);
        if (template == null)
            return;

        _txtTargetBp.Text = template.TargetBp;
        _txtTargetHba1c.Text = template.TargetHba1c;
        _txtGoal1.Text = template.Goal1;
        _txtGoal2.Text = template.Goal2;
        SetComboValue(_cmbDiet1, template.Diet1);
        SetComboValue(_cmbDiet2, template.Diet2);
        SetComboValue(_cmbDiet3, template.Diet3);
        SetComboValue(_cmbDiet4, template.Diet4);
        SetComboValue(_cmbExercisePrescription, template.ExercisePrescription);
        SetComboValue(_cmbExerciseTime, template.ExerciseTime);
        SetComboValue(_cmbExerciseFrequency, template.ExerciseFrequency);
        SetComboValue(_cmbExerciseIntensity, template.ExerciseIntensity);
        SetComboValue(_cmbDailyActivity, template.DailyActivity);
        _txtOther1.Text = template.Other1;
        _txtOther2.Text = template.Other2;
    }

    private void LoadRecord()
    {
        using var db = new AppDbContext();
        var record = db.PatientInfos.Find(_recordId);
        if (record == null)
            return;

        SetComboValue(_cmbMainDiagnosis, record.MainDiagnosis);
        var mainDiseaseId = PatientService.FindMainDiseaseId(record.MainDiagnosis);
        ReplaceComboItems(_cmbSheetName, PatientService.LoadSheetNames(mainDiseaseId));
        SetComboValue(_cmbSheetName, record.SheetName);

        if (record.IssueDate.HasValue)
            _dtpIssueDate.Value = record.IssueDate.Value;

        _txtTargetWeight.Text = record.TargetWeight?.ToString(CultureInfo.InvariantCulture) ?? "";
        _txtTargetBp.Text = record.TargetBp;
        _txtTargetHba1c.Text = record.TargetHba1c;
        _txtGoal1.Text = record.Goal1;
        _txtGoal2.Text = record.Goal2;
        SetComboValue(_cmbTargetAchievement, record.TargetAchievement);
        SetComboValue(_cmbDiet1, record.Diet1);
        SetComboValue(_cmbDiet2, record.Diet2);
        SetComboValue(_cmbDiet3, record.Diet3);
        SetComboValue(_cmbDiet4, record.Diet4);
        _txtDietComment.Text = record.DietComment;
        SetComboValue(_cmbExercisePrescription, record.ExercisePrescription);
        SetComboValue(_cmbExerciseTime, record.ExerciseTime);
        SetComboValue(_cmbExerciseFrequency, record.ExerciseFrequency);
        SetComboValue(_cmbExerciseIntensity, record.ExerciseIntensity);
        SetComboValue(_cmbDailyActivity, record.DailyActivity);
        _txtExerciseComment.Text = record.ExerciseComment;
        _chkNonsmoker.Checked = record.Nonsmoker ?? false;
        _chkSmokingCessation.Checked = record.SmokingCessation ?? false;
        _txtOther1.Text = record.Other1;
        _txtOther2.Text = record.Other2;
        _chkOphthalmology.Checked = record.Ophthalmology ?? false;
        _chkDental.Checked = record.Dental ?? false;
        _chkCancerScreening.Checked = record.CancerScreening ?? false;
    }

    private bool CheckRequiredFields()
    {
        if (GetComboValue(_cmbMainDiagnosis) == "")
        {
            ShowError(Constants.MsgSelectMainDiagnosis);
            return false;
        }
        if (GetComboValue(_cmbSheetName) == "")
        {
            ShowError(Constants.MsgSelectSheetName);
            return false;
        }
        return true;
    }

    /// <summary>新規登録（print指定時は計画書も生成）</summary>
    private void SaveNewPlan(bool print)
    {
        if (!CheckRequiredFields())
            return;

        try
        {
            var patientInfo = CreatePatientInfoFromCsv();

            using var db = new AppDbContext();
            db.PatientInfos.Add(patientInfo);
            db.SaveChanges();

            if (print)
            {
                var isFirstPlan = !db.PatientInfos.Any(
                    p => p.PatientId == patientInfo.PatientId && p.Id < patientInfo.Id);
                TreatmentPlanGenerator.GeneratePlan(patientInfo, isFirstPlan);
                ShowInfo(Constants.MsgDataSavedAndPrinted);
            }
            else
            {
                ShowInfo(Constants.MsgDataSaved);
            }

            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>編集内容を保存</summary>
    private void SaveData()
    {
        if (!CheckRequiredFields())
            return;

        try
        {
            using var db = new AppDbContext();
            var record = db.PatientInfos.Find(_recordId);
            if (record == null)
                return;

            ApplyFormToPatientInfo(record, includeBasicInfo: true);
            db.SaveChanges();
            ShowInfo(Constants.MsgDataSaved);
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Constants.MsgInvalidInputFormat, ex.Message));
        }
    }

    /// <summary>編集中の計画書を保存して印刷</summary>
    private void PrintPlan()
    {
        try
        {
            using var db = new AppDbContext();
            var record = db.PatientInfos.Find(_recordId);
            if (record == null)
                return;

            ApplyFormToPatientInfo(record, includeBasicInfo: false);
            db.SaveChanges();

            var isFirstPlan = !db.PatientInfos.Any(
                p => p.PatientId == record.PatientId && p.Id < record.Id);
            TreatmentPlanGenerator.GeneratePlan(record, isFirstPlan);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteData()
    {
        if (_recordId == null)
        {
            ShowError(Constants.MsgNoDataSelected);
            return;
        }

        using var db = new AppDbContext();
        var record = db.PatientInfos.Find(_recordId);
        if (record != null)
        {
            db.PatientInfos.Remove(record);
            db.SaveChanges();
        }
        Close();
    }

    private void SaveTemplate()
    {
        var mainDisease = GetComboValue(_cmbMainDiagnosis);
        var sheetName = GetComboValue(_cmbSheetName);
        if (mainDisease == "" || sheetName == "")
        {
            ShowError(Constants.MsgSelectMainDiagnosisAndSheet);
            return;
        }

        using var db = new AppDbContext();
        var template = db.Templates.FirstOrDefault(
            t => t.MainDisease == mainDisease && t.SheetName == sheetName);
        if (template == null)
        {
            template = new PlanTemplate { MainDisease = mainDisease, SheetName = sheetName };
            db.Templates.Add(template);
        }

        template.TargetBp = _txtTargetBp.Text;
        template.TargetHba1c = _txtTargetHba1c.Text;
        template.Goal1 = _txtGoal1.Text;
        template.Goal2 = _txtGoal2.Text;
        template.Diet1 = GetComboValue(_cmbDiet1);
        template.Diet2 = GetComboValue(_cmbDiet2);
        template.Diet3 = GetComboValue(_cmbDiet3);
        template.Diet4 = GetComboValue(_cmbDiet4);
        template.ExercisePrescription = GetComboValue(_cmbExercisePrescription);
        template.ExerciseTime = GetComboValue(_cmbExerciseTime);
        template.ExerciseFrequency = GetComboValue(_cmbExerciseFrequency);
        template.ExerciseIntensity = GetComboValue(_cmbExerciseIntensity);
        template.DailyActivity = GetComboValue(_cmbDailyActivity);
        template.Other1 = _txtOther1.Text;
        template.Other2 = _txtOther2.Text;
        db.SaveChanges();

        ShowInfo(Constants.MsgTemplateSaved);
    }

    /// <summary>患者CSVとフォーム入力から新規のPatientInfoを作成</summary>
    private PatientInfo CreatePatientInfoFromCsv()
    {
        if (!int.TryParse(_basicInfo.PatientIdText, out var patientId))
            throw new InvalidOperationException(
                string.Format(Constants.MsgPatientNotFoundFormat, _basicInfo.PatientIdText));

        var csvRecord = _csvPatients.FirstOrDefault(r => r.PatientId == patientId)
            ?? throw new InvalidOperationException(
                string.Format(Constants.MsgPatientNotFoundFormat, patientId));

        var patientInfo = new PatientInfo
        {
            PatientId = patientId,
            PatientName = csvRecord.Name,
            Kana = csvRecord.Kana,
            Gender = csvRecord.Gender,
            Birthdate = csvRecord.Birthdate,
            DoctorId = ParseIntOrThrow(_basicInfo.DoctorIdText, Constants.LabelDoctorId),
            DoctorName = _basicInfo.DoctorName,
            Department = _basicInfo.Department,
            DepartmentId = ParseIntOrThrow(_basicInfo.DepartmentIdText, Constants.LabelDepartmentId),
        };
        ApplyGuidanceFields(patientInfo);
        return patientInfo;
    }

    /// <summary>フォーム入力を既存レコードへ反映</summary>
    private void ApplyFormToPatientInfo(PatientInfo patientInfo, bool includeBasicInfo)
    {
        if (includeBasicInfo)
        {
            patientInfo.PatientId = ParseIntOrThrow(_basicInfo.PatientIdText, Constants.LabelPatientId);
            patientInfo.PatientName = _basicInfo.Name;
            patientInfo.Kana = _basicInfo.Kana;
            patientInfo.Gender = _basicInfo.Gender;
            patientInfo.Birthdate = DateTime.ParseExact(
                _basicInfo.BirthdateText, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            patientInfo.DoctorId = ParseIntOrThrow(_basicInfo.DoctorIdText, Constants.LabelDoctorId);
            patientInfo.DoctorName = _basicInfo.DoctorName;
            patientInfo.Department = _basicInfo.Department;
            patientInfo.DepartmentId = ParseIntOrThrow(_basicInfo.DepartmentIdText, Constants.LabelDepartmentId);
        }

        ApplyGuidanceFields(patientInfo);
    }

    private void ApplyGuidanceFields(PatientInfo patientInfo)
    {
        patientInfo.MainDiagnosis = GetComboValue(_cmbMainDiagnosis);
        patientInfo.SheetName = GetComboValue(_cmbSheetName);
        patientInfo.IssueDate = _dtpIssueDate.Value.Date;
        patientInfo.IssueDateAge = patientInfo.Birthdate.HasValue
            ? DateUtils.CalculateIssueDateAge(patientInfo.Birthdate.Value, patientInfo.IssueDate.Value)
            : null;
        patientInfo.TargetWeight = _txtTargetWeight.Text == ""
            ? null
            : double.Parse(_txtTargetWeight.Text, CultureInfo.InvariantCulture);
        patientInfo.TargetBp = _txtTargetBp.Text;
        patientInfo.TargetHba1c = _txtTargetHba1c.Text;
        patientInfo.Goal1 = _txtGoal1.Text;
        patientInfo.Goal2 = _txtGoal2.Text;
        patientInfo.TargetAchievement = GetComboValue(_cmbTargetAchievement);
        patientInfo.Diet1 = GetComboValue(_cmbDiet1);
        patientInfo.Diet2 = GetComboValue(_cmbDiet2);
        patientInfo.Diet3 = GetComboValue(_cmbDiet3);
        patientInfo.Diet4 = GetComboValue(_cmbDiet4);
        patientInfo.DietComment = _txtDietComment.Text;
        patientInfo.ExercisePrescription = GetComboValue(_cmbExercisePrescription);
        patientInfo.ExerciseTime = GetComboValue(_cmbExerciseTime);
        patientInfo.ExerciseFrequency = GetComboValue(_cmbExerciseFrequency);
        patientInfo.ExerciseIntensity = GetComboValue(_cmbExerciseIntensity);
        patientInfo.DailyActivity = GetComboValue(_cmbDailyActivity);
        patientInfo.ExerciseComment = _txtExerciseComment.Text;
        patientInfo.Nonsmoker = _chkNonsmoker.Checked;
        patientInfo.SmokingCessation = _chkSmokingCessation.Checked;
        patientInfo.Other1 = _txtOther1.Text;
        patientInfo.Other2 = _txtOther2.Text;
        patientInfo.Ophthalmology = _chkOphthalmology.Checked;
        patientInfo.Dental = _chkDental.Checked;
        patientInfo.CancerScreening = _chkCancerScreening.Checked;
    }

    private static int ParseIntOrThrow(string value, string fieldLabel)
        => int.TryParse(value, out var v)
            ? v
            : throw new InvalidOperationException(string.Format(Constants.MsgInvalidInputFormat, fieldLabel));

    private void ShowInfo(string message)
        => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowError(string message)
        => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
