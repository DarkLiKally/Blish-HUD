using Microsoft.Xna.Framework;

namespace Blish_HUD._Extensions; 

public static class ColorExtensions {

    public static Color ToXnaColor(this Gw2Sharp.WebApi.V2.Models.ColorMaterial color) {
        return new Color(color.Rgb[0], color.Rgb[1], color.Rgb[2]);
    }

}