import numpy as np
import pandas as pd
import re
from pathlib import Path
import matplotlib.pyplot as plt
import matplotlib.cm as cm
import matplotlib.colors as mcolors

# =====================================================
# PATHS
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

OUTPUT_DIR = Path(r"C:\Users\thoma\OneDrive\Documenten\MOTEX\motex_plots")
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
THROTTLE_COL = "Throttle"

Z_SCALE = 1   # vertical exaggeration (0.5 = half-scale)

WARNING_TTC_THRESHOLD = 2.0
COLLISION_TTC_THRESHOLD = 0.7

PRE_CUE_WINDOW = 4.0
POST_CUE_WINDOW = 2.0

BRAKE_THRESHOLD = 0.10
STEER_DEV_THRESHOLD = 0.03
STEER_MIN_DURATION = 0.15
BASELINE_WINDOW = 2.0
THROTTLE_WINDOW = 200.0

# =====================================================
# HELPERS
# =====================================================
def robust_read_csv(path):
    df = pd.read_csv(path, sep=";", engine="python")
    df.columns = df.columns.str.replace("\ufeff", "").str.strip()
    return df

def parse_scenario(fname):
    m = SCEN_RE.search(fname)
    return (m.group(1), m.group(2)) if m else (None, None)

def compute_time_rel(df):
    t = pd.to_numeric(df[TIME_COL], errors="coerce")
    return t - t.iloc[0]

def sanitize_ttc(df):
    ttc = pd.to_numeric(df[TTC_COL], errors="coerce")
    return ttc.where(np.isfinite(ttc), np.nan)

def cue_onset_time(time_rel, ttc):
    cross = ttc.index[(ttc.shift(1) > WARNING_TTC_THRESHOLD) &
                      (ttc <= WARNING_TTC_THRESHOLD)]
    return float(time_rel.loc[cross[0]]) if len(cross) else None

def compute_speed_kmh(df, time_rel):
    dx = df[X_COL].diff()
    dz = df[Z_COL].diff()
    dt = time_rel.diff()

    speed = np.sqrt(dx**2 + dz**2) / dt * 3.6

    # Remove impossible values
    speed = speed.replace([np.inf, -np.inf], np.nan)

    # Forward-fill first valid speed instead of zero
    speed = speed.fillna(method="bfill")

    return speed


# =====================================================
# TRIAL EXTRACTION
# =====================================================
def extract_trial(df, family, variant):
    time_rel = compute_time_rel(df)
    ttc = sanitize_ttc(df)
    speed = compute_speed_kmh(df, time_rel)

    cue_time = cue_onset_time(time_rel, ttc)
    if cue_time is None:
        return None

    # --- collision index first (defines the window end) ---
    col_candidates_all = df.index[pd.to_numeric(df[TTC_COL], errors="coerce") < COLLISION_TTC_THRESHOLD]
    if len(col_candidates_all) == 0:
        return None

    collision_idx = col_candidates_all[0]
    crash_time = float(time_rel.loc[collision_idx])

    # --- window = last 4 seconds before crash ---
    t0, t1 = crash_time - 4.0, crash_time
    mask = (time_rel >= t0) & (time_rel <= t1)

    df_win = df.loc[mask].copy()
    time_win = time_rel.loc[mask].copy()
    speed_win = speed.loc[mask].copy()


    # Relative origin inside window
#    x = df_win[X_COL] - df_win[X_COL].iloc[0]
#    z = df_win[Z_COL] - df_win[Z_COL].iloc[0]
    x = df_win[X_COL]
    z = df_win[Z_COL]


    if family == "C1" and variant == "D":
        x = -x

    events = {}

    cue_idx = (time_rel - cue_time).abs().idxmin()
    if cue_idx in df_win.index:
        events["cue"] = cue_idx

    post = df.loc[mask & (time_rel >= cue_time)]
    b = post[post[BRAKE_COL] > BRAKE_THRESHOLD]
    if len(b) and b.index[0] in df_win.index:
        events["brake"] = b.index[0]


    baseline = df_win[
        (time_win >= cue_time - BASELINE_WINDOW) &
        (time_win < cue_time)
    ]
    steer_mu = baseline[STEER_COL].astype(float).mean()
    steer = post[STEER_COL].astype(float)
    dev = (steer - steer_mu).abs() > STEER_DEV_THRESHOLD

    start = None
    for i in dev.index:
        if dev.loc[i] and start is None:
            start = i
        elif not dev.loc[i] and start is not None:
            if time_rel.loc[i] - time_rel.loc[start] >= STEER_MIN_DURATION:
                events["steer"] = start
                break
            start = None

    if THROTTLE_COL in df.columns:
        throttle = df[THROTTLE_COL].astype(float)
        drop = throttle.diff() < -0.05
        for idx in drop[drop].index:
            if abs(time_rel.loc[idx] - cue_time) <= THROTTLE_WINDOW:
                events["throttle"] = idx
                break

    return x, z, speed_win, events, collision_idx


# =====================================================
# PLOTTING
# =====================================================
def plot_scenario(trials, family, variant):

    # ---- MATCH UNITY SCREENSHOT ASPECT ----
    SCREEN_ASPECT = 1920 / 1080   # CHANGE if your screenshot differs
    HEIGHT = 6
    WIDTH = HEIGHT * SCREEN_ASPECT

    fig, ax = plt.subplots(figsize=(WIDTH, HEIGHT))

    # Transparent background
    fig.patch.set_alpha(0.0)
    ax.patch.set_alpha(0.0)

    cmap = cm.viridis

    # ---- FIX WORLD LIMITS (CRITICAL) ----
    Z_VIS = Z_SCALE  # set Z_SCALE = 2.0 at top for “2× bigger”
    zs = np.concatenate([t[1].values * Z_VIS for t in trials])


    # ---- SPEED NORMALIZATION ----
    all_speeds = np.concatenate([t[2].values for t in trials])
    norm = mcolors.Normalize(
        vmin=np.nanmin(all_speeds),
        vmax=np.nanmax(all_speeds)
    )

    # ---- TRAJECTORIES ----
    for x, z, speed, events, col_idx in trials:
        if col_idx is not None and col_idx in x.index:
            end = x.index.get_loc(col_idx)
        else:
            end = len(x) - 1
        for i in range(end):
            ax.plot(
                x.iloc[i:i+2],
                (z * Z_VIS).iloc[i:i+2],
                color=cmap(norm(speed.iloc[i])),
                alpha=0.5,
                linewidth=2
            )

        if col_idx is not None:
            ax.scatter(
                x.loc[col_idx],
                (z * Z_VIS).loc[col_idx],
                marker="x",
                s=120,
                color="red",
                linewidths=2
            )

    # ---- EVENT MARKERS ----
    for ev, color in [("cue","blue"),("brake","orange")]:
        xs_ev, zs_ev = [], []
        for x, z, _, events, _ in trials:
            if ev in events:
                xs_ev.append(x.loc[events[ev]])
                zs_ev.append((z * Z_VIS)[events[ev]])
        if xs_ev:
            ax.scatter(xs_ev, zs_ev, s=80, color=color, zorder=5)

    # ---- CLEAN OVERLAY ----
    ax.set_axis_off()

    out = OUTPUT_DIR / f"{family}{variant}_overlay.png"
    fig.savefig(
        out,
        dpi=300,
        bbox_inches=None,   # IMPORTANT: do NOT use "tight"
        transparent=True
    )
    plt.close(fig)


# =====================================================
# MAIN
# =====================================================
if __name__ == "__main__":
    scenarios = {}

    for folder in DATASET_FOLDERS:
        for csv in Path(folder).glob("*.csv"):
            family, variant = parse_scenario(csv.name)
            if family is None:
                continue

            df = robust_read_csv(csv)
            res = extract_trial(df, family, variant)
            if res is None:
                continue

            scenarios.setdefault((family, variant), []).append(res)

    print("\n=== SCENARIOS WITH VALID TTC CUES ===")
    for (family, variant), trials in sorted(scenarios.items()):
        print(f"{family}{variant}: {len(trials)} trials")
        plot_scenario(trials, family, variant)
