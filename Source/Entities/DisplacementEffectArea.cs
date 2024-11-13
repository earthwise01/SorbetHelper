using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Celeste.Mod.SorbetHelper.Components;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/DisplacementEffectArea")]
    public class DisplacementEffectArea : Entity {
        private readonly float horizontalDisplacement, verticalDisplacement;
        private readonly float waterDisplacement;
        private readonly float alpha;

        public DisplacementEffectArea(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);

            horizontalDisplacement = 1f - (data.Float("horizontalDisplacement", 0.0f) + 1f) / 2f; // remap the values from the "more friendly" -1 to 1 range to the actual expected 0 to 1 range (where 0.5 is no displacement)
            verticalDisplacement = 1f - (data.Float("verticalDisplacement", 0.0f) + 1f) / 2f;
            waterDisplacement = data.Float("waterDisplacement", 0.25f);
            alpha = data.Float("alpha", 1.0f);

            if (data.Bool("depthAdhering", false)) {
                Depth = data.Int("depth", 0);
                Add(new DepthAdheringDisplacementRenderHook(() => {}, RenderDisplacement, true));
            } else {
                Add(new DisplacementRenderHook(RenderDisplacement));
            }
        }

        public void RenderDisplacement() {
            var color = new Color(horizontalDisplacement, verticalDisplacement, waterDisplacement) * alpha;

            Draw.Rect(Position, Width, Height, color);
        }
    }
}
