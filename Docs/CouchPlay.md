# Couch play

Local split-screen for two to four humans. The verbs are the ones already in the game. Coyote, jump, cling, dash, punch, lunge, climb, and tag-back are not retuned here.

## Join

Match setup’s Start match opens the join screen. It does not load the round until someone is seated and Start match is confirmed.

| Seat | How |
|---|---|
| Keyboard | One player. A letter, number, Shift, Ctrl, or Alt joins. Esc leaves. |
| Pad | Up to four. Any button joins that pad. East leaves that pad. |

Enter, Space, or South starts once that device is already seated. The Back row returns to match setup and keeps the seats. Humans plus AI stay at or under 4. The AI count from setup fills only the empty seats. Four humans leave no room for AI.

Each seated player is P1–P4, in the order the seats fill. The color is the same blue / amber / purple / white set the verb HUD already uses. A small emissive plate, built like the It hat, sits on the pawn. The It crown stays the It tell.

## Input

Each device drives only its pawn.

The keyboard player keeps `ActionBinds.Current`, including any rebind made in pause. Each pad has its own pad table, copied from the defaults until that pad rebinds. Changing one pad does not write the keyboard table or another pad.

Hints and glyphs in a viewport read that player’s device and that device’s table. A pad that rebound Jump to North shows North. The keyboard still shows Space.

## View

| Humans | Layout |
|---|---|
| 1 | Full screen |
| 2 | Vertical split, or horizontal when the join screen’s Split row says so |
| 3 | Quadrants. The fourth quadrant is the score list |
| 4 | Quadrants |

Each human viewport has its own chase camera. Catch-up is unchanged: field-of-view pop 0, shake 0, slow motion 0, and each camera keeps its own timer. The verb cluster, the It chip, the name, and the timer sit inside that viewport at 1920×1080 and 1280×720.

The listener is on P1, or at the average of the human pawns when Listener says Average. One listener is enabled. The voice cap stays 16.

## Round

Tag, tag-back, and punch stagger stay per pawn. Tag-back immunity is 1.0 s and only closes the pair that just swapped. A stagger is 0.25 s on the pawn that was hit, then 0.50 s of immunity on that same pawn. Another pawn is not locked. The results list names every human and every AI, with time as It and tags.

Each pawn still moves with one `CharacterController.Move` per Update. No rigidbody locomotion. No root motion.

## Proof

`Tools/StrafeJumpSim` prints one `couch` line: join and leave, viewport rects for 1 / 2 vertical / 2 horizontal / 3 / 4 at both sizes, HUD clear, P2’s move leaving P1 still, results listing humans and AI, listener, 16 voices, the chase-cam locks, and teardown leftovers 0.

It also prints `frame-budget-split` for four humans and four cameras. That line is steady, with headroom above 0. The original four-pawn line (one human, three AI) is unchanged.
