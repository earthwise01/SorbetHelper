using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using FMOD.Studio;
using System.Runtime.InteropServices;

namespace Celeste.Mod.SorbetHelper.Entities;

// (fmod docs, for 2.00 but still seem to carry over for the version celeste uses)
// https://www.fmod.com/docs/2.00/api/studio-guide.html#event-callbacks
// https://www.fmod.com/docs/2.00/api/studio-api-eventdescription.html#studio_eventdescription_setcallback
// https://www.fmod.com/docs/2.00/unity/examples-timeline-callbacks.html
// (old communal helper branch i found later that helped a bit)
// https://github.com/CommunalHelper/CommunalHelper/tree/music-synced-entities

// still dont know if ill go for this methodd but it seems to also "work" i think
[GlobalEntity]
[CustomEntity("SorbetHelper/MusicSyncControllerFMOD")]
[Tracked]
public class MusicSyncControllerFMOD : Entity {
    private readonly HashSet<string> eventNames;
    private readonly string sessionPrefix;
    private readonly bool showDebugUI;

    public MusicSyncControllerFMOD(EntityData data, Vector2 _) {
        eventNames = data.List("eventNames", str => str).ToHashSet();

        sessionPrefix = data.Attr("sessionPrefix", "musicSync");

        if (showDebugUI = data.Bool("showDebugUI", false))
            Tag |= TagsExt.SubHUD;

        Tag |= Tags.TransitionUpdate;
        Depth = -1;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        // make sure that the music has the callback
        SetAffectedEvent(Audio.CurrentMusicEventInstance);
    }

    public override void Update() {
        base.Update();

        // set flags/counters
        var session = (Scene as Level).Session;
        session.SetCounter(sessionPrefix + "_bar", fmodTimelineInfo.Bar);
        session.SetCounter(sessionPrefix + "_beat", fmodTimelineInfo.Beat);

        if (fmodTimelineInfo.Marker != previousMarker) {
            session.SetFlag(sessionPrefix + "_" + fmodTimelineInfo.Marker, true);
            session.SetFlag(sessionPrefix + "_" + previousMarker, false);

            previousMarker = fmodTimelineInfo.Marker;
        }
    }

    private struct TimelineInfo {
        public int Bar, Beat;
        public float Tempo;
        public float TimeSigUpper, TimeSigLower;
        public FMOD.StringWrapper Marker;
    }

    private string previousMarker;
    private TimelineInfo fmodTimelineInfo;

    // these are all run on the audio thread
    // this means these are called: during freezeframes, when paused, *during lag*, etc
    // (just prepare the values here and actually update the session in update though)
    private void HandleBeat(TIMELINE_BEAT_PROPERTIES parameters) {
        fmodTimelineInfo.Bar = parameters.bar;
        fmodTimelineInfo.Beat = parameters.beat;
        fmodTimelineInfo.Tempo = parameters.tempo;
        fmodTimelineInfo.TimeSigUpper = parameters.timesignatureupper;
        fmodTimelineInfo.TimeSigLower = parameters.timesignaturelower;
        // this was a test but yeaa anything of this sort is risky here huh since e.g. this randomly crashed because the beat happened to line up with when the displacement was being rendered
        // i love multithreading (lie)
        // (Scene as Level)?.Displacement.AddBurst(Scene.Tracker.GetEntity<Player>()?.Position ?? Vector2.One, 0.2f, 4f, 32f);
    }

    private void HandleMarker(TIMELINE_MARKER_PROPERTIES parameters) {
        fmodTimelineInfo.Marker.nativeUtf8Ptr = parameters.name;
    }

    private void HandleEventChanged(EventInstance previous, EventInstance current) {
        if (previous is not null) {
            fmodTimelineInfo.Bar = 0;
            fmodTimelineInfo.Beat = 0;
            fmodTimelineInfo.Tempo = 0;
            fmodTimelineInfo.TimeSigUpper = 4;
            fmodTimelineInfo.TimeSigLower = 4;
            fmodTimelineInfo.Marker.nativeUtf8Ptr = nint.Zero;
        }
    }

    private static EventInstance affectedEvent = null;
    private static void SetAffectedEvent(EventInstance eventInstance) {
        if (eventInstance != affectedEvent) {
            affectedEvent?.setCallback(null, 0u);

            if (Engine.Scene?.Tracker.GetEntity<MusicSyncControllerFMOD>() is { } controller) {
                controller.HandleEventChanged(affectedEvent, eventInstance);
                (affectedEvent = eventInstance)?.setCallback(callback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER | EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
            } else {
                affectedEvent = null;
            }
        }
    }

    private static bool On_Audio_SetMusic(On.Celeste.Audio.orig_SetMusic orig, string path, bool startPlaying, bool allowFadeOut) {
        var result = orig(path, startPlaying, allowFadeOut);
        SetAffectedEvent(Audio.currentMusicEvent);
        return result;
    }

    private static readonly EVENT_CALLBACK callback = MusicCallback;
    private static unsafe FMOD.RESULT MusicCallback(EVENT_CALLBACK_TYPE callbackType, nint instancePtr, nint parametersPtr) {
        if (Engine.Scene?.Tracker.GetEntity<MusicSyncControllerFMOD>() is not { } controller)
            return FMOD.RESULT.OK;

        switch (callbackType) {
            case EVENT_CALLBACK_TYPE.TIMELINE_BEAT: {
                    var parameters = Marshal.PtrToStructure<TIMELINE_BEAT_PROPERTIES>(parametersPtr);
                    controller.HandleBeat(parameters);
                }
                break;
            case EVENT_CALLBACK_TYPE.TIMELINE_MARKER: {
                    var parameters = Marshal.PtrToStructure<TIMELINE_MARKER_PROPERTIES>(parametersPtr);
                    controller.HandleMarker(parameters);
                }
                break;
        }

        return FMOD.RESULT.OK;
    }

    internal static void Load() {
        On.Celeste.Audio.SetMusic += On_Audio_SetMusic;
    }

    internal static void Unload() {
        On.Celeste.Audio.SetMusic -= On_Audio_SetMusic;

        // make sure the callback is gone before unloading (i don't know if this does much but   aa native code & multithreading scary)
        affectedEvent?.setCallback(null, 0u);
        affectedEvent = null;
    }

    public override void Render() {
        base.Render();

        // im so good at coding
        var currentEvent = Audio.CurrentMusic;
        if (!showDebugUI || (eventNames.Count != 0 && !eventNames.Contains(currentEvent)))
            return;

        var debugText = currentEvent;

        ActiveFont.DrawOutline(debugText, new Vector2(96, 1020), new Vector2(0f, 1f), Vector2.One, Color.White, 2f, Color.Black);
    }
}