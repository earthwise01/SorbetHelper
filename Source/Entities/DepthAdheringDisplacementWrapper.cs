using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Components;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DepthAdheringDisplacementWrapper")]
[Tracked]
public class DepthAdheringDisplacementWrapper : Entity {
    private readonly bool distortBehind;

    public DepthAdheringDisplacementWrapper(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        distortBehind = data.Bool("distortBehind");
        // todo: maybe add an option to affect entities based on their SIDs/type names instead of their colliders?
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        foreach (DisplacementRenderHook component in CollideAllByComponentWithNullHitboxFallback<DisplacementRenderHook>()) {
            component.Entity.Add(new DepthAdheringDisplacementRenderHook(component.Entity.Render, component.RenderDisplacement, distortBehind));
            component.RemoveSelf();
        }

        RemoveSelf();
    }

    private List<T> CollideAllByComponentWithNullHitboxFallback<T>() where T : Component {
        List<T> result = [];

        foreach (Component item in Scene.Tracker.Components[typeof(T)]) {
            Entity entity = item.Entity;
            Collider entityCollider = entity.Collider;

            // if the entity doesn't have a collider (since waterfalls unfortunately don't while also being one of the main things you'd want to affect)
            // try and make a fallback collider so that it's still possible to give them depth adhering displacement
            if (entityCollider is null) {
                float tempWidth = 8f;
                float tempHeight = 8f;

                // if the entity has private fields called "width" or "height" try to use those for the temporary hitbox dimensions instead of the default of 8px
                Type entityType = entity.GetType();
                FieldInfo widthInfo = entityType.GetField("width", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo heightInfo = entityType.GetField("height", BindingFlags.NonPublic | BindingFlags.Instance);

                if (widthInfo?.GetValue(entity) is float width)
                    tempWidth = width;
                if (heightInfo?.GetValue(entity) is float height)
                    tempHeight = height;

                entityCollider = new Hitbox(tempWidth, tempHeight, entity.X, entity.Y);
            }

            if (entityCollider.Collide(this))
                result.Add(item as T);
        }

        return result;
    }
}
