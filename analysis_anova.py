import pandas as pd
from statsmodels.stats.anova import AnovaRM

# =====================================================
# CONFIG
# =====================================================
MASTER_CSV = "data_simulator.csv"
MODALITIES = ["Visual", "Audio", "Multimodal"]


# =====================================================
# LOAD DATA
# =====================================================
df = pd.read_csv(MASTER_CSV)

print("\nLoaded rows:", len(df))
print("Participants:", df["participant"].unique())

# =====================================================
# ---------------- RT ANALYSIS -------------------------
# =====================================================
print("\n================ RT ANALYSIS =================")

# ---- Filter valid RT trials ----
df_rt = df[
    df["modality"].isin(MODALITIES) &
    (df["has_cue"] == True) &
    df["RT_primary_ms"].notna()
].copy()

# ---- Participant × modality counts ----
counts_rt = (
    df_rt
    .groupby(["participant", "modality"])
    .size()
    .unstack(fill_value=0)
)

# ---- Keep only balanced participants ----
valid_participants_rt = counts_rt[
    (counts_rt[MODALITIES] > 0).all(axis=1)
].index

df_rt_balanced = df_rt[
    df_rt["participant"].isin(valid_participants_rt)
].copy()

# ---- Aggregate per participant × modality ----
df_rt_agg = (
    df_rt_balanced
    .groupby(["participant", "modality"], as_index=False)
    .agg(RT_primary_ms=("RT_primary_ms", "mean"))  # or "median"
)

# ---- Run RM-ANOVA ----
aov_rt = AnovaRM(
    data=df_rt_agg,
    depvar="RT_primary_ms",
    subject="participant",
    within=["modality"]
).fit()

print(aov_rt)


# =====================================================
# ---------------- min TTC ANALYSIS --------------------
# =====================================================
print("\n================ min TTC ANALYSIS ================")

# ---- Filter valid TTC trials ----
df_ttc = df[
    df["modality"].isin(MODALITIES) &
    df["min_TTC"].notna()
].copy()

print("\nTTC trials after filtering:", len(df_ttc))

# ---- Participant × modality counts ----
counts_ttc = (
    df_ttc
    .groupby(["participant", "modality"])
    .size()
    .unstack(fill_value=0)
)

print("\nParticipant × Modality counts (min_TTC):")
print(counts_ttc)

# ---- Keep only balanced participants ----
valid_participants_ttc = counts_ttc[
    (counts_ttc[MODALITIES] > 0).all(axis=1)
].index

df_ttc_balanced = df_ttc[
    df_ttc["participant"].isin(valid_participants_ttc)
].copy()

print("\nParticipants used for min_TTC ANOVA:", valid_participants_ttc.tolist())

# ---- Aggregate (mean per participant × modality) ----
df_ttc_agg = (
    df_ttc_balanced
    .groupby(["participant", "modality"], as_index=False)
    .agg(min_TTC=("min_TTC", "mean"))
)

print("\nAggregated min_TTC data:")
print(df_ttc_agg)

# ---- Run RM-ANOVA (min_TTC) ----
if df_ttc_agg["participant"].nunique() >= 2:
    aov_ttc = AnovaRM(
        data=df_ttc_agg,
        depvar="min_TTC",
        subject="participant",
        within=["modality"]
    ).fit()

    print("\nRepeated-Measures ANOVA — min_TTC")
    print(aov_ttc)
else:
    print("\n❌ Not enough participants for min_TTC RM-ANOVA.")

# =====================================================
# DONE
# =====================================================
print("\nAnalysis complete.")
