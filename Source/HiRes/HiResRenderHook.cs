using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.SorbetHelper.HiRes;

public class HiResRenderHook : Component {
    public readonly Action RenderHiRes;
    private readonly int depth;

    public HiResRenderHook(Action render, int depth) : base(false, true) {
        RenderHiRes = render;
        this.depth = depth;
    }

    private void TrackSelf(Scene scene) => HiResRenderLayer.GetHiResRenderLayer(scene, depth).Track(this);
    private void UntrackSelf(Scene scene) => HiResRenderLayer.GetHiResRenderLayer(scene, depth).Untrack(this);

    public override void Added(Entity entity) {
        base.Added(entity);

        if (entity.Scene is not null)
            TrackSelf(entity.Scene);
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        TrackSelf(scene);
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        UntrackSelf(entity.Scene);
    }

    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        UntrackSelf(scene);
    }
}

public class BetterAdditiveTestScene : Scene {

    private VirtualRenderTarget Foreground, Background;
    private int seed;
    private float scroll;

    private float yoyoyo;

    private BlendState BetterAdditive = new BlendState() {
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };

    public override void Begin() {
        Foreground = VirtualContent.CreateRenderTarget("betteradditivetest_a", 1920, 1080);
        Background = VirtualContent.CreateRenderTarget("betteradditivetest_b", 1920, 1080);
        base.Begin();
    }

    public override void End() {
        base.End();
        Foreground.Dispose();
        Background.Dispose();
    }

    public override void Update() {
        base.Update();

        scroll += Engine.DeltaTime * 200;

        yoyoyo = Calc.YoYo((scroll / 800) % 1f);

        if (OnInterval(1f))
            seed = Calc.Random.Next();
    }

    public override void Render() {
        Engine.Graphics.GraphicsDevice.SetRenderTarget(Foreground);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BetterAdditive);
        OVR.Atlas["overlay"].Draw(Vector2.Zero, Vector2.Zero, Color.White * yoyoyo * 0.5f);
        Calc.PushRandom(seed);
        for (int i = 0; i < 50; i++) {
            OVR.Atlas["star"].DrawCentered(new Vector2(Calc.Random.Range(0, 1920), Calc.Random.Range(0, 1080)), Color.Blue * Calc.Random.NextFloat(), 1f, Calc.Random.NextAngle());
        }
        Calc.PopRandom();
        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.SetRenderTarget(Background);
        Engine.Graphics.GraphicsDevice.Clear(Color.Gray);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
        GFX.Game["bgs/07/bg0"].Draw(new Vector2((scroll % 1920) - 1920, 0f), Vector2.Zero, Color.White, 6f);
        GFX.Game["bgs/07/bg0"].Draw(new Vector2(scroll % 1920, 0f), Vector2.Zero, Color.White, 6f);
        Draw.SpriteBatch.Draw(Foreground, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.SetRenderTarget(null);
        Engine.Graphics.GraphicsDevice.Clear(Color.Gray);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
        Draw.SpriteBatch.Draw(Background, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }

    [Command("enter_betteradditive_test_scene", "helppp")]
    public static void enterScene() {
        Engine.Scene = new BetterAdditiveTestScene();
    }
}