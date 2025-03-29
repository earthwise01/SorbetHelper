using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using FMOD.Studio;

namespace Celeste.Mod.SorbetHelper.Entities;

// (fmod docs, for 2.00 but still seem to carry over for the version celeste uses)
// https://www.fmod.com/docs/2.00/api/studio-guide.html#event-callbacks
// https://www.fmod.com/docs/2.00/api/studio-api-eventdescription.html#studio_eventdescription_setcallback
// https://www.fmod.com/docs/2.00/unity/examples-timeline-callbacks.html
// (old communal helper branch i found later that helped a bit)
// https://github.com/CommunalHelper/CommunalHelper/tree/music-synced-entities

// still dont know if ill go for this methodd but it seems to also "work" i think
[GlobalEntity] // hm is this even necessary .   idk its probably useful though
[CustomEntity("SorbetHelper/MusicSyncControllerFMOD")]
[Tracked]
public class MusicSyncControllerFMOD : Entity {
    private const string sessionPrefix = "musicSync_";

    private readonly bool showDebugUI;

    public MusicSyncControllerFMOD(EntityData data, Vector2 _) {
        if (Visible = showDebugUI = data.Bool("showDebugUI", false))
            Tag |= TagsExt.SubHUD;

        Tag |= Tags.TransitionUpdate;

        Depth = 1;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

    }

    public override void Update() {
        // make sure that the music has the callback if necessary
        UpdateMusicSyncEvent();

        base.Update();

        // set flags/counters
        var session = (Scene as Level).Session;
        sessionTimelineInfo = fmodTimelineInfo;
        session.SetCounter(sessionPrefix + "bar", sessionTimelineInfo.Bar);
        session.SetCounter(sessionPrefix + "beat", sessionTimelineInfo.Beat);

        var sorbetSession = SorbetHelperModule.Session;
        if (sessionTimelineInfo.Marker != sorbetSession.CurrentMusicSyncMarker) {
            if (!string.IsNullOrEmpty(sessionTimelineInfo.Marker))
                session.SetFlag(sessionPrefix + sessionTimelineInfo.Marker, true);
            if (!string.IsNullOrEmpty(sorbetSession.CurrentMusicSyncMarker))
                session.SetFlag(sessionPrefix + sorbetSession.CurrentMusicSyncMarker, false);

            sorbetSession.CurrentMusicSyncMarker = sessionTimelineInfo.Marker;
        }
    }

    private struct TimelineInfo {
        public int Bar, Beat;
        public float Tempo;
        public float TimeSigUpper, TimeSigLower;
        public FMOD.StringWrapper Marker;
        public int MarkerPosition;
    }

    private TimelineInfo sessionTimelineInfo;
    private static TimelineInfo fmodTimelineInfo;

    // these next 2 methods are all run on the audio thread
    // this means these are called: during freezeframes, when paused, *during lag*, etc
    // (just prepare the values here and actually update stuff in the entity's update method though)
    private static void HandleBeat(TIMELINE_BEAT_PROPERTIES parameters) {
        fmodTimelineInfo.Bar = parameters.bar;
        fmodTimelineInfo.Beat = parameters.beat;
        fmodTimelineInfo.Tempo = parameters.tempo;
        fmodTimelineInfo.TimeSigUpper = parameters.timesignatureupper;
        fmodTimelineInfo.TimeSigLower = parameters.timesignaturelower;
        // this was a test but yeaa anything of this sort is risky here huh since e.g. this randomly crashed because the beat happened to line up with when the displacement was being rendered
        // i love multithreading (lie)
        // (Scene as Level)?.Displacement.AddBurst(Scene.Tracker.GetEntity<Player>()?.Position ?? Vector2.One, 0.2f, 4f, 32f);
    }

    private static void HandleMarker(TIMELINE_MARKER_PROPERTIES parameters) {
        fmodTimelineInfo.Marker.nativeUtf8Ptr = parameters.name;
        fmodTimelineInfo.MarkerPosition = parameters.position;
    }

    private static void ResetTimelineInfo() {
        fmodTimelineInfo.Bar = 0;
        fmodTimelineInfo.Beat = 0;
        fmodTimelineInfo.Tempo = 0;
        fmodTimelineInfo.TimeSigUpper = 0;
        fmodTimelineInfo.TimeSigLower = 0;
        fmodTimelineInfo.Marker.nativeUtf8Ptr = nint.Zero;
        fmodTimelineInfo.MarkerPosition = 0;
    }

    private static EventInstance musicSyncEvent = null;
    private static bool CanAffectEvent(HashSet<string> eventNames, string eventName) => eventNames.Count == 0 || eventNames.Contains(eventName);
    private static void UpdateMusicSyncEvent() {
        var eventInstance = Audio.CurrentMusicEventInstance;
        var eventPath = Audio.CurrentMusic;

        if (eventInstance != musicSyncEvent) {
            musicSyncEvent?.setCallback(null, 0u);

            HashSet<string> eventNames = null;
            var areaKey = SaveData.Instance?.CurrentSession_Safe?.Area; // hm is there a better way to get this
            var hasController = areaKey.HasValue && SorbetHelperMapDataProcessor.MusicSyncEvents.TryGetValue((areaKey.Value.ID, areaKey.Value.Mode), out eventNames);

            ResetTimelineInfo();

            if (hasController && CanAffectEvent(eventNames, eventPath))
                (musicSyncEvent = eventInstance)?.setCallback(MusicCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER | EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
            else
                musicSyncEvent = null;
        }
    }

    private static bool On_Audio_SetMusic(On.Celeste.Audio.orig_SetMusic orig, string path, bool startPlaying, bool allowFadeOut) {
        var result = orig(path, startPlaying, allowFadeOut);
        UpdateMusicSyncEvent();
        return result;
    }

    private static FMOD.RESULT MusicCallback(EVENT_CALLBACK_TYPE callbackType, nint instancePtr, nint parametersPtr) {
        switch (callbackType) {
            case EVENT_CALLBACK_TYPE.TIMELINE_BEAT: {
                    var parameters = Marshal.PtrToStructure<TIMELINE_BEAT_PROPERTIES>(parametersPtr);
                    HandleBeat(parameters);
                }
                break;
            case EVENT_CALLBACK_TYPE.TIMELINE_MARKER: {
                    var parameters = Marshal.PtrToStructure<TIMELINE_MARKER_PROPERTIES>(parametersPtr);
                    HandleMarker(parameters);
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
        musicSyncEvent?.setCallback(null, 0u);
        musicSyncEvent = null;
    }

    public override void Render() {
        base.Render();

        if (!showDebugUI)
            return;

        var currentEvent = Audio.CurrentMusic;
        var areaKey = (Scene as Level).Session.Area;
        var hasController = SorbetHelperMapDataProcessor.MusicSyncEvents.TryGetValue((areaKey.ID, areaKey.Mode), out var events);

        if (!hasController || !CanAffectEvent(events, currentEvent))
            return;

        var debugText = currentEvent + "\n";

        // tempo marker & beat/bar
        if (sessionTimelineInfo.Beat != 0) {
            // time sig
            debugText += $"{sessionTimelineInfo.TimeSigUpper}/{sessionTimelineInfo.TimeSigLower} {sessionTimelineInfo.Tempo}bpm" + '\n';
            // beat & bar
            debugText += $"Bar {sessionTimelineInfo.Bar}" + '\n';
            debugText += $"Beat {sessionTimelineInfo.Beat}" + '\n';
        } else {
            debugText += "\n\n\n";
        }

        // marker
        var markerString = (string)sessionTimelineInfo.Marker;
        if (!string.IsNullOrEmpty(markerString))
            debugText += $"Marker \"{markerString}\" @ {sessionTimelineInfo.MarkerPosition}ms";

        ActiveFont.DrawOutline(debugText, new Vector2(96, 1020), new Vector2(0f, 1f), Vector2.One, Color.White, 2f, Color.Black);
    }
}