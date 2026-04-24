# Sorbet Helper

my celeste helper mod containing whatever i feel like adding

download is available on gamebanana (https://gamebanana.com/mods/344736)

for any bugs/suggestions, pls feel free to either ping `@limimimia` in [celestecord](https://discord.com/invite/celeste) (preferred) or create an issue on this repo, ty &lt;3

## entities

- Strawberry (With Noded Return)
    - strawberry variant which puts madeline into a bubble and moves her along a curved path defined by its nodes when grabbed. emits bubble particles when idle

- Custom Lightbeam
    - highly customisable lightbeam. also has a rainbow variant

- Resizable Waterfall
    - highly customisable and resizable waterfall. can be configured to look both more like a normal waterfall and a big fg waterfall

- Sparkling Water
    - variation of water with a different custom aesthetic

- Sparkling Water Color Controller
    - used for configuring the appearance of sparkling water entities

- Custom Falling Block
    - highly customisable falling block

- Dash Falling Block
    - falling block variant which falls upon being dashed into rather than when being grabbed or stood on

- Custom Gravity Falling Block
    - highly customisable version of gravity falling blocks from [Chrono Helper](https://gamebanana.com/mods/351933)
    - requires chrono helper

- Gravity Dash Falling Block
    - dash falling block that also functions as a chrono helper gravity falling block
    - requires chrono helper

- Dash Gate Block
    - kevin like block that instead moves like a switch gate when dashed into

- Touch Gate Block
    - move block like block that instead moves like a switch gate when stood on/grabbed

- Color Dash Block
    - cassette block/color switch block like entity which switches state when dashed into

- Crumble On Flag Block
    - block which crumbles when a flag is set. like a crumble wall on rumble that uses flags instead of rumble triggers

- Displacement Effect Area
    - creates a simple displacement effect in a specified area

- Displacement Effect Blocker
    - blocks the wavy displacement effect from water/waterfalls/etc and optionally the bur

- Displacement Depth Fixer
    - makes the displacement effect from stuff like water or waterfalls not distort things that render above them. can be useful if you want to have fg decals in front of water, put resizable waterfalls in the background, etc
    - also has an option to make the displacement not affect anything other than the entity producing it

- Fix Darkness Transparency Controller
    - fixes a weird vanilla bug where darkness alpha causes transparent objects like dash trails or lightbeams to fade towards black as they get more transparent

- Tileset Depth Splitter
    - allows changing the visual depth of specified tiletypes, and can automatically fill in any annoying gaps on their edges from using the ignores attribute with their surrounding tiles (no more 50000 million tileblending decals/bgtiles :tada:)

- Styleground Depth Controller
    - makes stylegrounds with a specified tag render at a specified gameplay depth
    - also has options for making stylegrounds render above the colorgrade/hud

- Entity As Styleground Controller
    - allows rendering specified entity types as part of an "entity as styleground renderer" styleground with a matching tag

- Light Cover Controller
    - makes specified entity types cover up any light touching them

- Return Bubble Tweaks Controller
    - fixes a few bugs with and has a few options for configuring the return bubble player state

- Puffer Tweaks Controller
    - has a few options for configuring the behavior of puffers

- FMOD Marker To Flag/Counter Controller
    - sets session flags and counters depending on the destination and tempo markers in the currently playing music event

- Slider Fade X/Y
    - fades a session slider according to the format stylegrounds use for their fade x/y attributes

- Accurate Killbox
    - custom killbox that actually acts how the bottom of a room does (meaning it only kills you once you go fully beneath it). will also be in the correct state from the start instead of always being uncollidable until the room finishes loading
    - flag toggleable (also has a placement that's just flag toggleable without the other changes)

- Kill Zone
    - kills you when you touch it. pretty simple
    - can be toggled by a flag & has an option to affect holdables

## triggers

- Alternate Interact Prompt
    - allows replacing the vanilla interact prompt with a few alternate designs
    - also has an option to make (even vanilla style) interact prompts respond to Up inputs rather than Talk inputs

- Mini Popup Trigger
    - spawns little customizable achievement like popups that slide in from the top right side of the screen

- Color Dash Block State Trigger
    - sets all color dash blocks in the map to a specified state
    - the current state can also be read/changed by other means by using the session counter `SorbetHelper_ColorDashBlockIndex`

## **Styleground Effects**

- Parallax Hi-Res Snow
    - highly customizable high resolution snow particles

- Hi-Res Godrays
    - customizable high resolution godrays

- Spiral Stars
    - variation of the falling stars effect from old site that instead spirals inwards towards the center like a blackhole

- Entity As Styleground Renderer
    - renders entities and decals affected by the entity as styleground controller and `sorbetHelper_styleground` decal registry attribute respectively

## **Decal Registry**

- `sorbetHelper_lightCover`
    - makes decals cover up any light touching them
    - differs from the everest `lightOcclude` attribute in that it covers up light using the decal's sprite, rather than blocks light using a specified rectangle
    - has the following attributes:
        - `alpha(float)` - the strength of the light cover (defaults to 1.0)
        - `maximumDepth(int)` - the maximum (farthest from the camera) depth a decal can have while being affected (defaults to -1 to only affect decals above madeline)
        - `minimumDepth(int)` - the minimum (closest to the camera) depth a decal can have while being affected (defaults to uncapped)
    - examples:
      ```xml
      <sorbetHelper_lightCover/>
      <sorbetHelper_lightCover alpha="0.7"/>
      <sorbetHelper_lightCover maximumDepth="100"/>
      ```
    - also available in controller form as the light cover controller

- `sorbetHelper_styleground`
    - allows decals to be drawn as part of a styleground layer
    - has the following attributes:
        - **(required)** `rendererTag(string)` - a styleground tag used to match with an entity as styleground renderer styleground in the map
        - `maximumDepth(int)` - the maximum (farthest from the camera) depth a decal can have while being affected (defaults to -1 to only affect decals above madeline)
        - `minimumDepth(int)` - the minimum (closest to the camera) depth a decal can have while being affected (defaults to uncapped)
    - will not work correctly if either the `rendererTag` variable or matching renderer itself is missing!
    - examples:
      ```xml
      <sorbetHelper_styleground rendererTag="clouds_bg"/>
      <sorbetHelper_styleground rendererTag="decals_fg" maximumDepth="-1000"/>
      ```
