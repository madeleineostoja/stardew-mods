namespace ConfigurableRumble
{
    internal static class ModEntry
    {
        internal static ModConfig Config { get; set; } = new();
    }
}

namespace StardewValley
{
    internal static class Rumble
    {
        internal static readonly List<(string Kind, float Power, float Duration)> Requests = new();

        public static void rumble(float power, float milliseconds) => Requests.Add(("Rumble", power, milliseconds));

        public static void rumbleAndFade(float power, float milliseconds) => Requests.Add(("Fade", power, milliseconds));

        public static void stopRumbling() => Requests.Add(("Stop", 0f, 0f));
    }
}
