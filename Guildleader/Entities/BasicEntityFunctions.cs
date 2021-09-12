using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader.Entities
{
    public static class BasicEntityFunctions
    {
        public static void DigBlock(EntityAction action)
        {
            Int3 targetPosition = action.details.targetCoords;
            Int3 userPosition = action.caster.worldPositon;
            int xdif = Math.Abs(targetPosition.x - userPosition.x);
            int ydif = Math.Abs(targetPosition.y - userPosition.y);
            int zdif = Math.Abs(targetPosition.z - userPosition.z);
            if (xdif <= 1 && ydif <= 1 && zdif <= 1)
            {
                WorldManager.currentWorld.SetTileIDAtLocation(targetPosition, 0);
            }
        }
    }
}
