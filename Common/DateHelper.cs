using System.Globalization;

namespace GarmentLotManager.Common;

public static class DateHelper
{
    public const string DisplayFormat = "dd/MM/yyyy";

    public static string ToDisplayDate(this DateTime? dt)
    {
        return dt.HasValue ? dt.Value.ToString(DisplayFormat) : string.Empty;
    }

    public static string ToDisplayDate(this DateTime dt)
    {
        return dt.ToString(DisplayFormat);
    }

    public static DateTime? ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt;
        }
        if (DateTime.TryParse(input.Trim(), new CultureInfo("en-GB"), DateTimeStyles.None, out dt))
        {
            return dt;
        }
        return null;
    }
}
