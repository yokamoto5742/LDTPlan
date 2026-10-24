using System.Diagnostics;
using System.Runtime.InteropServices;
using LDTPlan.Data;
using LDTPlan.Services;
using LDTPlan.Utils;
using Microsoft.Data.Sqlite;
using static LDTPlan.Forms.UiFactory;

namespace LDTPlan.Forms;

public class MainForm : Form
{
    private readonly TextBox _txtPatientId = CreateTextBox(80);
    private readonly TextBox _txtName = CreateTextBox(120, readOnly: true);
    private readonly TextBox _txtKana = CreateTextBox(130, readOnly: true);
    private readonly TextBox _txtGender = CreateTextBox(50, readOnly: true);
    private readonly TextBox _txtBirthdate = CreateTextBox(100, readOnly: true);
    private readonly TextBox _txtDoctorId = CreateTextBox(80, readOnly: true);
    private readonly TextBox _txtDoctorName = CreateTextBox(150, readOnly: true);
    private readonly TextBox _txtDepartment = CreateTextBox(150, readOnly: true);
    private readonly DataGridView _historyGrid = new();

    private List<PatientCsvRecord> _csvPatients = [];
    // 入力中の患者ID（数値でなければ null）と、それに対応する pat.csv の患者（なければ null）
    private int? _patientId;
    private PatientCsvRecord? _currentPatient;
    private FileMonitorService? _fileMonitor;

    public MainForm()
    {
        BuildUi();

        // 患者情報と履歴を読み込んでから画面を表示する（読込中に入力されないよう、表示前に読み込む）
        Load += (_, _) =>
        {
            StartupLog.Mark("画面読込開始");
            Initialize();
            StartupLog.Mark("履歴表示完了");
        };
        Shown += (_, _) =>
        {
            // 読込中に打たれたキーは溜まっていて表示後に患者ID欄へ届き、文字が重複するため捨てる
            DiscardPendingKeyInput();
            StartupLog.Mark("画面表示");
            StartupLog.Flush();

            // 既存DBへのインデックス追加は、作成中に書き込みを待たせるため履歴表示の後に裏で行う。
            // 失敗しても次回起動時に再試行されるので無視する
            Task.Run(() =>
            {
                try
                {
                    DbInitializer.EnsurePatientIdIndex();
                }
                catch (SqliteException)
                {
                }
            });
        };
        FormClosed += (_, _) => _fileMonitor?.Dispose();
    }

    private void BuildUi()
    {
        Text = Constants.AppTitle;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(AppConfig.WindowWidth, AppConfig.WindowHeight);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScroll = true;

        var patientRow = CreateRow(
            CreateLabel(Constants.LabelPatientId), _txtPatientId,
            CreateLabel(Constants.LabelName), _txtName,
            CreateLabel(Constants.LabelKana), _txtKana,
            CreateLabel(Constants.LabelGender), _txtGender,
            CreateLabel(Constants.LabelBirthdate), _txtBirthdate);
        var doctorRow = CreateRow(
            CreateLabel(Constants.LabelDoctorId), _txtDoctorId,
            CreateLabel(Constants.LabelDoctorName), _txtDoctorName,
            CreateLabel(Constants.LabelDepartment), _txtDepartment,
            CreateButton(Constants.ButtonSettings, OpenSettings),
            CreateButton(Constants.ButtonManual, OpenManualPdf));

        // 一覧の右端を、上の入力行（広いほう）の右端にそろえる
        var rowWidth = Math.Max(patientRow.PreferredSize.Width, doctorRow.PreferredSize.Width);
        ConfigureHistoryGrid(rowWidth - _historyGrid.Margin.Horizontal);

        var headingFont = new Font(Font.FontFamily, HeadingFontSize);
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
        layout.Controls.Add(patientRow);
        layout.Controls.Add(doctorRow);
        layout.Controls.Add(CreateRow(
            CreateButton(Constants.ButtonCreate, (_, _) => OpenPlanForm(PlanFormMode.Create)),
            CreateButton(Constants.ButtonCopy, CopyData),
            CreateButton(Constants.ButtonTemplateEdit, (_, _) => OpenPlanForm(PlanFormMode.Template)),
            CreateButton(Constants.ButtonClose, (_, _) => Close()),
            CreateLabel(Constants.SoapCloseNote)));
        layout.Controls.Add(CreateRow(historyHeading, CreateLabel(Constants.HistoryHint)));
        layout.Controls.Add(_historyGrid);

        Controls.Add(layout);

        _txtPatientId.TextChanged += OnPatientIdChanged;
    }

    private void ConfigureHistoryGrid(int tableWidth)
    {
        _historyGrid.Width = tableWidth;
        _historyGrid.Height = ScaleToFont(400);
        _historyGrid.ReadOnly = true;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.MultiSelect = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        // 既定の固定高さでは端末のフォントによって見出しの文字が切れるため、文字に合わせて高さを決める
        _historyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _historyGrid.Columns.Add("issue_date", Constants.ColumnIssueDate);
        _historyGrid.Columns.Add("department", Constants.ColumnDepartment);
        _historyGrid.Columns.Add("doctor_name", Constants.ColumnDoctor);
        _historyGrid.Columns.Add("main_diagnosis", Constants.ColumnMainDiagnosis);
        _historyGrid.Columns.Add("sheet_name", Constants.ColumnSheetName);
        _historyGrid.Columns.Add("creation_count", Constants.ColumnCreationCount);

        // 長さが決まっている発行日・回数は値の幅に固定し、残りを 2:2:2:3 で分ける（シート名が一番長い）
        _historyGrid.Columns["issue_date"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _historyGrid.Columns["creation_count"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _historyGrid.Columns["creation_count"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _historyGrid.Columns["department"]!.FillWeight = 2;
        _historyGrid.Columns["doctor_name"]!.FillWeight = 2;
        _historyGrid.Columns["main_diagnosis"]!.FillWeight = 2;
        _historyGrid.Columns["sheet_name"]!.FillWeight = 3;
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
            MessageDialog.Show(this, string.Format(Constants.MsgPatientCsvLoadErrorFormat, ex.Message),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        StartupLog.Mark("患者CSV読込完了");

        _fileMonitor = new FileMonitorService(this);
        StartupLog.Mark("監視開始完了");

        if (_csvPatients.Count > 0)
            _txtPatientId.Text = _csvPatients[0].PatientId.ToString();
        else
            RefreshHistory();
    }

    private const uint WmKeyFirst = 0x0100;
    private const uint WmKeyLast = 0x0109;
    private const uint PmRemove = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr HWnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Pt;
        public uint Private;
    }

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage msg, IntPtr hWnd, uint filterMin, uint filterMax, uint remove);

    /// <summary>メッセージキューに溜まっているキー入力をすべて捨てる</summary>
    private static void DiscardPendingKeyInput()
    {
        while (PeekMessage(out _, IntPtr.Zero, WmKeyFirst, WmKeyLast, PmRemove))
        {
        }
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
        _txtDepartment.Text = record?.Department ?? "";
    }

    private void RefreshHistory()
    {
        var rows = PatientService.FetchPatientHistory(_patientId);
        StartupLog.Mark("履歴検索完了");

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
        if (_currentPatient == null || !PatientService.CopyLatestPlan(_currentPatient, DateTime.Today))
            return;

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
            MessageDialog.Show(this, Constants.MsgManualPathError, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageDialog.Show(this, string.Format(Constants.MsgManualOpenErrorFormat, ex.Message),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
