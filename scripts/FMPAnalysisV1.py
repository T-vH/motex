import os
from pathlib import Path
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import re

# =====================================================
# USER SETTINGS
# =====================================================
DATASET_FOLDERS = [
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P01",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P02",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P03",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P04",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P05",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P06",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P07",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P08",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P09",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P10",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P11",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P12",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P13",
    r"C:\Users\thoma\Documents\Unity\Projects\MOTEX\MotorcyclePhysics-master\MotorcyclePhysics-master\Assets\Logs\P14",
]
OUTPUT_CSV = r"C:\Users\thoma\OneDrive\Documenten\MOTEX\output_motex_master.csv"
PLOT_DIR = r"C:\Users\thoma\OneDrive\Documenten\MOTEX\motex_plots"

WARNING_TTC_THRESHOLD = 2.0   # seconds
BASELINE_WINDOW = 2.0         # seconds
MAX_MEANINGFUL_TTC = 10.0     # seconds

# Control thresholds
BRAKE_THRESHOLD = 0.10
STEER_THRESHOLD = 0.10
STEER_SD_MULT = 3.0

KNOWN_MODALITIES = ["Control", "Visual", "Audio", "Multimodal"]

# Column names
TIME_COL = "Time"
STEER_COL = "Steer"
BRAKE_COL = "Brake"
TTC_COL = "TTC"


# =====================================================
# UTILITIES
# =====================================================
def ensure_dir(p):
    Path(p).mkdir(parents=True, exist_ok=True)


def robust_read_csv(path):
    df = pd.read_csv(path, sep=";", engine="python")
    df.columns = df.columns.str.replace("\ufeff", "").str.strip()
    return df


def parse_filename(fname):
    """
    Parse filenames like:
    MOTEX_P01_20251218_191427_Trial03_C1B_Control.csv
    MOTEX_T01_20251219_135714_Trial01_T7A_Visual.csv
    """
    participant_match = re.search(r"_([A-Z]\d+)_", fname)
    if not participant_match:
        raise ValueError(f"Participant not found: {fname}")
    participant = participant_match.group(1)

    scenario_match = re.search(r"_(C1|C2|L2|T7)([A-D])_", fname)
    if not scenario_match:
        raise ValueError(f"Scenario not found: {fname}")
    scenario_family = scenario_match.group(1)
    scenario_variant = scenario_match.group(2)

    if "Multimodal" in fname:
        modality = "Multimodal"
    elif "Visual" in fname:
        modality = "Visual"
    elif "Audio" in fname:
        modality = "Audio"
    elif "Control" in fname:
        modality = "Control"
    else:
        raise ValueError(f"Modality not found: {fname}")

    return {
        "participant": participant,
        "scenario_family": scenario_family,
        "scenario_variant": scenario_variant,
        "modality": modality
    }


def cue_onset_time(df, threshold):
    """
    Robust cue onset:
    - If TTC already <= threshold at first valid sample → cue at start
    - Else first crossing from >threshold to <=threshold
    """
    s = df["TTC_s"]
    first_valid = s.first_valid_index()
    if first_valid is None:
        return None

    if s.loc[first_valid] <= threshold:
        return df.loc[first_valid, "Time_rel"]

    cross = df.index[(s.shift(1) > threshold) & (s <= threshold)]
    if len(cross) == 0:
        return None

    return df.loc[cross[0], "Time_rel"]


# =====================================================
# CORE ANALYSIS
# =====================================================
def analyse_trial(csv_path):
    meta = parse_filename(csv_path.name)
    df = robust_read_csv(csv_path)

    required = {TIME_COL, STEER_COL, BRAKE_COL, TTC_COL}
    if not required.issubset(df.columns):
        raise ValueError(f"Missing required columns in {csv_path.name}")

    df = df.sort_values(TIME_COL).reset_index(drop=True)

    # ---- Relative time ----
    t0 = df[TIME_COL].iloc[0]
    df["Time_rel"] = df[TIME_COL] - t0

    # ---- TTC sanitization ----
    df["TTC_s"] = pd.to_numeric(df[TTC_COL], errors="coerce")
    df.loc[df["TTC_s"] > MAX_MEANINGFUL_TTC, "TTC_s"] = np.nan
    df.loc[np.isinf(df["TTC_s"]), "TTC_s"] = np.nan

    # ---- Cue onset ----
    cue_time = cue_onset_time(df, WARNING_TTC_THRESHOLD)
    if cue_time is None:
        return {
            **meta,
            "RT_ms": np.nan,
            "RT_source": None,
            "min_TTC": np.nan,
            "has_cue": False
        }

    # ---- Baseline steering ----
    baseline = df[
        (df["Time_rel"] >= cue_time - BASELINE_WINDOW) &
        (df["Time_rel"] < cue_time)
    ]

    if len(baseline) < 5:
        return {
            **meta,
            "RT_ms": np.nan,
            "RT_source": None,
            "min_TTC": np.nan,
            "has_cue": False
        }

    steer_mu = baseline[STEER_COL].mean()
    steer_sd = baseline[STEER_COL].std(ddof=0)
    if pd.isna(steer_sd) or steer_sd == 0:
        steer_sd = 0.0001

    # ---- Reaction detection (STRICTLY AFTER cue) ----
    post = df[df["Time_rel"] > cue_time]

    rt_candidates = {}

    # Brake reaction
    b = post[post[BRAKE_COL] > BRAKE_THRESHOLD]
    if len(b):
        rt_candidates["brake"] = b.iloc[0]["Time_rel"] - cue_time

    # Steering reaction
    s = post[(post[STEER_COL] - steer_mu).abs() > (STEER_THRESHOLD + STEER_SD_MULT * steer_sd)]
    if len(s):
        rt_candidates["steer"] = s.iloc[0]["Time_rel"] - cue_time

    if not rt_candidates:
        return {
            **meta,
            "RT_ms": np.nan,
            "RT_source": None,
            "min_TTC": np.nan,
            "has_cue": True
        }

    rt_s = min(rt_candidates.values())
    rt_source = min(rt_candidates, key=rt_candidates.get)
    reaction_time = cue_time + rt_s

    # ---- min TTC until reaction ----
    min_ttc = df[
        (df["Time_rel"] >= cue_time) &
        (df["Time_rel"] <= reaction_time)
    ]["TTC_s"].min()

    return {
        **meta,
        "RT_ms": rt_s * 1000.0,
        "RT_source": rt_source,
        "min_TTC": min_ttc,
        "has_cue": True
    }


# =====================================================
# PLOTTING
# =====================================================
def make_boxplot_with_points(df_plot, metric, ylabel, filename, plot_dir, known_modalities):
    """
    Create a boxplot with:
    - boxplot outliers shown
    - all individual points overlaid with jitter
    """
    rng = np.random.default_rng(42)

    labels = [m for m in known_modalities if m in df_plot["modality"].unique()]
    data = [
        df_plot[df_plot["modality"] == m][metric].dropna().values
        for m in labels
    ]

    # Skip if no data
    if not any(len(arr) > 0 for arr in data):
        print(f"Skipping {metric}: no data available.")
        return

    fig, ax = plt.subplots(figsize=(7, 5))

    bp = ax.boxplot(
        data,
        tick_labels=labels,
        showfliers=True,
        widths=0.45,
        patch_artist=False,
        flierprops=dict(
            marker="o",
            markerfacecolor="none",
            markeredgecolor="black",
            markersize=6,
            linestyle="none"
        )
    )

    # Optional line styling for publication-like appearance
    for element in ["boxes", "whiskers", "caps", "medians"]:
        for artist in bp[element]:
            artist.set(color="black", linewidth=1.1)

    # Overlay all individual observations
    for i, y in enumerate(data, start=1):
        if len(y) == 0:
            continue
        x = rng.normal(loc=i, scale=0.04, size=len(y))
        ax.scatter(
            x, y,
            s=22,
            facecolors="white",
            edgecolors="black",
            linewidths=0.6,
            alpha=0.9,
            zorder=3
        )

    ax.set_ylabel(ylabel)
    ax.set_xlabel("")
    ax.set_title("")
    ax.grid(False)

    save_path = Path(plot_dir, filename)
    plt.tight_layout()
    plt.savefig(save_path, dpi=300)
    print(f"Saved plot → {save_path}")

    plt.show()
    plt.close()


# =====================================================
# BATCH RUN
# =====================================================
def run():
    ensure_dir(PLOT_DIR)
    rows = []

    for folder in DATASET_FOLDERS:
        for f in Path(folder).glob("*.csv"):
            if f.name.endswith(".meta"):
                continue
            try:
                rows.append(analyse_trial(f))
            except Exception as e:
                print(f"ERROR {f.name}: {e}")

    df = pd.DataFrame(rows)
    df.to_csv(OUTPUT_CSV, index=False)
    print(f"\nSaved {len(df)} trials → {OUTPUT_CSV}")

    # ---- Separate valid datasets per metric ----
    df_rt_valid = df[df["has_cue"] & df["RT_ms"].notna()].copy()
    df_ttc_valid = df[df["has_cue"] & df["min_TTC"].notna()].copy()

    print("\nCounts per modality (all trials):")
    print(df.groupby("modality").size())

    print("\nRT valid counts per modality:")
    print(df_rt_valid.groupby("modality").size())

    print("\nTTC valid counts per modality:")
    print(df_ttc_valid.groupby("modality").size())

    print("\nRT_ms summary by modality:")
    print(df_rt_valid.groupby("modality")["RT_ms"].describe())

    print("\nmin_TTC summary by modality:")
    print(df_ttc_valid.groupby("modality")["min_TTC"].describe())

    # ---- Plots ----
    make_boxplot_with_points(
        df_plot=df_rt_valid,
        metric="RT_ms",
        ylabel="RT_ms",
        filename="RT_ms_by_modality4.png",
        plot_dir=PLOT_DIR,
        known_modalities=KNOWN_MODALITIES
    )

    make_boxplot_with_points(
        df_plot=df_ttc_valid,
        metric="min_TTC",
        ylabel="min_TTC",
        filename="min_TTC_by_modality4.png",
        plot_dir=PLOT_DIR,
        known_modalities=KNOWN_MODALITIES
    )


if __name__ == "__main__":
    run()