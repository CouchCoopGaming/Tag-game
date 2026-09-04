# Dummy_Runner / Dummy_It drop (Engineer path)

## Locked names
- `Dummy_Runner.prefab`
- `Dummy_It.prefab`

## Mat slots (exact)
1. **Base** — body vinyl
2. **Accent** — teal band/stripes (Runner) or black chevrons (It)
3. **ItOverride** — emissive rim slot (Engineer drives intensity); reserved on Runner

## Mesh FBX
- `Dummy_Runner.fbx` — armature + skinned mesh, slots Base/Accent/ItOverride
- `Dummy_It.fbx` — same hierarchy, It paint

## Materials (URP)
- `Mat_Runner_Base` / `Mat_Runner_Accent` / `Mat_Runner_ItOverride`
- `Mat_It_Base` / `Mat_It_Accent` / `Mat_It_ItOverride`

## Prefab wiring (Unity)
1. Import FBX (Humanoid or Generic, -Z forward).
2. Create prefab named exactly `Dummy_Runner` / `Dummy_It`.
3. Assign the three materials to mesh slots in order.
4. Attach existing gameplay comps (PlayerMotor, ItController, CharacterController, etc.) — keep capsule controller size until collider art pass.
5. Trail uses `Mat_Trail_Cyan` + `Trail_Ribbon.fbx` separately.

## Paint lock
| | Base | Accent |
|--|--|--|
| Runner | `#E8D9C0` | `#2BB3A3` one chest band + limb stripes |
| It | `#FF6A00` | black downward chevrons chest front + outer thighs |

Sensors: 2 eye dots + temple row of 3. No visor.


## Engineer bind (Hub)
1. Menu **Tag → Setup Dummy Prefabs From FBX** (fills prefab stubs with skinned mesh + mats).
2. On Player / DummyRunner root: add `DummyAvatarBinder`, assign `Dummy_Runner` + `Dummy_It` prefabs.
3. Keep CharacterController on root; kill/hide capsule mesh renderer.
4. Trail: `Mat_Trail_Cyan` + existing PlayerTrailEmitter.
