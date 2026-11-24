using Gentleman2D;

// The Gentleman 2D: ett smygspel där du bygger upp ett imperium i det tysta.
// Starta med: dotnet run

using var app = new GameApp();

// Autopiloten används bara för automatiska tester och skärmbilder:
//   GENTLEMAN_AUTOPILOT=/en/mapp dotnet run
string? autopilotDir = Environment.GetEnvironmentVariable("GENTLEMAN_AUTOPILOT");
if (!string.IsNullOrEmpty(autopilotDir))
    AutoPilot.Attach(app, autopilotDir);

app.Run();
