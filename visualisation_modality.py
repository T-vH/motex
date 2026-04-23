import numpy as np
import pandas as pd
import re
from pathlib import Path
import matplotlib.pyplot as plt
import os


# =====================================================
# PATHS
# =====================================================
DATASET_FOLDERS = [
    os.path.join("data", f"P{i:02d}") for i in range(1, 15)
]

OUTPUT_DIR = Path("figures")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# =====================================================
# REGEX
# =====================================================
SCEN_RE = re.compile(r"_(C1|C2|L2|T7)([A-D])_")

# =====================================================
# CONSTANTS
# =====================================================
TIME_COL = "Time"
TTC_COL = "TTC"
X_COL = "PosX"
Z_COL = "PosZ"
BRAKE_COL = "Brake"
STEER_COL = "Steer"

WARNING_TTC = 2.0
CRASH_TTC = 0.7

PRE_T = 6   # just enough to see approach
POST_T = 1.5   # reaction + crash dominates
N_TIME = 300

BRAKE_TH = 0.10
STEER_TH = 0.03

MOD_ORDER = ["control", "audio", "visual", "multimodal"]
MOD_COLOR = {
    "control": "#FFFFFF",      # white
    "audio":   "#00BFFF",      # cyan
    "visual":  "#00C853",      # green
    "multimodal": "#FF1744",   # red
}


# =====================================================
# HELPERS
# =====================================================
def robust_read_csv(path):
    df = pd.read_csv(path, sep=";", engine="python")
    df.columns = df.columns.str.replace("\ufeff", "").str.strip()
    return df


def parse_scenario(name):
    m = SCEN_RE.search(name)
    return (m.group(1), m.group(2)) if m else (None, None)


def parse_modality(name):
    n = name.lower()
    if "control" in n:
        return "control"
    if "audio" in n:
        return "audio"
    if "visual" in n:
        return "visual"
    if "multi" in n:
        return "multimodal"
    return None


def compute_time_rel(df):
    t = pd.to_numeric(df[TIME_COL], errors="coerce")
    return t - t.iloc[0]


def cue_time(time_rel, ttc):
    idx = ttc.index[(ttc.shift(1) > WARNING_TTC) & (ttc <= WARNING_TTC)]
    return float(time_rel.loc[idx[0]]) if len(idx) else None


def safe_interp(t_common, t, y):
    m = np.isfinite(t) & np.isfinite(y)
    if m.sum() < 2:
        return np.full_like(t_common, np.nan)
    return np.interp(t_common, t[m], y[m], left=np.nan, right=np.nan)


# =====================================================
# TRIAL EXTRACTION
# =====================================================
def extract_trial(df, fam, var):
    time_rel = compute_time_rel(df)
    ttc = pd.to_numeric(df[TTC_COL], errors="coerce")

    ct = cue_time(time_rel, ttc)
    if ct is None:
        return None

    mask = (time_rel >= ct - PRE_T) & (time_rel <= ct + POST_T)
    if mask.sum() < 20:
        return None

    t = time_rel[mask].values - ct
    x = pd.to_numeric(df.loc[mask, X_COL], errors="coerce").values
    z = pd.to_numeric(df.loc[mask, Z_COL], errors="coerce").values
    ttc_w = ttc.loc[mask].values

    # Reaction = earliest of brake or steer after cue
    rt = None
    post = time_rel >= ct

    brake = pd.to_numeric(df[BRAKE_COL], errors="coerce").fillna(0)
    steer = pd.to_numeric(df[STEER_COL], errors="coerce").fillna(0)

    b_idx = df.index[(brake > BRAKE_TH) & post]
    s_idx = df.index[(steer.abs() > STEER_TH) & post]

    times = []
    if len(b_idx):
        times.append(time_rel.loc[b_idx[0]])
    if len(s_idx):
        times.append(time_rel.loc[s_idx[0]])
    if times:
        rt = min(times) - ct

    return dict(t=t, x=x, z=z, ttc=ttc_w, rt=rt)


# =====================================================
# PLOTTING
# =====================================================
def plot_scenario(data, fam, var):
    t_common = np.linspace(-PRE_T, POST_T, N_TIME)

    # Match Unity top-down road aspect (wide)
    ROAD_ASPECT = 5   # width / height
    HEIGHT = 6.0
    WIDTH = HEIGHT * ROAD_ASPECT

    fig, ax = plt.subplots(figsize=(WIDTH, HEIGHT))

    fig.patch.set_alpha(0.0)
    ax.patch.set_alpha(0.0)
    ax.set_axis_off()

    for mod in MOD_ORDER:
        trials = data.get(mod, [])
        if not trials:
            continue

        X, Z, TTC = [], [], []
        rts = []

        for tr in trials:
            X.append(safe_interp(t_common, tr["t"], tr["x"]))
            Z.append(safe_interp(t_common, tr["t"], tr["z"]))
            TTC.append(safe_interp(t_common, tr["t"], tr["ttc"]))
            if tr["rt"] is not None:
                rts.append(tr["rt"])

        x_m = np.nanmean(X, axis=0)
        z_m = np.nanmean(Z, axis=0)
        ttc_m = np.nanmean(TTC, axis=0)

        ax.plot(x_m, z_m, color=MOD_COLOR[mod], linewidth=3, alpha=0.95)

        # Hazard cue (t=0)
        i0 = np.argmin(np.abs(t_common))
        ax.scatter(x_m[i0], z_m[i0], s=100, facecolors='none',
                   edgecolors="#2979FF", linewidths=2, zorder=10)

        # Mean reaction
        if rts:
            tr = np.mean(rts)
            ir = np.argmin(np.abs(t_common - tr))
            ax.scatter(x_m[ir], z_m[ir], s=100, facecolors='none',
                       edgecolors="#FF9100", linewidths=2, zorder=11)

        # Mean crash (from mean TTC)
        crash_idx = np.where(ttc_m < CRASH_TTC)[0]
        if len(crash_idx):
            ic = crash_idx[0]
            ax.scatter(x_m[ic], z_m[ic], s=140, marker="x",
                       color="#FF1744", linewidths=3, zorder=12)

    out = OUTPUT_DIR / f"{fam}{var}_modality_overlay.png"
    fig.savefig(out, dpi=300, transparent=True, bbox_inches=None)
    plt.close(fig)


# =====================================================
# MAIN
# =====================================================
scenarios = {}

for folder in DATASET_FOLDERS:
    for csv in Path(folder).glob("*.csv"):
        fam, var = parse_scenario(csv.name)
        if fam is None:
            continue

        mod = parse_modality(csv.name)
        if mod is None:
            continue

        df = robust_read_csv(csv)

        # Exclude participant who dropped out
        if "participant" in df.columns:
            df = df[df["participant"] != "P08"]
    
        tr = extract_trial(df, fam, var)
        if tr is None:
            continue

        scenarios.setdefault((fam, var), {}).setdefault(mod, []).append(tr)

if not scenarios:
    raise RuntimeError("No valid trials parsed — check filenames or TTC data")

for (fam, var), data in sorted(scenarios.items()):
    plot_scenario(data, fam, var)

print(f"\n✔ Overlay images saved to:\n{OUTPUT_DIR}")
