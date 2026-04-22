import pandas as pd
import matplotlib.pyplot as plt
from pathlib import Path

# =========================
# CONFIG
# =========================
file_path = Path("data_survey.csv")  # <-- update filename
participant_id_col = "Participant Test Number"

likert_order = [
    "Strongly disagree",
    "Disagree",
    "Neutral",
    "Agree",
    "Strongly agree"
]

likert_map = {
    "Strongly Disagree": "Strongly disagree",
    "strongly disagree": "Strongly disagree",
    "Disagree": "Disagree",
    "disagree": "Disagree",
    "Neutral": "Neutral",
    "neutral": "Neutral",
    "Neither disagree or agree": "Neutral",
    "Neither agree or disagree": "Neutral",
    "Agree": "Agree",
    "agree": "Agree",
    "Strongly Agree": "Strongly agree",
    "strongly agree": "Strongly agree",
}

# =========================
# LOAD DATA (CSV VERSION)
# =========================
df = pd.read_csv(
    file_path,
    encoding="utf-8",     # try "latin1" if this fails
    sep=",",              # try ";" if your CSV uses semicolons
    engine="python"
)

df.columns = (
    df.columns
    .str.replace("\xa0", " ", regex=False)
    .str.strip()
)

print("Columns found:")
for c in df.columns:
    print("-", repr(c))

# Exclude participant who dropped out
if participant_id_col in df.columns:
    df = df[df[participant_id_col] != "P08"]

# =========================
# HELPER
# =========================
def normalize_text(s: str) -> str:
    return " ".join(str(s).replace("\xa0", " ").split()).lower()

def find_col_contains(*keywords):
    normalized_cols = {c: normalize_text(c) for c in df.columns}
    normalized_keywords = [normalize_text(k) for k in keywords]

    for c, c_norm in normalized_cols.items():
        if all(k in c_norm for k in normalized_keywords):
            return c
    return None

def clean_likert(series: pd.Series) -> pd.Series:
    s = series.astype(str).str.strip()
    s = s.replace(likert_map)
    s = s.where(s.isin(likert_order))
    return s

# =========================
# SELECT QUESTIONS
# =========================
question_cols = {
    "a) MOTEX increased my\nawareness of surroundings.": find_col_contains("motex", "aware", "surroundings"),
    "b) The feedback was clear\nwithin the context.": find_col_contains("feedback", "clear", "context"),
    "c) The alerts appeared early\nenough to take action.": find_col_contains("alerts", "early enough", "take action"),
    "d) The alerts felt overwhelming.": find_col_contains("overloaded", "alerts"),
}

print("\nMatched columns:")
for title, col in question_cols.items():
    print(f"{title} -> {repr(col)}")

missing = [title for title, col in question_cols.items() if col is None]
if missing:
    raise ValueError(
        "Could not match these questions:\n- " + "\n- ".join(missing) +
        "\n\nCheck printed column names and adjust keywords."
    )

# Clean responses
for col in question_cols.values():
    df[col] = clean_likert(df[col])

# =========================
# PRINT SUMMARY STATS
# =========================
print("\nKEY METRICS FOR PAPER:")
for title, col in question_cols.items():
    valid = df[col].dropna()
    n = len(valid)
    counts = valid.value_counts().reindex(likert_order, fill_value=0)
    positive_n = counts["Agree"] + counts["Strongly agree"]
    neutral_n = counts["Neutral"]
    negative_n = counts["Disagree"] + counts["Strongly disagree"]

    print(
        f"{title.replace(chr(10), ' ')}\n"
        f"  positive = {positive_n}/{n} ({positive_n/n*100:.1f}%)\n"
        f"  neutral  = {neutral_n}/{n} ({neutral_n/n*100:.1f}%)\n"
        f"  negative = {negative_n}/{n} ({negative_n/n*100:.1f}%)\n"
    )

# =========================
# PLOT (unchanged)
# =========================
fig, axes = plt.subplots(2, 2, figsize=(12, 5))
axes = axes.flatten()

flat_titles = {
    "a) MOTEX increased my awareness of surroundings.": question_cols[
        "a) MOTEX increased my\nawareness of surroundings."
    ],
    "b) The feedback was clear within the context.": question_cols[
        "b) The feedback was clear\nwithin the context."
    ],
    "c) The alerts appeared early enough to take action.": question_cols[
        "c) The alerts appeared early\nenough to take action."
    ],
    "d) The alerts felt overwhelming.": question_cols[
        "d) The alerts felt overwhelming."
    ],
}

y_max = 0
for col in flat_titles.values():
    valid = df[col].dropna()
    counts = valid.value_counts().reindex(likert_order, fill_value=0)
    y_max = max(y_max, counts.max())

color_gradient = [
    "#deebf7",
    "#9ecae1",
    "#6baed6",
    "#3182bd",
    "#08519c"
]

for ax, (title, col) in zip(axes, flat_titles.items()):
    valid = df[col].dropna()
    counts = valid.value_counts().reindex(likert_order, fill_value=0)

    ax.bar(range(len(likert_order)), counts.values, color=color_gradient)

    ax.set_xticks(range(len(likert_order)))
    ax.set_xticklabels(likert_order, rotation=30, ha="right")
    ax.set_ylabel("Count")
    ax.set_ylim(0, y_max + 1)

    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)

    for i, v in enumerate(counts.values):
        ax.text(i, v + 0.05, str(v), ha="center", fontsize=9)

    ax.set_title(title, fontsize=11, loc="left", pad=6)

plt.tight_layout(pad=1.0)
plt.savefig("questionnaire_collage_clean.png", dpi=300, bbox_inches="tight")
plt.show()
