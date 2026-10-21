using System.Reflection;
using Xunit;

namespace BetterJournal.Tests;

public sealed class EventConditionEvaluationTests
{
    private static readonly Func<int, string[], bool> Check = HasRequiredValue;
    private static readonly Func<int, string[], bool> Throw = ThrowingCondition;
    private static readonly HashSet<MethodInfo> Allowed = new() { Check.Method, Throw.Method };

    [Fact]
    public void RejectedTailPreflightsTheWholeHeaderWithoutInvokingAnything()
    {
        Func<int, string[], bool> replacement = Replacement;
        Func<int, string[], bool> wrapper = (value, args) => Check(value, args);
        Func<int, string[], bool> instance = new InstanceCondition().Check;
        var handlers = new Dictionary<string, Delegate>
        {
            ["supported"] = Check,
            ["replaced"] = replacement,
            ["wrapped"] = wrapper,
            ["multicast"] = Check + Check,
            ["instance"] = instance
        };
        int calls = 0;
        foreach (string tail in new[] { "unknown", "!unknown", "replaced", "!replaced", "wrapped", "multicast", "instance", "!!supported" })
        {
            var result = EventConditionEvaluation.Evaluate(new[] { new[] { "supported", "1" }, new[] { tail } },
                Allowed, name => handlers.GetValueOrDefault(name), (handler, args) =>
                {
                    calls++;
                    return ((Func<int, string[], bool>)handler)(1, args);
                });
            Assert.Null(result);
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ReusedArgumentsUseFreshInputsAndResolutionAndDoNotNegateExceptions()
    {
        var arguments = new[] { new[] { "supported", "2" } };
        Delegate? current = Check;
        int value = 2;
        IReadOnlyList<ResolvedEventCondition>? Evaluate(string[][] conditions) =>
            EventConditionEvaluation.Evaluate(conditions, Allowed, _ => current,
                (handler, args) => ((Func<int, string[], bool>)handler)(value, args));

        Assert.NotNull(Evaluate(arguments));
        value = 1;
        Assert.Null(Evaluate(arguments));
        value = 2;
        Assert.NotNull(Evaluate(arguments));
        current = (Func<int, string[], bool>)Replacement;
        Assert.Null(Evaluate(arguments));
        current = Check;
        Assert.NotNull(Evaluate(arguments));
        Assert.Empty(Evaluate(Array.Empty<string[]>())!);

        // The original negated name remains at index zero, just as in the game's argument shape.
        var negated = new[] { new[] { "!supported", "2" } };
        Assert.Null(Evaluate(negated));
        value = 1;
        Assert.Equal("!supported", Assert.Single(Evaluate(negated)!).Arguments[0]);
        current = Throw;
        Assert.Throws<InvalidOperationException>(() => Evaluate(negated));
    }

    private static bool HasRequiredValue(int value, string[] args) => value >= int.Parse(args[1]);
    private static bool Replacement(int value, string[] args) => true;
    private static bool ThrowingCondition(int value, string[] args) => throw new InvalidOperationException();

    private sealed class InstanceCondition
    {
        public bool Check(int value, string[] args) => true;
    }
}
