"""Rewrite StrafeJumpSim --pose-keys-evasion rows into the --pose-keys column layout.

EvasionPose.WriteKeys writes ... Drop, ElbowYawL, ElbowYawR, HeadYaw, Bank, Seat,
ThighRollL, ThighRollR. ingame_noclip.load_keys reads columns 26..29 as
shoulderL, thRollL, thRollR, hipDrop, so reading the evasion file directly put
HeadYaw on the shoulder and ThighRollL (degrees) into the hips-bone drop (metres).
Output: ... Drop, ElbowYawL, ElbowYawR, shoulderL=0, thRollL, thRollR, hipDrop=0.
Bank and Seat are 0 on every evasion row (checked); HeadYaw is not applied by
apply_frame either way.
  python3 evasion_keys_to_ingame.py in.tsv out.tsv
"""
import sys

src, dst = sys.argv[1], sys.argv[2]
with open(src) as f, open(dst, "w") as o:
    for line in f:
        if line.startswith("#") or not line.strip():
            o.write(line)
            continue
        p = line.rstrip("\n").split("\t")
        name, nums = p[0], p[1:]
        bank, seat = float(nums[27]), float(nums[28])
        assert abs(bank) < 1e-6 and abs(seat) < 1e-6, (name, nums[0], bank, seat)
        out = nums[:26] + ["0.000", nums[29], nums[30], "0.000"]
        o.write("\t".join([name] + out) + "\n")
