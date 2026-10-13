using System.Diagnostics;
using LDTPlan.Data;
using LDTPlan.Models;
using LDTPlan.Services;
using LDTPlan.Utils;
using static LDTPlan.Forms.UiFactory;

namespace LDTPlan.Forms;

public class MainForm : Form
{
    private readonly TextBox _txtPatientId = CreateTextBox(100);
    private readonly TextBox _txtName = CreateTextBox(150, readOnly: true);
    private readonly TextBox _txtKana = CreateTextBox(150, readOnly: true);
    private readonly TextBox _txtGender = CreateTextBox(80, readOnly: true);
    private readonly TextBox _txtBirthdate = CreateTextBox(120, readOnly: true);
    private readonly TextBox _txtDoctorId = CreateTextBox(80, readOnly: true);
    private readonly TextBox _txtDoctorName = CreateTextBox(150, readOnly: true);
    private readonly TextBox _txtDepartmentId = CreateTextBox(80, readOnly: true);
    private readonly TextBox _txtDepartment = CreateTextBox(150, readOnly: true);
    private readonly DataGridView _historyGrid = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 3000 };

    private List<PatientCsvRecord> _csvPatients = [];
    // 入力中の患者ID（数値でなければ null）と、それに対応する pat.csv の患者（なければ null）
    private int? _patientId;
    private PatientCsvRecord? _currentPatient;
    private FileMonitorService? _fileMonitor;

    public MainForm()
    {
        BuildUi();

        // 画面を先に表示してから患者情報と履歴を読み込む（DBアクセスで表示を待たせない）
        Shown += (_, _) =>
        {
            Update();
            StartupLog.Mark("画面表示");
            Initialize();
            StartupLog.Mark("履歴表示完了");
            StartupLog.Flush();
        };
        Resize += (_, _) => WarnIfWindowTooSmall();
        FormClosed += (_, _) => _fileMonitor?.Dispose();
        _statusTimer.Tick += (_, _) =>
        {
            _statusLabel.Text = "";
            _statusTimer.Stop();
        };
    }

    private void BuildUi()
    {
        Text = Constants.AppTitle;
        ClientSize = new Size(
            AppConfig.GetInt("settings", "window_width", 1200),
            AppConfig.GetInt("settings", "window_height", 900));
        StartPosition = FormStartPosition.CenterScreen;
        AutoScroll = true;

        var tableWidth = AppConfig.GetInt("DataTable", "width", 1200);

        ConfigureHistoryGrid(tableWidth);

        var headingFont = new Font(Font.FontFamily, AppConfig.GetInt("UI", "heading_font_size", 16) * 0.75f);
        var historyHeading = CreateLabel(Constants.HistoryHeading);
        historyHeading.Font = headingFont;

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10),
        };
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelPatientId), _txtPatientId,
            CreateLabel(Constants.LabelName), _txtName,
            CreateLabel(Constants.LabelKana), _txtKana,
            CreateLabel(Constants.LabelGender), _txtGender,
            CreateLabel(Constants.LabelBirthdate), _txtBirthdate));
        layout.Controls.Add(CreateRow(
            CreateLabel(Constants.LabelDoctorId), _txtDoctorId,
            CreateLabel(Constants.LabelDoctorName), _txtDoctorName,
            CreateLabel(Constants.LabelDepartmentId), _txtDepartmentId,
            CreateLabel(Constants.LabelDepartment), _txtDepartment,
            CreateButton(Constants.ButtonSettings, OpenSettings),
            CreateButton(Constants.ButtonManual, OpenManualPdf)));
        layout.Controls.Add(CreateRow(
            CreateButton(Constants.ButtonCreate, (_, _) => OpenPlanForm(PlanFormMode.Create)),
            CreateButton(Constants.ButtonCopy, CopyData),
            CreateButton(Constants.ButtonTemplateEdit, (_, _) => OpenPlanForm(PlanFormMode.Template)),
            CreateButton(Constants.ButtonClose, (_, _) => Close()),
            CreateLabel(Constants.SoapCloseNote)));
        layout.Controls.Add(CreateRow(historyHeading, CreateLabel(Constants.HistoryHint)));
        layout.Controls.Add(_historyGrid);

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_statusLabel);

        Controls.Add(layout);
        Controls.Add(statusStrip);

        _txtPatientId.TextChanged += OnPatientIdChanged;
    }

    private void ConfigureHistoryGrid(int tableWidth)
    {
        _historyGrid.Width = tableWidth;
        _historyGrid.Height = 400;
        _historyGrid.ReadOnly = true;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.MultiSelect = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _historyGrid.Columns.Add("issue_date", Constants.ColumnIssueDate);
        _historyGrid.Columns.Add("department", Constants.ColumnDepartment);
        _historyGrid.Columns.Add("doctor_name", Constants.ColumnDoctor);
        _historyGrid.Columns.Add("main_diagnosis", Constants.ColumnMainDiagnosis);
        _historyGrid.Columns.Add("sheet_name", Constants.ColumnSheetName);
        _historyGrid.Columns.Add("creation_count", Constants.ColumnCreationCount);
        _historyGrid.CellClick += OnHistoryRowClicked;
    }

    private void Initialize()
    {
        try
        {
            _csvPatients = PatientCsvService.LoadPatientData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Constants.MsgPatientCsvLoadErrorFormat, ex.Message),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _fileMonitor = new FileMonitorService(this);

        if (_csvPatients.Count > 0)
            _txtPatientId.Text = _csvPatients[0].PatientId.ToString();
        else
            RefreshHistory();
    }

    private void OnPatientIdChanged(object? sender, EventArgs e)
    {
        _patientId = int.TryParse(_txtPatientId.Text.Trim(), out var id) ? id : null;
        _currentPatient = _csvPatients.FirstOrDefault(r => r.PatientId == _patientId);
        ShowPatientInfo(_currentPatient);
        RefreshHistory();
    }

    /// <summary>患者基本情報を表示する（pat.csv にない患者は全欄を空にする）</summary>
    private void ShowPatientInfo(PatientCsvRecord? record)
    {
        _txtName.Text = record?.Name ?? "";
        _txtKana.Text = record?.Kana ?? "";
        _txtGender.Text = record?.Gender ?? "";
        _txtBirthdate.Text = record?.Birthdate?.ToString("yyyy/MM/dd") ?? "";
        _txtDoctorId.Text = record?.DoctorId.ToString() ?? "";
        _txtDoctorName.Text = record?.DoctorName ?? "";
        _txtDepartmentId.Text = record?.DepartmentId.ToString() ?? "";
        _txtDepartment.Text = record?.Department ?? "";
    }

    private void RefreshHistory()
    {
        var rows = PatientService.FetchPatientHistory(_patientId);

        _historyGrid.Rows.Clear();
        foreach (var row in rows)
        {
            var index = _historyGrid.Rows.Add(
                row.IssueDate, row.Department, row.DoctorName, row.MainDiagnosis, row.SheetName,
                row.CreationCount?.ToString() ?? "");
            _historyGrid.Rows[index].Tag = row.Id;
        }
        _historyGrid.ClearSelection();
    }

    private void OnHistoryRowClicked(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;
        if (_historyGrid.Rows[e.RowIndex].Tag is int recordId)
            OpenPlanForm(PlanFormMode.Edit, recordId);
    }

    private void OpenPlanForm(PlanFormMode mode, int? recordId = null)
    {
        using var form = new PlanEditForm(mode, _currentPatient, recordId);
        form.ShowDialog(this);
        RefreshHistory();
    }

    /// <summary>前回計画コピー: 最新の計画書を発行日と医師情報を更新して複製する</summary>
    private void CopyData(object? sender, EventArgs e)
    {
        var csvRecord = _currentPatient;
        if (csvRecord == null)
            return;

        using var db = new AppDbContext();
        var source = db.PatientInfos
            .Where(p => p.PatientId == csvRecord.PatientId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();
        if (source == null)
            return;

        var today = DateTime.Today;
        var copy = new PatientInfo
        {
            PatientId = source.PatientId,
            PatientName = source.PatientName,
            Kana = source.Kana,
            Gender = source.Gender,
            Birthdate = source.Birthdate,
            IssueDate = today,
            IssueDateAge = source.Birthdate.HasValue
                ? DateUtils.CalculateIssueDateAge(source.Birthdate.Value, today)
                : null,
            DoctorId = csvRecord.DoctorId,
            DoctorName = csvRecord.DoctorName,
            Department = csvRecord.Department,
            DepartmentId = csvRecord.DepartmentId,
            MainDiagnosis = source.MainDiagnosis,
            SheetName = source.SheetName,
            CreationCount = source.CreationCount + 1,
            TargetWeight = source.TargetWeight,
            TargetBp = source.TargetBp,
            TargetHba1c = source.TargetHba1c,
            Goal1 = source.Goal1,
            Goal2 = source.Goal2,
            TargetAchievement = source.TargetAchievement,
            Diet1 = source.Diet1,
            Diet2 = source.Diet2,
            Diet3 = source.Diet3,
            Diet4 = source.Diet4,
            DietComment = source.DietComment,
            ExercisePrescription = source.ExercisePrescription,
            ExerciseTime = source.ExerciseTime,
            ExerciseFrequency = source.ExerciseFrequency,
            ExerciseIntensity = source.ExerciseIntensity,
            DailyActivity = source.DailyActivity,
            ExerciseComment = source.ExerciseComment,
            Nonsmoker = source.Nonsmoker,
            SmokingCessation = source.SmokingCessation,
            Other1 = source.Other1,
            Other2 = source.Other2,
            Ophthalmology = source.Ophthalmology,
            Dental = source.Dental,
            CancerScreening = source.CancerScreening,
        };
        db.PatientInfos.Add(copy);
        db.SaveChanges();

        ShowStatus(Constants.MsgDataCopied);
        RefreshHistory();
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        using var dialog = new SettingsDialog(RefreshHistory);
        dialog.ShowDialog(this);
    }

    private void OpenManualPdf(object? sender, EventArgs e)
    {
        var path = AppConfig.ManualPdfPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            ShowStatus(Constants.MsgManualPathError);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowStatus(string.Format(Constants.MsgManualOpenErrorFormat, ex.Message));
        }
    }

    private void WarnIfWindowTooSmall()
    {
        if (Width < 1000)
            ShowStatus(Constants.MsgWindowTooSmall);
    }

    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
        _statusTimer.Stop();
        _statusTimer.Start();
    }
}
