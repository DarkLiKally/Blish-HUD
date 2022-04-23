using Microsoft.Xna.Framework;

namespace Blish_HUD; 

public static class Vector2Extension {

    public static Vector2 OffsetBy(this Vector2 vector, float xOffset, float yOffset) {
        return new Vector2(vector.X + xOffset, vector.Y + yOffset);
    }

    public static Vector2 ToWorldCoord(this Vector2 vector) {
        return new Vector2(WorldUtil.GameToWorldCoord(vector.X), WorldUtil.GameToWorldCoord(vector.Y));
    }

    public static Vector2 ToGameCoord(this Vector2 vector) {
        return new Vector2(WorldUtil.WorldToGameCoord(vector.X), WorldUtil.WorldToGameCoord(vector.Y));   
    }

}