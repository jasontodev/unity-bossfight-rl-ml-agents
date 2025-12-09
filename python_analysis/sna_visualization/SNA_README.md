# Social Network Analysis (SNA) Visualizer

This folder contains two complementary visualizers for the boss-fight episode data:

- **Static publication plot** (`publication_sna.py`): Matplotlib/NetworkX PNG export with data‑driven sizing and curved edges to avoid overlap.
- **Interactive HTML plot** (`interactive_sna.py`): Vis.js-based UI with per-agent controls, draggable nodes, and save/load of all settings in the browser.

Both tools read the same episode JSON and compute node sizes and edge widths from the data using logarithmic scaling (no hardcoded thicknesses).

---

## Data Inputs

Expected episode format (compatible with the rest of the analysis scripts):
```json
{
  "episode": 123,
  "actions": [
    { "agentId": "Boss", "targetId": "Party Member 3", "branch": "attack", "value": 1 },
    { "agentId": "Party Member 2", "targetId": "Party Member 2", "branch": "heal", "value": 1 },
    { "agentId": "Party Member 1", "targetId": "Boss", "branch": "attack", "value": 1 }
  ]
}
```
Key fields:
- `agentId`, `targetId`: Who acted and who was targeted.
- `branch`: `"attack"`, `"heal"`, `"threat_boost"` (threat is inferred from attacks to Boss).
- `value`: Typically `1` when the action occurs.
- Optional metadata such as `winCondition`, `duration`, and class selection counts are used where available.

The scripts also accept a top-level object with an `episodes` array.

---

## Static Plot (`publication_sna.py`)

**What it does**
- Builds a directed NetworkX graph of Boss, Tank, Healer, MeleeDPS, RangedDPS.
- Computes node sizes from class selection counts (log scale; falls back to degree centrality).
- Computes edge widths from action frequencies (log scale). Visual emphasis multipliers (e.g., boss→melee late) are applied *after* scaling.
- Uses curved edges with varying radii to prevent overlap between threat and damage lines.
- Draws a self-heal as a green curved arrow into the Healer node.
- Adds taunt (Tank→Boss) as purple, threat as blue, boss damage as yellow, party damage as red, healing as green.

**Run it**
```bash
cd python_analysis/sna_visualization
python publication_sna.py --input episodes.json --output sna.png --compare --early-range 0 15000 --late-range 15001 30000
```
Outputs two PNGs (`sna_early.png`, `sna_late.png`) when `--compare` is used. Without `--compare`, a single `sna.png` is produced.

Key options:
- `--input <path>`: Episode JSON.
- `--output <path>`: Base output filename.
- `--compare --early-range a b --late-range c d`: Split early/late windows.

---

## Interactive Plot (`interactive_sna.py`)

**What it does**
- Converts the SNA graph to vis.js nodes/edges and emits a self-contained HTML.
- Nodes are draggable and independent (physics off by default).
- Per-agent controls for:
  - Node size
  - Edge thickness per edge type (damage, threat, healing, taunt) and per Boss→party target
  - Edge curvature (roundness) per edge type
  - Edge type (continuous/straight/curved CW/curved CCW)
- Global control for arrow length.
- Self-heal (Healer→Healer) added automatically if missing.
- Save/Load: All positions, sizes, widths, curvatures, edge types, and arrow length persist to `localStorage`.

**Run it**
```bash
cd python_analysis/sna_visualization
python interactive_sna.py --input ../episodes.json --output interactive_sna.html --compare --early-range 0 15000 --late-range 15001 30000
```
Outputs `interactive_sna_early.html` and `interactive_sna_late.html`. Open in a browser to use.

**UI layout**
- Left sidebar: stacked controls (ultrawide-friendly), grouped by agent (Boss, Tank, Healer, MeleeDPS, RangedDPS).
- Right: large canvas (`#mynetwork`) for the graph. Nodes drag independently.

**Save/Load**
- Save: Stores node positions, node size multipliers, edge width multipliers, curvature, edge types, and arrow length in `localStorage`.
- Load: Restores all values and updates sliders/dropdowns to match.

---

## Visual Encoding

- **Colors**: Boss damage (yellow), party damage (red), threat (blue), taunt (purple), healing (green).
- **Widths**: `log(1 + weight)` scaling from action counts; minor multipliers for visual emphasis only.
- **Curves**: Party damage and threat use opposite arc directions/radii to avoid overlap; healing self-loop uses a curved arrow into Healer.
- **Nodes**: Sized from class selection counts (log scale), with fallback to centrality if counts absent.

---

## Tips & Troubleshooting

- If edges overlap in the interactive view, adjust curvature/type per edge type in the sidebar.
- If arrows look small, increase the global Arrow Length slider.
- If data lacks `targetId`, attacks to Boss and heals to self are inferred when possible.
- If you change screen size, reload to reflow the layout; positions persist only if saved.

---

## Requirements

- Python 3.9+ recommended.
- Dependencies (install from repo root or `python_analysis`):
  ```bash
  pip install -r python_analysis/requirements.txt
  ```
- For interactive HTML generation, no browser dependencies beyond a modern browser (vis.js is pulled from CDN).


