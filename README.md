Supplementary Material – MOTEX

This repository contains the supplementary material for the paper:

Heuvelen, T. van & Bazilinskyy, P. (2025).
Evaluating a Helmet-Integrated Multimodal HMI for Proactive Urban Motorcyclist Safety.
Proceedings of the 18th International Conference on Automotive User Interfaces and Interactive Vehicular Applications (AutoUI '26), Gothenburg, Sweden.

--------------------------------------------------
STRUCTURE
--------------------------------------------------

The repository is organised into four main components:

/Arduino Code  
Contains firmware for the physical MOTEX prototype.
- MOTEX_Firmware.ino: Arduino Uno R3 firmware handling:
  - AS5600 magnetic encoder input
  - WS2812B LED output
  - EJEAS-based audio triggering

/Scripts  
Contains analysis and processing scripts used in this study.
- Data analysis scripts (Python)
- Statistical analysis (e.g., RT, TTC, questionnaire aggregation)
- Plot generation scripts (boxplots and distributions used in the paper)

/Unity  
Unity 2022.3 simulation environment used for the experiment.
- 16 hazard scenarios (C1, C2, L2, T7)
- TTC calculation and triggering logic
- NPC behaviour and NavMesh trajectories
- First-person rider interface and feedback integration

/User Test Data  
Contains anonymised participant data.
- Simulator logs (per participant and scenario)
- Extracted behavioural metrics:
  - Reaction Time (RT)
  - Minimum Time-To-Collision (minTTC)
  - Control inputs (steering, braking)
- Questionnaire responses (post-test)
- Demographic overview (age, gender, riding profile)

--------------------------------------------------
EXPERIMENTAL DATA
--------------------------------------------------

Participants:
- N = 14 recruited
- N = 13 included in analysis (1 excluded due to incomplete data)

Data types:
- Behavioural data from simulator logs
- Questionnaire data (Likert-scale + open-ended responses)

All data are anonymised.

--------------------------------------------------
ANALYSIS
--------------------------------------------------

The analysis pipeline includes:
- Extraction of cue onset (TTC ≤ 2.0 s)
- Computation of:
  - RT_primary (braking or steering response)
  - Minimum TTC until reaction
- Statistical analysis:
  - Friedman test (non-parametric repeated measures)
  - Bonferroni-corrected post-hoc comparisons

Plots generated:
- Boxplots of RT and minTTC per modality
- Questionnaire distributions

--------------------------------------------------
HARDWARE (MOTEX PROTOTYPE)
--------------------------------------------------

The physical prototype consists of:
- Arduino Uno R3
- AS5600 magnetic encoder
- WS2812B LED strip (helmet-integrated)
- EJEAS V6 intercom for audio feedback

The system provides:
- Peripheral visual cues
- Spatialised auditory cues
- Multimodal combinations

--------------------------------------------------
NOTES
--------------------------------------------------

- Unity version: 2022.3
- Python version used for analysis: 3.11+
- Required Python packages are listed in requirements.txt

- One participant (P08) was excluded due to incomplete data collection.

- TTC values above 10 seconds and invalid values were filtered during preprocessing.

--------------------------------------------------
CONTACT
--------------------------------------------------

For questions regarding the dataset or implementation, please contact the authors.
