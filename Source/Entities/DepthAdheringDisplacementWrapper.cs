using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Components;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/DepthAdheringDisplacementWrapper")]
    [Tracked]
    public class DepthAdheringDisplacementWrapper : Entity {
        private readonly bool distortBehind;
        private readonly bool onlyCollideTopLeft;

        public DepthAdheringDisplacementWrapper(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Collider = new Hitbox(data.Width, data.Height);
            distortBehind = data.Bool("distortBehind");
            onlyCollideTopLeft = data.Bool("onlyCollideTopLeft");
            Depth = Depths.Top;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            foreach (DisplacementRenderHook component in CollideAllByComponentNullHitboxBackup<DisplacementRenderHook>()) {
                component.Entity.Add(new DepthAdheringDisplacementRenderHook(component.Entity.Render, component.RenderDisplacement, distortBehind));
                component.RemoveSelf();
            }

            RemoveSelf();
        }

        /*
            modified version of the normal CollideAllByCompenent which attempts to construct a temporary hitbox if an entity is missing a collider.
            since waterfalls don't have colliders normally i need to do this as otherwise it'd be impossible to give
            waterfalls depth adhering displacement which is. hopefully obvious why that'd be a Not Great idea.
        */
        private List<T> CollideAllByComponentNullHitboxBackup<T>() where T : Component {
            List<T> list = [];

            foreach (Component item in Scene.Tracker.Components[typeof(T)]) {
                Entity entity = item.Entity;
                Collider colliderBackup = entity.Collider;
                if (entity.Collider == null || onlyCollideTopLeft) {
                    float tempWidth = 8f;
                    float tempHeight = 8f;

                    if (!onlyCollideTopLeft) {
                        // if the entity has fields called "width" or "height" try to use those for the temporary hitbox instead of the default of 8f
                        Type entityType = entity.GetType();
                        // Logger.Log(LogLevel.Verbose, "SorbetHelper/DepthAdheringDisplacementWrapper", $"making backup hitbox for {entityType}");
                        FieldInfo widthInfo = entityType.GetField("width", BindingFlags.NonPublic | BindingFlags.Instance);
                        FieldInfo heightInfo = entityType.GetField("height", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (widthInfo != null && widthInfo.GetValue(entity) != null)
                            tempWidth = (float)widthInfo.GetValue(entity);
                        if (heightInfo != null && heightInfo.GetValue(entity) != null)
                            tempHeight = (float)heightInfo.GetValue(entity);
                    }

                    entity.Collider = new Hitbox(tempWidth, tempHeight);
                }

                if (Collide.Check(this, entity)) {
                    list.Add(item as T);
                }
                entity.Collider = colliderBackup;
            }

            return list;
        }
    }
}
