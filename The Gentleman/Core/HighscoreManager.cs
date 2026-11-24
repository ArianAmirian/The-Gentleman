using System.Text.Json;

namespace TheGentleman.Core;

public record HighscoreEntry(string Name, int Score, int LevelsCleared, DateTime Date);

/// <summary>Sparar topp 10 i en JSON-fil så att resultaten finns kvar mellan spelomgångar.</summary>
public class HighscoreManager
{
    public const int MaxEntries = 10;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string filePath;
    private readonly List<HighscoreEntry> entries;

    public HighscoreManager(string? filePath = null)
    {
        this.filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TheGentleman",
            "highscores.json");
        entries = Load();
    }

    public IReadOnlyList<HighscoreEntry> Entries => entries;

    /// <summary>Platsar poängen på topplistan?</summary>
    public bool Qualifies(int score) =>
        score > 0 && (entries.Count < MaxEntries || score > entries[^1].Score);

    public void Add(HighscoreEntry entry)
    {
        entries.Add(entry);
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        Save();
    }

    private List<HighscoreEntry> Load()
    {
        try
        {
            if (File.Exists(filePath))
                return JsonSerializer.Deserialize<List<HighscoreEntry>>(File.ReadAllText(filePath)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Trasig eller oläsbar fil: börja om med en tom lista i stället för att krascha.
        }
        return new();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, JsonSerializer.Serialize(entries, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Om det inte går att spara ska spelet ändå fortsätta.
        }
    }
}
