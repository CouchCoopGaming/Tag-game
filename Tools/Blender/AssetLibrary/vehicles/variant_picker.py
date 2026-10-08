"""Seeded variant picker for the midsize sedan line.

Pure data. Nothing here is imported by gameplay code. A street spawner
can call pick(seed) and instance the named prefab.
"""

import random


# year is the sheet the fascia follows. color is the factory-palette slot.
# Names stay generic. The mapping to the inspiration sheet is in the PR table.
VARIANTS = (
    {"name": "Sedan_Mid_A", "year": 2025, "color": "navy", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_White", "year": 2025, "color": "white", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_Black", "year": 2025, "color": "black", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_Grey", "year": 2025, "color": "grey", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_Silver", "year": 2025, "color": "silver", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_Red", "year": 2025, "color": "red", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_Ocean", "year": 2025, "color": "ocean", "fascia": "bar", "wheel": "ten_spoke_16"},
    {"name": "Sedan_Mid_A_21", "year": 2021, "color": "silver", "fascia": "split", "wheel": "five_spoke_17"},
    {"name": "Sedan_Mid_A_21_White", "year": 2021, "color": "white", "fascia": "split", "wheel": "five_spoke_17"},
    {"name": "Sedan_Mid_A_21_Black", "year": 2021, "color": "black", "fascia": "split", "wheel": "five_spoke_17"},
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
