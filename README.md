# Sorbet Helper

celeste helper mod mostly featuring whatever random stuff i happen to make

download: https://gamebanana.com/mods/344736

## **Entities**

### Strawberry (With Node Based Return)
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

### Displacement Wrapper
makes the displacement effect from stuff like water or waterfalls either only affect stuff behind them or nothing else at all.<br>
can be useful for if you want to have fg decals in front of water not get distorted or have waterfalls not distort everything behind them.<br>
(for reference vanilla displacement doesn't care about depth and just affects everything touching it.)

note that for some specific entities, mainly waterfalls, due to Reasons™ (specifically entity load order stuff) this can only affect any placed down *before* itself, so if you have trouble getting it to work try placing the wrapper again.

### Displacement Effect Blocker
blocks the displacement effect produced by entities such as water or waterfalls.<br>
optionally also works with entitites affected by a depth adhering displacement wrapper.

### Winged Strawberry Direction Controller
allows changing the direction any winged strawberries in the room will fly!

### Crumble On Flag Block
similar to a vanilla crumble wall on rumble except it breaks when a flag is enabled and will also reappear if the flag is disabled again.<br>
has an optional toggle to reverse flag activation behavior.

### Flag Toggled Killbox
same as a vanilla killbox but with the added ability to able be flag-toggleable.

### Kill Zone
kills Madeline on contact, optionally flag-toggleable.

## Special Thanks <sub>(aka the people whose code i stole)</sub>
**maddie480**: code + tooltips for the customisable rainbow effect and settings from rainbow spinner color controllers, which was stolen for rainbow lightbeams<br>
**catapillie/communal helper team**: code for the dash knockback effect from station movers, which was stolen for dash falling blocks<br>
**cruor/vexatos (ahorn/lönn team)**: code + tooltips which were stolen for like 99% of the lönn and ahorn plugins
