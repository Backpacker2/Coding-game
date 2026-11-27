using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class UnlockDataTests
{
    private static readonly string[] _expectedIds =
    [
        "FOR_LOOP",
        "BIGGER_FIELD",
        "RADISH",
        "CONDITIONS",
        "WHILE_LOOP",
        "POTATO",
        "FUNCTIONS",
        "VARIABLES",
        "LOGIC",
    ];

    [Fact]
    public void EveryFeatureOfEveryUnlockIsARealName()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        var drone = new DroneApi(rules, EvaluatorTestSupport.NewField(rules));
        HashSet<string> known = ["for", "while", "if", "def", "==", "variables", "+", "-", "*", "<", ">", "<=", ">=", "and"];
        known.UnionWith(drone.Commands.Keys);
        known.UnionWith(drone.Constants.Keys);

        foreach (Unlock unlock in rules.Unlocks)
        {
            foreach (string feature in unlock.Features)
            {
                Assert.Contains(feature, known);
            }
        }
    }

    [Fact]
    public void EveryUnlockPaysWithACropThatIsNotItsOwnFeature()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        IEnumerable<string> cropNames = rules.Crops.Select(crop => crop.Name);

        foreach (Unlock unlock in rules.Unlocks)
        {
            Assert.Contains(unlock.Price.Item, cropNames);
            Assert.DoesNotContain(unlock.Price.Item, unlock.Features);
        }
    }

    [Fact]
    public void UnlocksAreTheNineInOrder()
    {
        GameRules rules = EvaluatorTestSupport.Rules();

        Assert.Equal(_expectedIds, rules.Unlocks.Select(unlock => unlock.Id));
    }
}
