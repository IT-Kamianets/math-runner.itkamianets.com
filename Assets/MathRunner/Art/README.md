# Math Runner starter art

All 32 numbered entries from `models.md`, plus road, sidewalk, lane marking,
answer-board backing, and ground-fill meshes. Generated locally with
`python Tools/generate_starter_models.py`; no external service or paid credits.

- **OBJ + palette.mtl**: native Unity import, flat normals, URP material conversion
  handled by `StarterModelImporter`. Keep the MTL beside the OBJs.
- **GLB**: portable colored meshes, meters, Y up. Unity needs a glTF importer to
  use these directly; use the OBJ versions in this project.
- **catalog.json**: actual triangle counts, budgets, and bounding dimensions.
- Every mesh has a bottom-centered pivot. Buildings share a 6 m frontage.
- Runner variants are 1.8 m tall, static A-pose meshes with separate parts.
  They have no skin weights, skeleton, or animations yet.
- Shield GLB uses transparency; the OBJ is an opaque shape for replacing with
  the game's shield material. Flat colors need no texture files or UV maps.
- Answer boards include the backing only; add TextMeshPro for numbers.

Open Unity to import and generate `.meta` files, then choose
**Math Runner > Art > Create Starter Prefabs** for basic colliders and character
controllers. This overwrites prefabs at those generated paths when run again.
The overhead sign uses separate part colliders so the slide opening stays clear.
Pickup prefabs use trigger colliders; movement, rigidbodies and gameplay scripts
remain the game's responsibility. These are starter assets, not animated finished art.

Contact sheet: `Tools/starter-models-preview.png` (each cell is independently scaled).
