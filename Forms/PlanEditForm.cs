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

/// <summary>計画書の新規作成・編集・テンプレート編集画面</summary>
public class PlanEditForm : Form
{
    private readonly PlanFormMode _mode;
    // メイン画面で表示中の患者（pat.csv にない患者IDのときは null）
    private readonly PatientCsvRecord? _patient;
    private readonly int? _recordId;

    private readonly ComboBox _cmbMainDiagnosis = CreateCombo(PatientService.LoadMainDiseases());
    private readonly ComboBox _cmbSheetName = CreateCombo(PatientService.LoadSheetNames());
    private readonly TextBox _txtCreationCount = CreateTextBox(50);
    private readonly DateTimePicker _dtpIssueDate = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy/MM/dd",
        Width = 130,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 4, 3, 4),
    };

    private readonly TextBox _txtGoal1 = CreateTextBox(700);
    private readonly TextBox _txtGoal2 = CreateTextBox(700);
    private readonly ComboBox _cmbTargetAchievement = CreateCombo(Constants.TargetAchievementItems);
    private readonly TextBox _txtTargetWeight = CreateTextBox(80);
    private readonly TextBox _txtTargetBp = CreateTextBox(100);
    private readonly TextBox _txtTargetHba1c = CreateTextBox(80);
    private readonly ComboBox _cmbDiet1 = CreateCombo(Constants.DietItems);
    private readonly ComboBox _cmbDiet2 = CreateCombo(Constants.DietItems);
    private readonly ComboBox _cmbDiet3 = CreateCombo(Constants.DietItems);
    private readonly ComboBox _cmbDiet4 = CreateCombo(Constants.DietItems);
    private readonly TextBox _txtDietComment = CreateTextBox(700);
    private readonly ComboBox _cmbExercisePrescription = CreateCombo(Constants.ExercisePrescriptionItems);
    private readonly ComboBox _cmbExerciseTime = CreateCombo(Constants.ExerciseTimeItems);
    private readonly ComboBox _cmbExerciseFrequency = CreateCombo(Constants.ExerciseFrequencyItems);
    private readonly ComboBox _cmbExerciseIntensity = CreateCombo(Constants.ExerciseIntensityItems);
    private readonly ComboBox _cmbDailyActivity = CreateCombo(Constants.DailyActivityItems);
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
        PatientCsvRecord? patient,
        int? recordId = null)
    {
        _mode = mode;
        _patient = patient;
        _recordId = recordId;

        BuildUi();
        WireEvents();

        switch (_mode)
        {
            case PlanFormMode.Create:
                _txtCreationCount.Text = "1";
                _dtpIssueDate.Value = DateTime.Today;
                break;
            case PlanFormMode.Edit:
                LoadRecord();
                break;
        }
    }

    private void BuildUi()
    {
        Text = ModeTitle();
        StartPosition = FormStartPosition.CenterParent;
        AutoScroll = true;
        MinimizeBox = false;

        var headingFont = new Font(Font.FontFamily, AppConfig.HeadingFontSize, FontStyle.Bold);
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
            headerRow.Controls.Add(CreateLabel(Constants.LabelCreationCount));
            headerRow.Controls.Add(_txtCreationCount);
            headerRow.Controls.Add(CreateLabel(Constants.LabelCreationCountSuffix));
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
            CreateLabel(Constants.LabelDiet2), _cmbDiet2));
        layout.Controls.Add(CreateRow(
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
        // 下に余白が残らないよう、高さは中身に合わせる（モードによってボタンの行などの高さが変わるため）
        MinimumSize = new Size(0, 600);
        ClientSize = new Size(1250, layout.PreferredSize.Height);
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
        _txtCreationCount.Text = record.CreationCount?.ToString() ?? "";

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

        var patientInfo = new PatientInfo();
        try
        {
            ApplyBasicInfo(patientInfo);
            ApplyGuidanceFields(patientInfo);

            using var db = new AppDbContext();
            db.PatientInfos.Add(patientInfo);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            return;
        }

        // 保存後は計画書の生成に失敗しても画面を閉じる（再実行で同じ計画書が二重に登録されるのを防ぐ）
        if (print)
        {
            try
            {
                TreatmentPlanGenerator.GeneratePlan(patientInfo);
                ShowInfo(Constants.MsgDataSavedAndPrinted);
            }
            catch (Exception ex)
            {
                ShowError(string.Format(Constants.MsgSavedButPlanFailedFormat, ex.Message));
            }
        }
        else
        {
            ShowInfo(Constants.MsgDataSaved);
        }

        Close();
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

            ApplyBasicInfo(record);
            ApplyGuidanceFields(record);
            db.SaveChanges();
            ShowInfo(Constants.MsgDataSaved);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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

            ApplyGuidanceFields(record);
            db.SaveChanges();

            TreatmentPlanGenerator.GeneratePlan(record);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteData()
    {
        var answer = MessageBox.Show(this, Constants.MsgConfirmDelete, Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            using var db = new AppDbContext();
            var record = db.PatientInfos.Find(_recordId);
            if (record != null)
            {
                db.PatientInfos.Remove(record);
                db.SaveChanges();
            }
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
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

    /// <summary>メイン画面で表示中の患者の基本情報を反映（新規登録・編集保存で使用）</summary>
    private void ApplyBasicInfo(PatientInfo patientInfo)
    {
        var csv = _patient ?? throw new InvalidOperationException(Constants.MsgPatientNotInCsv);

        patientInfo.PatientId = csv.PatientId;
        patientInfo.PatientName = csv.Name;
        patientInfo.Kana = csv.Kana;
        patientInfo.Gender = csv.Gender;
        patientInfo.Birthdate = csv.Birthdate;
        patientInfo.DoctorId = csv.DoctorId;
        patientInfo.DoctorName = csv.DoctorName;
        patientInfo.DepartmentId = csv.DepartmentId;
        patientInfo.Department = csv.Department;
    }

    /// <summary>フォーム入力を反映（発行日時点の年齢は生年月日から計算するため、基本情報の後に呼ぶ）</summary>
    private void ApplyGuidanceFields(PatientInfo patientInfo)
    {
        patientInfo.MainDiagnosis = GetComboValue(_cmbMainDiagnosis);
        patientInfo.SheetName = GetComboValue(_cmbSheetName);
        patientInfo.CreationCount = ParseIntOrThrow(_txtCreationCount.Text, Constants.LabelCreationCount);
        patientInfo.IssueDate = _dtpIssueDate.Value.Date;
        patientInfo.IssueDateAge = patientInfo.Birthdate.HasValue
            ? DateUtils.CalculateIssueDateAge(patientInfo.Birthdate.Value, patientInfo.IssueDate.Value)
            : null;
        patientInfo.TargetWeight = _txtTargetWeight.Text == ""
            ? null
            : ParseDoubleOrThrow(_txtTargetWeight.Text, Constants.LabelTargetWeight);
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

    private static double ParseDoubleOrThrow(string value, string fieldLabel)
        => double.TryParse(value, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new InvalidOperationException(string.Format(Constants.MsgInvalidInputFormat, fieldLabel));

    private void ShowInfo(string message)
        => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowError(string message)
        => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
