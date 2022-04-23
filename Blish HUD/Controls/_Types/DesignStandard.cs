﻿using Microsoft.Xna.Framework;

namespace Blish_HUD.Controls {
    public struct DesignStandard {

        public Point Size { get; }

        public Point PanelOffset { get; }

        public Point ControlOffset { get; }

        public DesignStandard(Point size, Point panelOffset, Point controlOffset) {
            this.Size          = size;
            this.PanelOffset   = panelOffset;
            this.ControlOffset = controlOffset;
        }

    }
}
