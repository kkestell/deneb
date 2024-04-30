using System.Text;
using Wcwidth;

namespace Deneb.Utils;

public enum Alignment
{
    Left,
    Right
}

public class FixedWidthStringBuilder
{
    private readonly StringBuilder _stringBuilder = new();

    public void Append(string? value, int? width = null, Alignment align = Alignment.Left)
    {
        if (value is null)
            return;
        
        var finalWidth = width ?? StringWidth(value);

        if (finalWidth < 4 && StringWidth(value) > finalWidth)
        {
            value = value[..finalWidth];
        }
        else if (StringWidth(value) > finalWidth)
        {
            value = value[..(finalWidth - 3)] + "...";
        }

        var formattedValue = align switch
        {
            Alignment.Left => PadRight(value, finalWidth),
            Alignment.Right => PadLeft(value, finalWidth),
            _ => string.Empty
        };

        _stringBuilder.Append(formattedValue);
    }

    private static int StringWidth(string str)
    {
        return str.Sum(c => UnicodeCalculator.GetWidth(c));
    }
    
    private static string PadRight(string value, int width)
    {
        var padding = width - StringWidth(value);
        return value + new string(' ', padding);
    }
    
    private static string PadLeft(string value, int width)
    {
        var padding = width - StringWidth(value);
        return new string(' ', padding) + value;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}
