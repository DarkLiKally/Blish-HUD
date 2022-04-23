namespace Blish_HUD.Controls {
    public interface IAccordion {

        bool Collapsed { get; set; }

        bool ToggleAccordionState();

        void Expand();

        void Collapse();

    }
}
