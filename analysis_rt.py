# =====================================================
# MOTEX — Reaction Strategy + RT Variability Analysis
# =====================================================

import pandas as pd
from scipy.stats import chi2_contingency, levene
import statsmodels.formula.api as smf
import statsmodels.api as sm   # <-- REQUIRED for families

# =====================================================
# CONFIG
# =====================================================
MASTER_CSV = "data_simulator.csv"
MODALITIES = ["Visual", "Audio", "Multimodal"]

# =====================================================
# LOAD
# =====================================================
df = pd.read_csv(MASTER_CSV)

# Exclude participant who dropped out
if "participant" in df.columns:
    df = df[df["participant"] != "P08"]

df_valid = df[
    df["modality"].isin(MODALITIES) &
    df["RT_ms"].notna() &
    df["RT_source"].isin(["brake", "steer"])
].copy()

print("\nLoaded valid trials:", len(df_valid))
print("Participants:", df_valid["participant"].unique())

# =====================================================
# 1. REACTION STRATEGY ANALYSIS
# =====================================================
print("\n================ STRATEGY ANALYSIS ================")

# ---------- Counts & proportions ----------
strategy_counts = (
    df_valid
    .groupby(["modality", "RT_source"])
    .size()
    .unstack(fill_value=0)
)

strategy_props = strategy_counts.div(strategy_counts.sum(axis=1), axis=0)

print("\nReaction counts:")
print(strategy_counts)

print("\nReaction proportions:")
print(strategy_props)

# ---------- Chi-square ----------
chi2, p, dof, exp = chi2_contingency(strategy_counts)

print("\nChi-square test (RT_source × modality)")
print(f"χ²({dof}) = {chi2:.3f}, p = {p:.4f}")

# ---------- Logistic regression (clustered by participant) ----------
df_valid["steer_binary"] = (df_valid["RT_source"] == "steer").astype(int)

glm = smf.glm(
    "steer_binary ~ C(modality)",
    data=df_valid,
    family=sm.families.Binomial()   # <-- FIX
).fit(
    cov_type="cluster",
    cov_kwds={"groups": df_valid["participant"]}
)

print("\nLogistic regression (cluster-robust SEs by participant)")
print(glm.summary())

# =====================================================
# 2. RT VARIABILITY ANALYSIS
# =====================================================
print("\n================ RT VARIABILITY ====================")

# ---------- Within-participant SD ----------
rt_sd = (
    df_valid
    .groupby(["participant", "modality"])["RT_ms"]
    .std()
    .reset_index(name="RT_sd")
)

print("\nWithin-participant RT SDs (head):")
print(rt_sd.head())

# ---------- Summary ----------
print("\nRT SD summary by modality:")
print(rt_sd.groupby("modality")["RT_sd"].describe())

# ---------- Levene test ----------
groups = [
    rt_sd[rt_sd["modality"] == m]["RT_sd"].dropna()
    for m in MODALITIES
]

lev_stat, lev_p = levene(*groups, center="median")

print("\nLevene test on RT variability (median-centered)")
print(f"W = {lev_stat:.3f}, p = {lev_p:.4f}")

# =====================================================
# DONE
# =====================================================
print("\nAnalysis complete.")
