using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

/// <summary>Tracks item lanes through a connected run of overlapping slots.</summary>
public sealed class PlannerMultiDayLaneLayout
{
    public IReadOnlyDictionary<int, IReadOnlyList<PlannerMultiDayLane>> Create(
        IReadOnlyList<PlannerSlotView> slots,
        DateOnly date,
        PlannerFocusBlock? activeFocusBlock)
    {
        var snapshots = new List<(int SlotIndex, List<LaneWidth> Lanes, int LaneCount)>();
        List<LaneWidth>? lanes = null;
        HashSet<string> previousIdentities = [];

        for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            var slot = slots[slotIndex];
            // A lone item returns to the date guide. The next overlap starts
            // a fresh lane group, even if that item continues into it.
            if (slot.Items.Length <= 1)
            {
                lanes = null;
                previousIdentities.Clear();
                continue;
            }

            var identities = slot.Items.Select(item => item.Identity).ToHashSet();
            if (lanes is null || !identities.Overlaps(previousIdentities))
            {
                lanes = [];
            }

            var rows = PlannerTimelineRenderModel.ForSlot(slot);
            var ordered = slot.Items.Select((item, index) => (Item: item, Row: rows[index], Index: index))
                .OrderBy(entry => entry.Item.Start)
                .ThenBy(entry => entry.Index);
            foreach (var entry in ordered)
            {
                var laneIndex = lanes.FindIndex(lane => lane.Identity == entry.Item.Identity);
                if (laneIndex < 0)
                {
                    laneIndex = lanes.Count;
                    lanes.Add(new LaneWidth(entry.Item.Identity));
                }

                var width = PlannerMultiDayCellLayout.NaturalWidth(
                    entry.Row, entry.Item,
                    PlannerMultiDayCellLayout.ShowFinish(entry.Item, date, activeFocusBlock),
                    laneIndex == 0);
                var lane = lanes[laneIndex];
                if (entry.Row.IsSelected && laneIndex > 0)
                {
                    lane.SelectedWidth = Math.Max(lane.SelectedWidth, width);
                    lane.ReserveSelectionColumn = true;
                }
                else
                {
                    lane.NormalWidth = Math.Max(lane.NormalWidth, width);
                }
            }

            snapshots.Add((slotIndex, lanes, lanes.Count));
            previousIdentities = identities;
        }

        // Widths are finalized after the full group is known, while each
        // snapshot retains only the lanes that had appeared at that slot.
        return snapshots.ToDictionary(
            snapshot => snapshot.SlotIndex,
            snapshot => (IReadOnlyList<PlannerMultiDayLane>)snapshot.Lanes
                .Take(snapshot.LaneCount)
                .Select(lane => new PlannerMultiDayLane(lane.Identity, lane.Width,
                    lane.ReserveSelectionColumn))
                .ToArray());
    }

    private sealed class LaneWidth(string identity)
    {
        public string Identity { get; } = identity;
        public int NormalWidth { get; set; }
        public int SelectedWidth { get; set; }
        public bool ReserveSelectionColumn { get; set; }
        public int Width => Math.Max(SelectedWidth,
            NormalWidth + (ReserveSelectionColumn ? 1 : 0));
    }
}
