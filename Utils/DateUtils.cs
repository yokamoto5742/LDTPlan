namespace LDTPlan.Utils;

public static class DateUtils
{
    /// <summary>発行日時点の年齢を計算</summary>
    public static int CalculateIssueDateAge(DateTime birthDate, DateTime issueDate)
    {
        var age = issueDate.Year - birthDate.Year;
        if (issueDate.Month < birthDate.Month ||
            (issueDate.Month == birthDate.Month && issueDate.Day < birthDate.Day))
            age--;
        return age;
    }
}
