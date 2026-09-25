# Campus smoke

After pull: **CutArenaBootstrap** Rebuild, then **PgkLandmarkPlacer -> Place**. Walk on foot. Spines and the crash cross stay empty. Soft-play and the army bunker use `PGK_Slide_TubeDeck_2m` on the 2.00 deck. Astro keeps the straight chute. `Mega_SlideTube` and `PGK_Slide_Tube90` are not placed. Feel was not edited.

TubeDeck axis blocker: Place smoke seats soft-play + army (count=2), but local mesh AABB is still spanY~1.14 (minY/maxY ~+/-0.57) instead of feet~0 / crown~1.91. Brief wants +Y up and high mouth at mesh y=1.91. Do not invent a placer rotation hack. Needs AD FBX re-export (Blender +Y up, pivot mulch under low mouth). Mega/Tube90 stay out.

## SW

Spawn_SW faces northeast. Mushroom steps at (4.5, 6.94) sit beside that exit. Hopscotch SW is the court. The bench at (9.26, 9) is west of the climb net; the passage east of the net, toward the south bar, stays open. No arch here.

## West

South bar into soft-play. **SoftS south** at (14.5, 2.6): balance beam, mushrooms, spring, two hops. **West dome and slide** are west-only. The dome is local (-4.66, -6.70), feet seated, top about 1.31, south of the tubes (world x 7.95-10.73, z 1.66-4.44). The ground slide is local (5.90, -5.53): low mouth on the mulch, high end about 2.41, east of the tube cap. The 2.00 deck chute is `PGK_Slide_TubeDeck_2m`, yaw 90, stem 0, low mouth at local z=5.90. The tube street stays open. Cross the south spine. **Merry-north** at (7.2, 31.2): mushrooms, spring, two hops, 0.56 m west of the bar.

## NW

Cross the north spine. **NW arch** at (7.70, 42). Piers are about 1.7 m off hopscotch NW and the north bar. The deck is at 1.05 and the span underneath is open. NW cluster at (9.4, 45.0), then hopscotch NW. Astro carries the same dome north of its tubes, off Spawn_NW's exit. Its 2.00 chute stays the straight slide. West forts do not have the east spiral.

## East

Spawn_SE, hopscotch SE, then the **SE arch** at (65.90, 10.5). **SE cluster** at (66.2, 7.5): mushrooms, spring, two hops. **East spiral** on army and knight, local (2.80, 0): feet on the ground, top 2.40, a climb onto the 2.00 deck. Army's 2.00 chute is `PGK_Slide_TubeDeck_2m`, yaw 90, stem 0, low mouth at local z=5.90, 1.49 m off that spiral. Knight keeps the straight chute. Kickball's field stays open. **NE arch** at (65.65, 41), bench at (65.65, 41.90). NE cluster at (66.2, 45.0). **NE hops and seesaw**: hop east edge x=68.45, seesaw west edge x=69.01, gap 0.56 m. Then hopscotch NE. The arch span under the bench stays open.

## Crash

Cross the bowl east-west on z 24-30. Both lips and the middle are open lawn.

Pass: that order is walkable, the named pieces are where listed, and the three arches still clear courts and bars by at least 1.3 m.
