import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path

# =====================================================
# PATHS
# =====================================================
DATA_PATH = Path(r"C:\Users\thoma\OneDrive\Documenten\MOTEX\output_motex_master.csv")
OUTPUT_DIR = Path(r"C:\Users\thoma\OneDrive\Documenten\MOTEX")
OUTPUT_DIR.mkdir(exist_ok=True)

# =====================================================
# LOAD DATA
# =====================================================
df = pd.read_csv(DATA_PATH)

# Construct scenario label
df["scenario"] = df["scenario_family"] + df["scenario_variant"]

MOD_ORDER = ["Control", "Audio", "Visual", "Multimodal"]
SCEN_ORDER = [
    "C1A","C1B","C1C","C1D",
    "C2A","C2B","C2C","C2D",
    "T7A","T7B","T7C","T7D",
]

# =====================================================
# GENERIC 4×4 PLOTTING FUNCTION
# =====================================================
def plot_4x4(
    df,
    value_col,
    ylabel,
    out_name,
    ylims=None
):
    fig, axes = plt.subplots(3, 4, figsize=(16, 16), sharey=True)
    axes = axes.flatten()

    for ax, scen in zip(axes, SCEN_ORDER):
        sub = df[df["scenario"] == scen]

        if sub.empty:
            ax.axis("off")
            continue

        data = []
        labels = []

        for mod in MOD_ORDER:
            vals = sub.loc[sub["modality"] == mod, value_col].dropna()
            if len(vals):
                data.append(vals.values)
                labels.append(mod)

        if not data:
            ax.axis("off")
            continue

        # Boxplots
        ax.boxplot(
            data,
            widths=0.6,
            patch_artist=True,
            boxprops=dict(facecolor="lightgray", edgecolor="black"),
            medianprops=dict(color="black"),
            whiskerprops=dict(color="black"),
            capprops=dict(color="black"),
            flierprops=dict(marker='o', markersize=3, alpha=0.5)
        )

        # Jittered individual points
        for i, vals in enumerate(data, start=1):
            x = np.random.normal(i, 0.04, size=len(vals))
            ax.plot(x, vals, "k.", alpha=0.6, markersize=4)

        ax.set_title(scen, fontsize=16)
        ax.set_xticks(range(1, len(labels) + 1))
        ax.set_xticklabels(labels, rotation=45, ha="right", fontsize=16)


        if ylims:
            ax.set_ylim(*ylims)

        ax.grid(axis="y", alpha=0.3)

    # Hide unused axes
    for ax in axes[len(SCEN_ORDER):]:
        ax.axis("off")

    fig.supylabel(ylabel, fontsize=24)
    fig.tight_layout(rect=[0.04, 0.02, 1, 0.98])

    out = OUTPUT_DIR / out_name
    fig.savefig(out, dpi=300)
    plt.close(fig)

    print(f"Saved: {out}")

# =====================================================
# FIGURE 1 — MINIMUM TTC
# =====================================================
df_ttc = df[df["min_TTC"].notna()]

plot_4x4(
    df=df_ttc,
    value_col="min_TTC",
    ylabel="Minimum TTC (s)",
    out_name="min_TTC_4x4_by_scenario.png",
    ylims=(0, df_ttc["min_TTC"].max() * 1.1)
)

# =====================================================
# FIGURE 2 — REACTION TIME
# =====================================================
df_rt = df[df["RT_primary_ms"].notna()]

plot_4x4(
    df=df_rt,
    value_col="RT_primary_ms",
    ylabel="Reaction time (ms)", 
    out_name="RT_4x4_by_scenario.png",
    ylims=(0, df_rt["RT_primary_ms"].quantile(0.98))
)

print("\n✔ 4×4 TTC and RT figures generated successfully.")
