## MOTEX: A helmet-integrated HMI delivering proactive directional cues
Motorcyclists are among the most vulnerable road users, frequently hindered by late detection of hazards in complex urban traffic. We evaluated MOTEX, a helmet-integrated HMI delivering proactive directional cues through peripheral visual, auditory, and multimodal feedback. In a within-subject simulator study (N=13) in 16 scenarios derived from common accident typologies, no statistically significant effects were found for reaction time (p=0.33) or safety margins (p=0.42). However, visual and multimodal cues yielded numerically faster responses (574ms; SD=317 and 570ms; SD=360) compared to the control (747ms; SD=445). Approximately 70% of the participants reported increased situation awareness, alongside low cognitive workload and moderate trust - suggesting the system functions as a supportive "second pair of eyes" for spatial confirmation rather than prescriptive warning. These findings demonstrate the feasibility of non-intrusive, decoupled helmet-integrated safety systems as modular retrofits bridging human perception and automated hazard detection. 

## Citation and usage of code
If you use this work for academic work please cite the following paper:

> Van Heuvelen, T, & Bazilinskyy, P. (2026). A second pair of eyes: Evaluating a helmet-integrated multimodal HMI for proactive urban motorcyclist safety. Under review. Available at https://bazilinskyy.github.io/publications/vanheuvelen2026second

## Structure
* anova.py: statistical analysis code (ANOVA and post-hoc tests).
* rt_analysis.py: response time analysis code.
* visualisation_event_centric.py: visualisation code for event-centric figures. Extra analysis not in the paper.
* visualisation_modality.py: visualisation code for modality comparison figures. Extra analysis not in the paper.
* visualisation_rt_ttc.py: visualisation code for response time and time-to-collision figures.
* visualisation_survey.py: visualisation code for survey responses.
* arduino.ino: Arduino code.
* unity/: Unity project for the virtual environment.

## Notes
- Find anonymised user data in the supplementary material of the paper.
- Unity version: 2022.3.51f1.
- Python version used for analysis: 3.11+.
- Required Python packages are listed in requirements.txt
- One participant (P08) was excluded due to incomplete data collection.
- TTC values above 10 seconds and invalid values were filtered during preprocessing.

## Contact
For questions regarding the dataset or implementation, please contact the authors.
