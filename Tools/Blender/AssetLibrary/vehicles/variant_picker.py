"""Seeded variant picker for the midsize sedan line.

Pure data. Nothing here is imported by gameplay code. A street spawner
can call pick(seed) and instance the named prefab.
"""

import random


# Five model years on one shell. 2025 is the fascia change.
# Extra colors are the 2025 body only. Names stay generic.
VARIANTS = (
    {"name": "Sedan_Mid_A_21", "year": 2021, "color": "crimson", "fascia": "separate", "wheel": "five_spoke_18"},
    {"name": "Sedan_Mid_A_22", "year": 2022, "color": "crimson", "fascia": "separate", "wheel": "ten_spoke_18"},
    {"name": "Sedan_Mid_A_23", "year": 2023, "color": "crimson", "fascia": "tier", "wheel": "seven_spoke_18"},
    {"name": "Sedan_Mid_A_24", "year": 2024, "color": "crimson", "fascia": "thin", "wheel": "twelve_spoke_18"},
    {"name": "Sedan_Mid_A_25", "year": 2025, "color": "crimson", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_White", "year": 2025, "color": "white", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_Black", "year": 2025, "color": "black", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_Grey", "year": 2025, "color": "grey", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_Silver", "year": 2025, "color": "silver", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_Navy", "year": 2025, "color": "navy", "fascia": "bar", "wheel": "fifteen_spoke_18"},
    {"name": "Sedan_Mid_A_25_Ocean", "year": 2025, "color": "ocean", "fascia": "bar", "wheel": "fifteen_spoke_18"},
)


def pick(seed):
    """Return one variant dict. The same seed always returns the same car."""
    rng = random.Random(int(seed))
    return dict(rng.choice(VARIANTS))


def lineup(n, seed):
    """n distinct variants, stable for this seed. Falls back to a repeat only if n is huge."""
    rng = random.Random(int(seed))
    rows = list(VARIANTS)
    rng.shuffle(rows)
    if n >= len(rows):
        return [dict(row) for row in rows]
    return [dict(row) for row in rows[:n]]
