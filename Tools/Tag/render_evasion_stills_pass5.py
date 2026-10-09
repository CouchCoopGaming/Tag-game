"""Pass 5 evasion stills. Does not write into pass1-pass4.

  POSE_KEYS=Docs/EvasionStills/pass5/pose_keys.tsv blender --background --python Tools/Tag/render_evasion_stills_pass5.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import render_evasion_stills_pass4 as stills

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
stills.OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass5")
stills.KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass5", "pose_keys.tsv"))
stills.CLIPS = ("stutter", "dive")


def main():
    stills.main()


if __name__ == "__main__":
    main()
