# The Gentleman

Det här repot innehåller två versioner av spelet:

- **[The Gentleman 2D](The%20Gentleman%202D/README.md)** är den grafiska versionen med Raylib: en stad med dag och natt, kameror, polis, tillverkning, kunder och ett gömställe att bygga ut.
- **The Gentleman (terminal)**, nedan, är det ursprungliga turbaserade smygspelet i terminalen.

---

## Terminalversionen

Ett turbaserat smygspel i terminalen, skrivet i C# (.NET 10).

Du är The Gentleman (`@`), en mästertjuv. På fem banor ska du ta diamanten (`$`) och ta dig ut genom utgången (`E`) utan att vakterna ser dig.

## Starta spelet

```bash
cd "The Gentleman"
dotnet run
```

Spelet måste köras i en riktig terminal. Gör terminalfönstret minst 80 tecken brett.

## Så spelar du

| Tangent | Gör |
|---|---|
| Piltangenter / WASD | Gå |
| Mellanslag | Vänta en tur |
| R | Börja om banan |
| Esc | Avbryt kuppen |

- Spelet är **turbaserat**: vakterna rör sig bara när du gör ett drag.
- Vakter (`> < ^ v`) går fram och tillbaka och ser **6 rutor rakt fram**. Synfältet visas med röda prickar.
- Väggar (`█`) och stängda dörrar (`D`) skymmer sikten.
- `k` är en nyckel som öppnar en låst dörr. Varje nyckel används en gång.
- `*` är ett guldmynt (+100 poäng) och `$` är diamanten (+1000 poäng).

### Poäng

- Byte du plockat upp
- +500 för varje klarad bana
- +20 för varje drag under banans par
- +250 per liv kvar om du klarar alla banor

Du har 3 liv. De 10 bästa resultaten sparas i en topplista.

## Kodstruktur

Spelet är uppdelat i klasser enligt objektorienterade principer:

```
The Gentleman/
├── Program.cs              Startar menyn
├── Models/                 Sakerna i spelvärlden
│   ├── Position.cs         Position (record struct) och Direction
│   ├── Entity.cs           Abstrakt basklass för allt på kartan
│   ├── Player.cs           Spelaren: nycklar, diamant, byte
│   ├── Guard.cs            Vakt: patrullering och synfält
│   └── Items.cs            Item (abstrakt) → Coin, Key, Diamond
├── Core/                   Spelregler och spelflöde
│   ├── Level.cs            Karta, väggar, dörrar, inläsning från text
│   ├── LevelSession.cs     Reglerna för en tur, helt utan Console
│   ├── Game.cs             En spelomgång: banor, liv, poäng
│   └── HighscoreManager.cs Topplistan, sparas som JSON
├── UI/                     Allt som har med skärmen att göra
│   ├── Renderer.cs         Ritar banor och skärmar
│   └── Menu.cs             Huvudmenyn
└── Data/
    └── LevelLibrary.cs     Alla banor, ritade som text
```

OOP-principer som används:

- **Arv**: `Player`, `Guard` och alla föremål ärver från `Entity`.
- **Abstraktion**: `Entity` och `Item` är abstrakta klasser.
- **Polymorfism**: varje föremål bestämmer själv vad som händer i `PickUp()`.
- **Inkapsling**: till exempel `Guard.Facing` och `Game.Score` kan bara ändras inifrån sin egen klass.
- **Ansvarsfördelning**: reglerna (`LevelSession`) är skilda från utskriften (`Renderer`), så de kan testas utan terminal.

## Lägga till en bana

Rita en ny bana i `Data/LevelLibrary.cs`. Tecken: `#` vägg, `@` start, `$` diamant, `E` utgång, `D` dörr, `k` nyckel, `*` mynt och `> < ^ v` vakter. Banan måste ha exakt en `@`, exakt en `$` och minst en `E`.
