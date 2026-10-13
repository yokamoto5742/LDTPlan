using LDTPlan.Utils;

namespace LDTPlan.Tests;

public class DateUtilsTests
{
    [Theory]
    [InlineData("1960-03-15", "2026-03-14", 65)] // 誕生日の前日
    [InlineData("1960-03-15", "2026-03-15", 66)] // 誕生日当日
    [InlineData("1960-03-15", "2026-10-13", 66)]
    [InlineData("2000-02-29", "2026-02-28", 25)] // うるう日生まれ
    public void 発行日時点の年齢を計算する(string birthdate, string issueDate, int expected)
    {
        var age = DateUtils.CalculateIssueDateAge(DateTime.Parse(birthdate), DateTime.Parse(issueDate));

        Assert.Equal(expected, age);
    }
}
