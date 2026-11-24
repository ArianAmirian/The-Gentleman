using Raylib_cs;
using Gentleman2D.UI;

namespace Gentleman2D.Core;

/// <summary>
/// Spelets klocka. En verklig sekund är två spelminuter, så ett dygn tar 12 minuter.
/// </summary>
public class GameClock
{
    public const float GameMinutesPerSecond = 2f;
    public const int MinutesPerDay = 24 * 60;
    public const int MorningMinute = 6 * 60;

    private int lastMorningDay = 1;

    public int Day { get; set; } = 1;
    public float Minutes { get; set; } = 8 * 60;

    /// <summary>Totala spelminuter sedan start, används för deadlines.</summary>
    public float Absolute => (Day - 1) * MinutesPerDay + Minutes;

    /// <summary>Hur många spelminuter som gick den senaste uppdateringen.</summary>
    public float DeltaMinutes { get; private set; }

    /// <summary>Körs varje morgon kl 06:00 (hyra, nya byten i butikerna...).</summary>
    public event Action? Morning;

    public int Hour => (int)(Minutes / 60);
    public string TimeText => $"{Hour:00}:{(int)Minutes % 60:00}";
    public bool IsNight => Minutes >= 21 * 60 || Minutes < 5 * 60;

    public void Update(float dt)
    {
        DeltaMinutes = dt * GameMinutesPerSecond;
        Minutes += DeltaMinutes;
        if (Minutes >= MinutesPerDay)
        {
            Minutes -= MinutesPerDay;
            Day++;
        }
        CheckMorning();
    }

    /// <summary>Hoppar fram till en viss tid nästa dag (när man sover eller blir gripen).</summary>
    public void SkipToNextDay(int hour)
    {
        if (Minutes >= MorningMinute) Day++;
        Minutes = hour * 60;
        CheckMorning();
    }

    public void SetState(int day, float minutes)
    {
        Day = day;
        Minutes = minutes;
        lastMorningDay = minutes >= MorningMinute ? day : day - 1;
    }

    private void CheckMorning()
    {
        if (Minutes >= MorningMinute && Day > lastMorningDay)
        {
            lastMorningDay = Day;
            Morning?.Invoke();
        }
    }

    /// <summary>0 = full dag, 1 = mitt i natten. Mjuka övergångar i skymning och gryning.</summary>
    public float Darkness
    {
        get
        {
            float h = Minutes / 60f;
            if (h >= 7 && h < 18) return 0;
            if (h >= 18 && h < 21.5f) return Smooth((h - 18) / 3.5f);
            if (h >= 4.5f && h < 7) return 1 - Smooth((h - 4.5f) / 2.5f);
            return 1;
        }
    }

    /// <summary>Färgen på omgivningsljuset: vitt på dagen, orange i skymningen och mörkblått på natten.</summary>
    public Color Ambient
    {
        get
        {
            var day = new Color(255, 252, 245, 255);
            var dusk = new Color(255, 170, 120, 255);
            var night = new Color(58, 70, 120, 255);
            float d = Darkness;
            bool evening = Minutes > 12 * 60;
            if (d < 0.5f)
                return Theme.Lerp(day, evening ? dusk : new Color(230, 190, 200, 255), d * 2);
            return Theme.Lerp(evening ? dusk : new Color(230, 190, 200, 255), night, (d - 0.5f) * 2);
        }
    }

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
