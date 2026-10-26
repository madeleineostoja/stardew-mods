using Microsoft.Xna.Framework;
using StardewValley;

namespace Overslept;

internal static class MorningCatchUp
{
    internal static void AdvanceTo(int target)
    {
        var characters = new List<NPC>();
        Utility.ForEachVillager(npc =>
        {
            if (!npc.isMarried())
                characters.Add(npc);
            return true;
        });

        // Complete only routes from earlier ticks; a route starting at wakeup time can play normally.
        var remainingTicks = (target / 100 * 60 + target % 100 - (Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100)) / 10;
        while (Game1.timeOfDay < target && remainingTicks-- > 0)
        {
            foreach (var npc in characters)
                CatchUpSchedule(npc, Game1.timeOfDay);
            Game1.performTenMinuteClockUpdate();
        }

        if (Game1.timeOfDay < target)
            throw new InvalidOperationException("The game clock did not advance at its normal ten-minute rate.");
    }

    private static void CatchUpSchedule(NPC npc, int time)
    {
        SettleSquareReturn(npc);
        npc.checkSchedule(time);
        // A new departure can request a return from a square activity before starting its route.
        if (npc.IsReturningToEndPoint())
        {
            SettleSquareReturn(npc);
            npc.checkSchedule(time);
        }
        CompleteScheduledRoute(npc);
    }

    private static void SettleSquareReturn(NPC npc)
    {
        if (!npc.IsReturningToEndPoint())
            return;

        var bounds = npc.GetBoundingBox();
        npc.Position += new Vector2(npc.lastCrossroad.Center.X - bounds.Center.X, npc.lastCrossroad.Center.Y - bounds.Center.Y);
        npc.returnToEndPoint();
    }

    private static void CompleteScheduledRoute(NPC npc)
    {
        var controller = npc.controller;
        if (controller is not { NPCSchedule: true, pathToEndPoint: not null } || npc.temporaryController is not null)
            return;

        // Warps can consume the remaining path, so check it again before taking another point.
        while (controller.pathToEndPoint.Count > 0)
        {
            var point = controller.pathToEndPoint.Pop();
            npc.Position = new Vector2(point.X * 64, point.Y * 64);
            controller.handleWarps(new Rectangle(point.X * 64, point.Y * 64, 64, 64));
        }

        npc.Halt();
        if (controller.finalFacingDirection != -1)
            npc.faceDirection(controller.finalFacingDirection);
        npc.DirectionsToNewLocation = null;
        npc.endOfRouteMessage.Value = npc.nextEndOfRouteMessage;
        npc.controller = null;
        controller.endBehaviorFunction?.Invoke(npc, controller.location);
    }
}
