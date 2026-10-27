# Math Runner: 3D models

Style for every Meshy model: stylized low-poly cartoon, bright saturated colors, clean flat-ish textures, same look as one set. Add this phrase to every prompt: "stylized low-poly cartoon, bright colors, game-ready, clean topology".

Export format: FBX or GLB. Y up, 1 unit = 1 meter, pivot at the bottom center.

## 1. Generate in Meshy

### Character
| # | Model | Notes | Budget |
|---|---|---|---|
| 1 | Runner kid (main character) | Cartoon kid with pigtails, grey sweater, jeans, sneakers. Use auto-rig. A-pose or T-pose. | 15k tris, 2048 tex |
| 2 | Runner skin 2 | Alternate outfit, optional unlock | 15k tris |
| 3 | Runner skin 3 | Alternate outfit, optional unlock | 15k tris |

Animations needed (Meshy animation library or Mixamo): run, jump, slide, stumble/hit, idle, celebrate, lane-switch lean (optional).

### Street buildings (modular, placed on both sides of the track)
| # | Model | Notes | Budget |
|---|---|---|---|
| 4 | Shop with awning, red | Ground-floor shop, 2 upper floors | 5k tris |
| 5 | Shop with awning, teal | Same footprint as #4 | 5k tris |
| 6 | Shop with awning, yellow | Same footprint | 5k tris |
| 7 | Apartment block, tall | 5 to 6 floors, balconies | 5k tris |
| 8 | Apartment block, wide | Rooftop details | 5k tris |
| 9 | Corner building | Used to break up repetition | 5k tris |
| 10 | Skyline building, far | Very simple, for the distance | 1.5k tris |
| 11 | Skyline building, far 2 | Very simple | 1.5k tris |

All buildings must share one footprint width so they tile along the street. Check this after generating.

### Street props
| # | Model | Budget |
|---|---|---|
| 12 | Street lamp | 800 tris |
| 13 | Tree, round | 1.5k tris |
| 14 | Tree, small / bush | 800 tris |
| 15 | Bench (also used as a low obstacle) | 800 tris |
| 16 | Trash bin | 500 tris |
| 17 | Mailbox | 500 tris |
| 18 | Bus stop shelter | 1.5k tris |
| 19 | Planter with flowers | 600 tris |
| 20 | Parked car, small | 2k tris |
| 21 | Parked bus (background) | 2.5k tris |
| 22 | Hydrant | 400 tris |

### Obstacles (placed on the track)
| # | Model | Player action | Budget |
|---|---|---|---|
| 23 | Road barrier (striped) | Switch lane | 600 tris |
| 24 | Traffic cone cluster | Switch lane | 500 tris |
| 25 | Crate stack | Switch lane | 800 tris |
| 26 | Low fence / low bench | Jump over | 600 tris |
| 27 | Overhead beam / hanging sign | Slide under | 800 tris |

### Pickups and power-ups
| # | Model | Notes | Budget |
|---|---|---|---|
| 28 | Coin | Gold, thick, readable at distance | 300 tris |
| 29 | Slow-motion clock | Matches the "Slow Speed" power-up | 800 tris |
| 30 | Magnet | Matches the "Power Coin" power-up | 800 tris |
| 31 | Shield bubble | Can also be made as a shader | 500 tris |
| 32 | Extra life heart | Optional | 500 tris |

## 2. Build ourselves in Unity

| Asset | How |
|---|---|
| Road segment | Flat plane, tiling asphalt texture, exact length so segments connect |
| Sidewalk and curb | Cubes with a tiling texture, set as a prefab |
| Lane markings | Textured quad or decal |
| Answer boards (the cyan number panels) | Rounded-rect quad plus TextMeshPro 3D text |
| Floating question text | TextMeshPro, no mesh |
| Skybox | Procedural gradient or a free HDRI |
| Ground fill beyond the sidewalk | Plane with a tint |
| Fog | URP fog setting for the horizon fade |
| Shadow blob under the player | Projected quad |
| Particles | Unity VFX: coin burst, dust, speed lines, confetti, hit stars |
| Shield bubble effect | Shader or transparent sphere (alternative to #31) |
| All UI | uGUI |

## 3. Suggested Meshy prompts

Replace `<style>` with the style phrase at the top.

- **Runner kid:** "cute cartoon girl running character, brown hair in two pigtails, grey sweater, blue jeans, sneakers, <style>, A-pose".
- **Shop building:** "small town shop building with striped awning and large windows, three floors, red and white, <style>".
- **Barrier:** "orange and white striped road barrier, <style>".
- **Coin:** "large gold coin with a star emblem, <style>".

## 4. Priority

1. **Must have for the first playable:** #1 character, #23 to #27 obstacles, #28 coin, and our own road, sidewalk and answer boards.
2. **Needed for the look:** #4 to #9 buildings, #12 to #14 props.
3. **Later:** #2, #3 skins, #10, #11, #15 to #22 extra dressing, #29 to #32 power-ups.

## 5. Checks after each import

- Scale is correct against the 1.8 m runner, with pivot at the bottom center.
- Buildings share one footprint width.
- Triangle counts are within budget.
- Materials are converted to URP Lit (or a simple unlit-style shader for mobile).
- Collider is set up on obstacles and pickups.
