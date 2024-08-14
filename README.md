# Sorbet Helper

celeste helper mod mostly featuring whatever random stuff i happen to make

download: https://gamebanana.com/mods/344736

if you find any bugs or have any feature requests pls feel free to either ping @earthwise_ on the [Mt. Celeste Climbing Assosiation](https://discord.com/invite/celeste) discord server or create an issue on this repo!

## **Entities**

### Strawberry (With Noded Return)
same as a regular strawberry, but upon grabbing one Madeline is placed into a bubble and moved along a path (similarly to a vanilla key with return).<br>
can also have wings or seeds and a customisable delay between grabbing the berry and being bubbled.

(if you'd rather if the bubble took Madeline to the latest spawn point no matter what instead of a specifc location, i'd recommend you check out [Lunatic Helper](https://gamebanana.com/mods/53692)'s return strawberries instead.)

### Custom/Rainbow Lightbeam
a customisable version of a lightbeam.<br>
settings include toggling the lightbeam on and off with a flag, changing the color, toggling the fade out on room transitions or when approaching them, and making the lightbeam rainbow!

### Resizable Waterfall
a custom waterfall with the appearance/resizablity of a vanilla big waterfall and the behavior of a vanilla small waterfall.<br>
has options for toggling visual detail lines (disabling this will make it appear visually more like a small waterfall), making it ignore any solids in its way, and customising visual depth.

### Dash Falling Block
a falling block which falls upon being dashed into rather than when being grabbed or stood on.<br>
has options to customize its visual depth and SFX, direction, along with toggles to allow activating it in the same ways as vanilla falling blocks as well.

### Dash/Touch Gate Blocks
a pair of entities which function similarly to switch gates, but are instead activated by dashing into them or standing on/grabbing them respectively.<br>
has a decent amount of options, such as setting a flag upon activation, making them able to go back and forth, changing which sides the blocks can be activated from, etc.

### Stylegrounds Above HUD Controller
allows stylegrounds with the `sorbetHelper_drawAboveHud` tag to render above the hud!<br>
only one controller is required per map, and has options for how affected stylegrounds should behave when the game is paused.<br>
<sup>(note that unfortunately above hud stylegrounds aren't affected by colorgrades atm due to Technical Limitations.)</sup>

### Displacement Depth Fixer
makes the displacement effect from stuff like water or waterfalls either only affect stuff behind them or nothing else at all.<br>
can be useful for if you want to have fg decals in front of water not get distorted or have waterfalls not distort everything behind them.<br>
(for reference vanilla displacement doesn't care about depth and just affects everything touching it.)

### Displacement Effect Blocker
completely blocks the displacement effects produced by various things such as water, waterfalls or madeline dashing.<br>
optionally also works with entities affected by a displacement depth fixer.

### Winged Strawberry Direction Controller
allows changing the direction any winged strawberries in the room will fly!

### Crumble On Flag Block
similar to a vanilla crumble wall on rumble except it breaks when a flag is enabled and will also reappear if the flag is disabled again.<br>
has an optional toggle to reverse flag activation behavior.

### Flag Toggled Killbox
same as a vanilla killbox but with the added ability to able be flag-toggleable.

### Kill Zone
kills Madeline on contact, optionally flag-toggleable.

## **Decal Registry**

### `sorbetHelper_lightCover`
makes decals block/cover any light touching them

has 2 optional attributes, `maximumDepth(int)` and `minimumDepth(int)`, which determine the range of decal depths that are affected, and default to `-1` and `-2147483648`<sup>(the minimum value for an int)</sup> respectively (which makes only decals above madeline get affected since madeline is at depth `0`, and lower depths mean closer to the camera).

e.g. this makes decals with a depth anywhere in the range of `-100` to `-100000` (including `-100` and `-100000`) cover any light touching them
```xml
<decal path="path/to/decal">
    <sorbetHelper_lightCover maximumDepth="-100" minimumDepth="-100000"/>
</decal>
```

differs from the vanilla `lightOcclude` attribute in that it 1. fully covers the decal, rather than an arbitrary rectangle, 2. doesn't "block" light but instead covers it, and, 3. doesn't stop working if a light source goes inside it (such as the player spotlight). <sup>(also 4. its probably decently worse for performance but shhh)</sup>

## Special Thanks
**maddie480**: code + tooltips for the customisable rainbow effect and settings from rainbow spinner color controllers which was stolen for rainbow lightbeams, and code + tooltips from flag switch gates which gate blocks are *heavily* based off<br>
**catapillie/communal helper team**: code for the dash knockback effect from station movers, which was stolen for dash falling blocks and dash gate blocks<br>
**vitellary**: reminding me that gate blocks exisited and making a kevin switch gate retexture which was used as a base for the appearance of dash gate blocks<br>
**cruor/vexatos (ahorn/lönn team)**: code + tooltips which were stolen for a bunch of the lönn and ahorn plugins<br>
