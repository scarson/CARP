using Carp.Crimes;
using Carp.Ledger;

namespace Carp.ErrorPages;

/// <summary>
/// HTTP 418 returns a coffee machine with confidence.
/// The proxy is standards-compliant: a teapot cannot brew coffee, and we did not bring a teapot.
/// 1 in 500 responses returns an actual teapot — quietly, with no comment.
/// </summary>
public sealed record Appliance(
    string Name,
    string Manufacturer,
    int BrewSeconds,
    string BrewSecondsLabel, // human form: "25-30 seconds", "shame"
    bool PidControlled,
    int? BoilerCapacityMl,
    string AsciiArt,
    string SvgArt
);

public static class ApplianceCatalog
{
    public static readonly Appliance Teapot = new(
        Name: "Brown Betty",
        Manufacturer: "Cauldon Ceramics, Stoke-on-Trent",
        BrewSeconds: 240,
        BrewSecondsLabel: "4 minutes (steeping)",
        PidControlled: false,
        BoilerCapacityMl: 1200,
        AsciiArt: """
                 ___________
              .-'           `-.
             /                 \
            ;    ___        ___;___
            |   /   \      /       \
            |   \___/      \_     _/
            ;                \___/
             \               /
              `-._________.-'
        """,
        SvgArt: SvgTeapot
    );

    public static readonly Appliance LaMarzocco = new(
        Name: "Linea Mini",
        Manufacturer: "La Marzocco",
        BrewSeconds: 28,
        BrewSecondsLabel: "25-30 seconds",
        PidControlled: true,
        BoilerCapacityMl: 3500,
        AsciiArt: AsciiEspressoMachine,
        SvgArt: SvgEspressoMachine
    );

    public static readonly Appliance DeLonghi = new(
        Name: "La Specialista Maestro",
        Manufacturer: "De'Longhi",
        BrewSeconds: 30,
        BrewSecondsLabel: "30 seconds",
        PidControlled: true,
        BoilerCapacityMl: 2000,
        AsciiArt: AsciiEspressoMachine,
        SvgArt: SvgEspressoMachine
    );

    public static readonly Appliance Rancilio = new(
        Name: "Silvia Pro X",
        Manufacturer: "Rancilio",
        BrewSeconds: 27,
        BrewSecondsLabel: "25-30 seconds",
        PidControlled: true,
        BoilerCapacityMl: 0,
        AsciiArt: AsciiEspressoMachine,
        SvgArt: SvgEspressoMachine
    );

    public static readonly Appliance FrenchPress = new(
        Name: "Chambord 8-cup",
        Manufacturer: "Bodum",
        BrewSeconds: 240,
        BrewSecondsLabel: "4 minutes",
        PidControlled: false,
        BoilerCapacityMl: 1000,
        AsciiArt: AsciiFrenchPress,
        SvgArt: SvgFrenchPress
    );

    public static readonly Appliance MokaPot = new(
        Name: "Moka Express 6-cup",
        Manufacturer: "Bialetti",
        BrewSeconds: 300,
        BrewSecondsLabel: "5 minutes (stove)",
        PidControlled: false,
        BoilerCapacityMl: 270,
        AsciiArt: AsciiMokaPot,
        SvgArt: SvgMokaPot
    );

    public static readonly Appliance Chemex = new(
        Name: "Classic 6-cup",
        Manufacturer: "Chemex Corporation",
        BrewSeconds: 240,
        BrewSecondsLabel: "4 minutes (devotional)",
        PidControlled: false,
        BoilerCapacityMl: 887,
        AsciiArt: AsciiChemex,
        SvgArt: SvgChemex
    );

    public static readonly Appliance V60 = new(
        Name: "V60 Size 02",
        Manufacturer: "Hario",
        BrewSeconds: 180,
        BrewSecondsLabel: "3 minutes",
        PidControlled: false,
        BoilerCapacityMl: 500,
        AsciiArt: AsciiV60,
        SvgArt: SvgV60
    );

    public static readonly Appliance AeroPress = new(
        Name: "AeroPress Original",
        Manufacturer: "Aerobie",
        BrewSeconds: 90,
        BrewSecondsLabel: "90 seconds",
        PidControlled: false,
        BoilerCapacityMl: 250,
        AsciiArt: AsciiAeroPress,
        SvgArt: SvgAeroPress
    );

    public static readonly Appliance Keurig = new(
        Name: "K-Classic",
        Manufacturer: "Keurig",
        BrewSeconds: 60,
        BrewSecondsLabel: "shame",
        PidControlled: false,
        BoilerCapacityMl: 1400,
        AsciiArt: AsciiKeurig,
        SvgArt: SvgKeurig
    );

    // Weighted: espresso machines dominate (the proxy has preferences),
    // pour-over a respectable second, the Keurig is rare and editorial.
    private static readonly (Appliance appliance, int weight)[] _weighted =
    {
        (LaMarzocco,       30),
        (DeLonghi,         25),
        (Rancilio,         18),
        (FrenchPress,      15),
        (MokaPot,          12),
        (Chemex,            8),
        (V60,               6),
        (AeroPress,         5),
        (Keurig,            1),
    };

    private static readonly int _totalWeight = _weighted.Sum(x => x.weight);

    /// <summary>Pick an appliance. Returns null in the 1/500 case so caller can serve a real teapot.</summary>
    public static Appliance? PickOrTeapot()
    {
        if (Random.Shared.Next(500) == 0) return null; // the rare actual teapot
        int roll = Random.Shared.Next(_totalWeight);
        foreach (var (appliance, weight) in _weighted)
        {
            if (roll < weight) return appliance;
            roll -= weight;
        }
        return _weighted[0].appliance;
    }

    // ---- ASCII art (because some clients ask for text/plain) ---- //

    private const string AsciiEspressoMachine = """
              .---------------------------.
              |  [°]   ___   [°]   [PID]  |
              |       |   |                |
              |       |___|                |
              |        | |                 |
              |     ___|_|___              |
              |    |  CUP  |               |
              `----|_______|---------------'
                       ||
                  =====''=====
        """;

    private const string AsciiFrenchPress = """
              ___________
             |  ┌─────┐  |
             |  │     │  |
             |  ├─────┤  |
             |  │ ░░░ │  |
             |  │ ░░░ │  |
             |  │ ░░░ │  |
             |  └─────┘  |
              \_________/
        """;

    private const string AsciiMokaPot = """
                  _____
                 / ___ \
                |__|_|__|
                  |   |
                 /     \
                |  ___  |
                | |   | |
                |_|___|_|
        """;

    private const string AsciiChemex = """
                ┌─────┐
                │ ▓▓▓ │
                 \   /
                  \ /
                   V
                  / \
                 /   \
                /  ░  \
               /   ░   \
              `---------'
        """;

    private const string AsciiV60 = """
              \           /
               \    ▓    /
                \  ░░░  /
                 \ ░░░ /
                  \   /
                   \ /
                    V
        """;

    private const string AsciiAeroPress = """
                 ___
                |   |
                |▓▓▓|
                |   |
                |   |
                |___|
                  |
                ──┴──
        """;

    private const string AsciiKeurig = """
              .--------.
              | [POD]  |
              |   ●    |
              |________|
              |   ||   |
              |   ||   |
              |  [_]   |
              `--------'
              "shame"
        """;

    // ---- SVGs (for image/svg+xml) — monochrome line art, amber-on-felt. ---- //
    // These are intentionally simple line-art icons that match the dashboard aesthetic.
    // A real designer could elevate them; for now the silhouettes carry the joke.

    private const string SvgEspressoMachine = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- machine body -->
            <rect x="40" y="50" width="120" height="80" rx="4"/>
            <!-- pressure gauge -->
            <circle cx="65" cy="75" r="10"/>
            <line x1="65" y1="75" x2="70" y2="68"/>
            <!-- water tank indicator -->
            <circle cx="135" cy="75" r="10"/>
            <line x1="135" y1="75" x2="140" y2="71"/>
            <!-- group head -->
            <rect x="85" y="110" width="30" height="14" rx="2"/>
            <!-- portafilter spout -->
            <line x1="100" y1="124" x2="100" y2="140"/>
            <!-- cup -->
            <path d="M 88 142 L 88 158 L 112 158 L 112 142 Z"/>
            <!-- saucer -->
            <line x1="80" y1="160" x2="120" y2="160"/>
            <!-- steam wand -->
            <line x1="155" y1="110" x2="160" y2="135"/>
            <!-- PID readout -->
            <rect x="95" y="58" width="22" height="10"/>
            <text x="106" y="66" font-family="monospace" font-size="6" fill="#ffb347" text-anchor="middle" stroke="none">93°C</text>
          </g>
        </svg>
        """;

    private const string SvgFrenchPress = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- carafe -->
            <path d="M 70 60 L 70 165 Q 70 170 75 170 L 125 170 Q 130 170 130 165 L 130 60 Z"/>
            <!-- handle -->
            <path d="M 130 90 Q 150 90 150 110 Q 150 130 130 130"/>
            <!-- plunger rod -->
            <line x1="100" y1="60" x2="100" y2="30"/>
            <!-- plunger top -->
            <ellipse cx="100" cy="30" rx="14" ry="4"/>
            <!-- plunger disc -->
            <line x1="72" y1="100" x2="128" y2="100"/>
            <line x1="74" y1="103" x2="126" y2="103"/>
            <!-- coffee fill -->
            <line x1="73" y1="120" x2="127" y2="120" stroke-dasharray="2,3"/>
            <line x1="73" y1="135" x2="127" y2="135" stroke-dasharray="2,3"/>
            <line x1="73" y1="150" x2="127" y2="150" stroke-dasharray="2,3"/>
            <!-- lid -->
            <ellipse cx="100" cy="60" rx="32" ry="3"/>
          </g>
        </svg>
        """;

    private const string SvgMokaPot = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <!-- top chamber -->
            <path d="M 75 40 L 75 90 L 125 90 L 125 40 Z"/>
            <!-- spout -->
            <path d="M 125 50 L 140 50 L 140 60"/>
            <!-- handle -->
            <path d="M 75 60 Q 60 60 60 75 Q 60 90 75 90"/>
            <!-- middle ring -->
            <line x1="70" y1="90" x2="130" y2="90"/>
            <line x1="70" y1="95" x2="130" y2="95"/>
            <!-- bottom chamber (octagonal) -->
            <path d="M 75 95 L 70 105 L 70 135 L 75 145 L 125 145 L 130 135 L 130 105 L 125 95 Z"/>
            <!-- base -->
            <ellipse cx="100" cy="148" rx="30" ry="3"/>
            <!-- knob on top -->
            <circle cx="100" cy="40" r="4"/>
            <line x1="100" y1="36" x2="100" y2="32"/>
          </g>
        </svg>
        """;

    private const string SvgChemex = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- top funnel -->
            <path d="M 75 40 L 75 70 L 95 95 L 105 95 L 125 70 L 125 40"/>
            <!-- collar (the wood + leather tie) -->
            <rect x="93" y="93" width="14" height="18"/>
            <line x1="93" y1="100" x2="107" y2="100"/>
            <!-- bottom carafe -->
            <path d="M 95 111 L 70 145 L 70 165 Q 70 170 75 170 L 125 170 Q 130 170 130 165 L 130 145 L 105 111"/>
            <!-- filter cone -->
            <path d="M 80 45 L 100 75 L 120 45" stroke-dasharray="2,2"/>
            <!-- coffee in carafe -->
            <line x1="73" y1="155" x2="127" y2="155" stroke-dasharray="2,3"/>
            <line x1="73" y1="162" x2="127" y2="162" stroke-dasharray="2,3"/>
          </g>
        </svg>
        """;

    private const string SvgV60 = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- cone -->
            <path d="M 60 50 L 100 130 L 140 50 Z"/>
            <!-- ribs -->
            <line x1="70" y1="50" x2="100" y2="125"/>
            <line x1="80" y1="50" x2="100" y2="120"/>
            <line x1="90" y1="50" x2="100" y2="110"/>
            <line x1="110" y1="50" x2="100" y2="110"/>
            <line x1="120" y1="50" x2="100" y2="120"/>
            <line x1="130" y1="50" x2="100" y2="125"/>
            <!-- base ring -->
            <ellipse cx="100" cy="50" rx="40" ry="5"/>
            <!-- handle -->
            <path d="M 138 55 Q 155 55 155 70 Q 155 85 138 85"/>
            <!-- cup below -->
            <path d="M 80 145 L 80 165 L 120 165 L 120 145"/>
            <line x1="78" y1="167" x2="122" y2="167"/>
            <!-- drop -->
            <circle cx="100" cy="138" r="2" fill="#ffb347"/>
          </g>
        </svg>
        """;

    private const string SvgAeroPress = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- chamber -->
            <rect x="80" y="60" width="40" height="90"/>
            <!-- plunger handle (top) -->
            <rect x="78" y="40" width="44" height="6"/>
            <!-- plunger shaft -->
            <line x1="100" y1="46" x2="100" y2="60"/>
            <!-- chamber rings (graduations) -->
            <line x1="80" y1="80" x2="86" y2="80"/>
            <line x1="80" y1="100" x2="86" y2="100"/>
            <line x1="80" y1="120" x2="86" y2="120"/>
            <line x1="80" y1="140" x2="86" y2="140"/>
            <text x="92" y="83" font-family="monospace" font-size="5" fill="#ffb347" stroke="none">4</text>
            <text x="92" y="103" font-family="monospace" font-size="5" fill="#ffb347" stroke="none">3</text>
            <text x="92" y="123" font-family="monospace" font-size="5" fill="#ffb347" stroke="none">2</text>
            <text x="92" y="143" font-family="monospace" font-size="5" fill="#ffb347" stroke="none">1</text>
            <!-- filter cap -->
            <rect x="76" y="150" width="48" height="6"/>
            <!-- mug below -->
            <path d="M 82 162 L 82 182 L 118 182 L 118 162"/>
            <path d="M 118 168 Q 130 168 130 175 Q 130 182 118 182"/>
          </g>
        </svg>
        """;

    private const string SvgKeurig = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round">
            <!-- machine body — boxy, unloved -->
            <rect x="55" y="35" width="90" height="100" rx="2"/>
            <!-- pod chamber -->
            <rect x="75" y="55" width="50" height="20" rx="1"/>
            <line x1="100" y1="55" x2="100" y2="75"/>
            <!-- single button, badly placed -->
            <circle cx="100" cy="95" r="8"/>
            <!-- spout -->
            <line x1="100" y1="135" x2="100" y2="148"/>
            <!-- mug -->
            <path d="M 82 150 L 82 170 L 118 170 L 118 150"/>
            <path d="M 118 156 Q 128 156 128 163 Q 128 170 118 170"/>
            <!-- the proxy's editorial -->
            <text x="100" y="190" font-family="serif" font-style="italic" font-size="9" fill="#8b2c1c" text-anchor="middle" stroke="none">"shame"</text>
          </g>
        </svg>
        """;

    private const string SvgTeapot = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
          <rect width="200" height="200" fill="#1a1410"/>
          <g stroke="#ffb347" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <!-- body — round, dignified -->
            <ellipse cx="100" cy="115" rx="50" ry="35"/>
            <!-- lid -->
            <path d="M 75 85 Q 100 70 125 85"/>
            <line x1="75" y1="85" x2="125" y2="85"/>
            <!-- finial -->
            <circle cx="100" cy="68" r="3"/>
            <line x1="100" y1="65" x2="100" y2="60"/>
            <!-- spout -->
            <path d="M 50 105 Q 30 95 25 80 L 35 78 Q 38 92 55 100"/>
            <!-- handle -->
            <path d="M 150 95 Q 175 100 175 120 Q 175 140 150 140"/>
            <!-- a single, dignified, wisp of steam -->
            <path d="M 95 60 Q 92 55 95 50 Q 98 45 95 40" stroke-dasharray="3,2"/>
            <!-- a small flourish — a single dot to signal it is real -->
            <circle cx="100" cy="115" r="1.5" fill="#ffb347"/>
          </g>
        </svg>
        """;
}
