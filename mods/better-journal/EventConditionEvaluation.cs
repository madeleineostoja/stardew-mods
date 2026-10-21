using System.Reflection;

namespace BetterJournal;

internal sealed record ResolvedEventCondition(Delegate Handler, string[] Arguments, bool Negated);

internal static class EventConditionEvaluation
{
    // Only definitions are cached. Resolution and whole-header auditing must precede every evaluation.
    public static IReadOnlyList<ResolvedEventCondition>? Evaluate(
        string[][] conditions,
        IReadOnlySet<MethodInfo> allowedMethods,
        Func<string, Delegate?> resolve,
        Func<Delegate, string[], bool> invoke)
    {
        var resolved = new List<ResolvedEventCondition>(conditions.Length);
        foreach (var args in conditions)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                return null;

            bool negated = args[0].StartsWith('!');
            string name = negated ? args[0][1..] : args[0];
            var handler = resolve(name);
            if (handler is null || handler.Target is not null || !handler.Method.IsStatic
                || handler.GetInvocationList().Length != 1 || !allowedMethods.Contains(handler.Method))
                return null;

            resolved.Add(new ResolvedEventCondition(handler, args, negated));
        }

        foreach (var condition in resolved)
        {
            // An exception escapes to the per-candidate boundary, never through negation.
            if (invoke(condition.Handler, condition.Arguments) == condition.Negated)
                return null;
        }
        return resolved;
    }
}
