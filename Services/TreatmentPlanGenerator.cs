using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ClosedXML.Excel;
using LDTPlan.Models;
using LDTPlan.Utils;
using ZXing;
using ZXing.Common;

namespace LDTPlan.Services;

/// <summary>Excelテンプレートから療養計画書を生成する</summary>
public static class TreatmentPlanGenerator
{
    private const string CommonSheetName = "共通情報";
    private const string FirstPlanSheetName = "初回用";
    private const string ContinuedPlanSheetName = "継続用";

    /// <summary>計画書を生成してExcelで開く</summary>
    public static void GeneratePlan(PatientInfo patientInfo, bool isFirstPlan)
    {
        var filePath = CreatePlanFile(patientInfo, isFirstPlan);
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    /// <summary>計画書ファイルを生成して保存先パスを返す</summary>
    public static string CreatePlanFile(PatientInfo patientInfo, bool isFirstPlan)
    {
        var documentCode = BuildDocumentCode(patientInfo);
        var filePath = Path.Combine(AppConfig.OutputPath, $"{documentCode}.xlsm");
        Directory.CreateDirectory(AppConfig.OutputPath);

        using var workbook = new XLWorkbook(AppConfig.TemplatePath);
        PopulateCommonSheet(workbook.Worksheet(CommonSheetName), patientInfo);

        using var barcodeImage = CreateBarcodePng(documentCode);
        AddBarcodeToSheet(workbook.Worksheet(FirstPlanSheetName), barcodeImage);
        AddBarcodeToSheet(workbook.Worksheet(ContinuedPlanSheetName), barcodeImage);

        ActivateTargetSheet(workbook, isFirstPlan);

        workbook.SaveAs(filePath);
        return filePath;
    }

    /// <summary>共通情報シートに患者情報を書き込む</summary>
    public static void PopulateCommonSheet(IXLWorksheet sheet, PatientInfo p)
    {
        SetCell(sheet, "B2", p.PatientId);
        SetCell(sheet, "B3", p.PatientName);
        SetCell(sheet, "B4", p.Kana);
        SetCell(sheet, "B5", p.Gender);
        SetCell(sheet, "B6", p.Birthdate);
        SetCell(sheet, "B7", p.IssueDate);
        SetCell(sheet, "B8", p.DoctorId);
        SetCell(sheet, "B9", p.DoctorName);
        SetCell(sheet, "B10", p.DepartmentId);
        SetCell(sheet, "B11", p.Department);
        SetCell(sheet, "B12", p.MainDiagnosis);
        // B13は旧作成回数欄のため空欄のままにする
        SetCell(sheet, "B14", p.TargetWeight);
        SetCell(sheet, "B15", p.SheetName);
        SetCell(sheet, "B16", p.TargetBp);
        SetCell(sheet, "B17", p.TargetHba1c);
        SetCell(sheet, "B18", p.Goal1);
        SetCell(sheet, "B19", p.Goal2);
        SetCell(sheet, "B20", p.TargetAchievement);
        SetCell(sheet, "B21", p.Diet1);
        SetCell(sheet, "B22", p.Diet2);
        SetCell(sheet, "B23", p.Diet3);
        SetCell(sheet, "B24", p.Diet4);
        SetCell(sheet, "B25", p.ExercisePrescription);
        SetCell(sheet, "B26", p.ExerciseTime);
        SetCell(sheet, "B27", p.ExerciseFrequency);
        SetCell(sheet, "B28", p.ExerciseIntensity);
        SetCell(sheet, "B29", p.DailyActivity);
        SetCell(sheet, "B30", p.Nonsmoker);
        SetCell(sheet, "B31", p.SmokingCessation);
        SetCell(sheet, "B32", p.Other1);
        SetCell(sheet, "B33", p.Other2);
        SetCell(sheet, "B34", p.Ophthalmology);
        SetCell(sheet, "B35", p.Dental);
        SetCell(sheet, "B36", p.CancerScreening);
        SetCell(sheet, "B37", p.IssueDateAge);
        SetCell(sheet, "B38", p.DietComment);
        SetCell(sheet, "B39", p.ExerciseComment);
    }

    private static void SetCell(IXLWorksheet sheet, string address, object? value)
    {
        var cell = sheet.Cell(address);
        switch (value)
        {
            case null:
                cell.Clear(XLClearOptions.Contents);
                break;
            case string s:
                cell.Value = s;
                break;
            case int i:
                cell.Value = i;
                break;
            case double d:
                cell.Value = d;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static string BuildDocumentCode(PatientInfo p)
    {
        var patientId = (p.PatientId ?? 0).ToString().PadLeft(9, '0');
        var departmentId = (p.DepartmentId ?? 0).ToString().PadLeft(3, '0');
        var doctorId = (p.DoctorId ?? 0).ToString().PadLeft(5, '0');
        var issueDate = (p.IssueDate ?? DateTime.Now).ToString("yyyyMMdd");
        var currentTime = DateTime.Now.ToString("HHmmss");
        return $"{patientId}{AppConfig.DocumentNumber}{departmentId}{doctorId}{issueDate}{currentTime}";
    }

    private static MemoryStream CreateBarcodePng(string data)
    {
        var imageWidth = AppConfig.GetInt("Barcode", "image_width", 200);
        var imageHeight = AppConfig.GetInt("Barcode", "image_height", 30);

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                // 貼り付けサイズより大きめに描画して縮小時の画質を確保する
                Width = imageWidth * 4,
                Height = imageHeight * 4,
                Margin = AppConfig.GetInt("Barcode", "quiet_zone", 1),
            },
        };

        var pixelData = writer.Write(data);
        using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
        try
        {
            Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    private static void AddBarcodeToSheet(IXLWorksheet sheet, MemoryStream barcodePng)
    {
        barcodePng.Position = 0;
        sheet.AddPicture(barcodePng)
            .MoveTo(sheet.Cell(AppConfig.Get("Barcode", "image_position", "B2")))
            .WithSize(
                AppConfig.GetInt("Barcode", "image_width", 200),
                AppConfig.GetInt("Barcode", "image_height", 30));
    }

    private static void ActivateTargetSheet(XLWorkbook workbook, bool isFirstPlan)
    {
        foreach (var sheet in workbook.Worksheets)
            sheet.TabSelected = false;

        var target = workbook.Worksheet(isFirstPlan ? FirstPlanSheetName : ContinuedPlanSheetName);
        target.TabSelected = true;
        target.TabActive = true;
    }
}
