using System;
using Microsoft.Xna.Framework;

namespace Blish_HUD; 

public static class PointExtensions {

    public static System.Drawing.Point ToSystemDrawingPoint(this Point point) {
        return new System.Drawing.Point(point.X, point.Y);
    }

    public static Point ScaleToUi(this Point point) {
        return new Point((int)(point.X * GameService.Graphics.UIScaleMultiplier),
                                                 (int)(point.Y * GameService.Graphics.UIScaleMultiplier));
    }

    public static Point UiToScale(this Point point) {
        return new Point((int)(point.X / GameService.Graphics.UIScaleMultiplier),
                                                 (int)(point.Y / GameService.Graphics.UIScaleMultiplier));
    }

    public static Rectangle InBounds(this Point point, Rectangle bounds) {
        return new Rectangle(bounds.Location, point);
    }

    public static System.Drawing.Size ToSystemDrawingSize(this Point point) {
        return new System.Drawing.Size(point.X, point.Y);
    }

    public static Point ToXnaPoint(this System.Drawing.Point point) {
        return new Point(point.X, point.Y);
    }

    public static Point ResizeKeepAspect(Point src, int maxWidth, int maxHeight, bool enlarge = false)
    {
        maxWidth  = enlarge ? maxWidth : Math.Min(maxWidth,   src.X);
        maxHeight = enlarge ? maxHeight : Math.Min(maxHeight, src.Y);

        decimal rnd = Math.Min(maxWidth / (decimal)src.X, maxHeight / (decimal)src.Y);
        return new Point((int)Math.Round(src.X * rnd), (int)Math.Round(src.Y * rnd));
    }
}