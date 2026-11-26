using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

public static partial class SaveFile
{
    private static IReadOnlyDictionary<string, int> ReadBestRates(string fileName, GameRules rules, int version, IReadOnlyDictionary<string, int>? bestRates)
    {
        if (bestRates is null)
        {
            return version >= FirstVersionWithBestRates
                ? throw new SaveException(fileName, "bestRates is missing")
                : new Dictionary<string, int>();
        }

        foreach (KeyValuePair<string, int> best in bestRates)
        {
            if (!rules.Crops.Any(crop => crop.Name == best.Key))
            {
                throw new SaveException(fileName, $"bestRates: unknown item {best.Key}");
            }

            if (best.Value < 1)
            {
                throw new SaveException(fileName, $"bestRates: {best.Key}: best rate must be at least 1, was {best.Value}");
            }
        }

        return bestRates;
    }

    private static ScriptBook ReadScripts(string fileName, GameRules rules, IReadOnlyList<ScriptDto> scripts, string selectedScript)
    {
        var savedScripts = new List<SavedScript>();
        foreach (ScriptDto script in scripts)
        {
            if (script.Python is null)
            {
                throw new SaveException(fileName, $"script {script.Name}: python is null");
            }

            if (script.JavaScript is null)
            {
                throw new SaveException(fileName, $"script {script.Name}: javaScript is null");
            }

            savedScripts.Add(new SavedScript(script.Name, script.Python, script.JavaScript));
        }

        try
        {
            return new ScriptBook(rules, savedScripts, selectedScript);
        }
        catch (ArgumentException exception)
        {
            throw new SaveException(fileName, exception.Message);
        }
    }

    private static ScriptBook ReadLegacyScripts(GameRules rules, LegacyScriptsDto scripts)
    {
        var script = new SavedScript(
            ScriptBook.FirstScriptName,
            scripts.Python ?? rules.StarterText(ScriptLanguage.Python),
            scripts.JavaScript ?? rules.StarterText(ScriptLanguage.JavaScript));
        return new ScriptBook(rules, [script], ScriptBook.FirstScriptName);
    }

    private static void CheckItems(string fileName, IReadOnlyDictionary<string, int> items, GameRules rules)
    {
        foreach (KeyValuePair<string, int> item in items)
        {
            if (!rules.Crops.Any(crop => crop.Name == item.Key))
            {
                throw new SaveException(fileName, $"unknown item {item.Key}");
            }

            if (item.Value < 1)
            {
                throw new SaveException(fileName, $"{item.Key}: count must be at least 1, was {item.Value}");
            }
        }
    }

    private static HashSet<string> CheckBoughtUnlocks(string fileName, IReadOnlyList<string> ids, GameRules rules)
    {
        var checkedIds = new HashSet<string>();
        foreach (string id in ids)
        {
            if (!rules.Unlocks.Any(unlock => unlock.Id == id))
            {
                throw new SaveException(fileName, $"unknown unlock id {id}");
            }

            if (!checkedIds.Add(id))
            {
                throw new SaveException(fileName, $"duplicate unlock id {id}");
            }
        }

        return checkedIds;
    }

    private static HashSet<string> CheckDoneGoals(string fileName, IReadOnlyList<string> ids, GameRules rules)
    {
        var checkedIds = new HashSet<string>();
        foreach (string id in ids)
        {
            if (!rules.Goals.Any(goal => goal.Id == id))
            {
                throw new SaveException(fileName, $"unknown goal id {id}");
            }

            if (!checkedIds.Add(id))
            {
                throw new SaveException(fileName, $"duplicate goal id {id}");
            }
        }

        return checkedIds;
    }

    private static (Tile[,] Tiles, TilePosition DronePosition) ReadField(string fileName, FieldDto field, GameRules rules, FieldSize size)
    {
        int tileCount = size.Width * size.Height;
        if (field.Tiles.Count != tileCount)
        {
            throw new SaveException(fileName, $"field has {field.Tiles.Count} tiles, expected {tileCount} for a {size.Width}x{size.Height} field");
        }

        var dronePosition = new TilePosition(field.Drone.X, field.Drone.Y);
        if (dronePosition.X < 0 || dronePosition.X >= size.Width || dronePosition.Y < 0 || dronePosition.Y >= size.Height)
        {
            throw new SaveException(fileName, $"drone ({dronePosition.X}, {dronePosition.Y}) is outside the {size.Width}x{size.Height} field");
        }

        var tiles = new Tile[size.Width, size.Height];
        for (int index = 0; index < tileCount; index++)
        {
            int x = index % size.Width;
            int y = index / size.Width;
            tiles[x, y] = ReadTile(fileName, field.Tiles[index], rules, x, y);
        }

        return (tiles, dronePosition);
    }

    private static Tile ReadTile(string fileName, TileDto tile, GameRules rules, int x, int y)
    {
        string where = $"tile ({x}, {y})";
        if (tile.Crop is null)
        {
            if (tile.AgeTicks != 0)
            {
                throw new SaveException(fileName, $"{where}: an empty tile has age 0, was {tile.AgeTicks}");
            }

            return new Tile(tile.Ground, null, 0);
        }

        Crop crop = rules.Crops.FirstOrDefault(candidate => candidate.Name == tile.Crop)
            ?? throw new SaveException(fileName, $"{where}: unknown crop {tile.Crop}");
        if (crop.Ground != tile.Ground)
        {
            throw new SaveException(fileName, $"{where}: {crop.Name} grows on {crop.Ground}, not {tile.Ground}");
        }

        if (tile.AgeTicks < 0 || tile.AgeTicks > crop.GrowTimeTicks)
        {
            throw new SaveException(fileName, $"{where}: age must be from 0 to {crop.GrowTimeTicks}, was {tile.AgeTicks}");
        }

        return new Tile(tile.Ground, crop, tile.AgeTicks);
    }
}
