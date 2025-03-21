using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity]
[CustomEntity("SorbetHelper/MusicSyncController")]

// i   give up on the fmod api .  callbacks?? no thanks sorry im just going to do this myself i cant rly notice any real difference anyway
public class MusicSyncController : Entity {
    public readonly record struct TimelineMarker(string Name, int Position, int EndPosition);
    public readonly record struct TempoMarker(float Tempo, int TimeSigUpper, int TimeSigLower, int Position);

    private readonly string eventName;
    private readonly HashSet<TempoMarker> tempoMarkers = [];
    private readonly HashSet<TimelineMarker> timelineMarkers = [];

    private readonly string sessionPrefix;

    private readonly bool showDebugUI;

    public MusicSyncController(EntityData data, Vector2 _) {
        eventName = data.Attr("eventName");

        var tempoMarkersRaw = data.Attr("tempoMarkers", "120-4-4-0");
        foreach (var raw in tempoMarkersRaw.Split(','))
            if (ParseTempoMarker(raw, out var marker))
                tempoMarkers.Add(marker);

        var markersRaw = data.Attr("markers");
        foreach (var raw in markersRaw.Split(','))
            if (ParseTimelineMarker(raw, out var marker))
                timelineMarkers.Add(marker);

        sessionPrefix = data.Attr("sessionPrefix", "musicSync");

        if (showDebugUI = data.Bool("showDebugUI", false))
            Tag |= TagsExt.SubHUD;

        Tag |= Tags.TransitionUpdate;
        Depth = -1;
    }

    // format: tempo-timeSigUpper-timeSigLower-position
    private static bool ParseTempoMarker(string raw, out TempoMarker marker) {
        marker = default;

        var split = raw.Split('-');
        if (split.Length < 4)
            return false;

        if (!float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var tempo))
            return false;

        if (!int.TryParse(split[1], out var timeSigUpper))
            return false;
        if (!int.TryParse(split[2], out var timeSigLower))
            return false;

        if (!int.TryParse(split[3], out var position))
            return false;

        marker = new TempoMarker(tempo, timeSigUpper, timeSigLower, position);

        return true;
    }

    // format: name-position
    private static bool ParseTimelineMarker(string raw, out TimelineMarker marker) {
        marker = default;

        var split = raw.Split('-');
        if (split.Length < 2)
            return false;

        var name = split[0];
        if (string.IsNullOrEmpty(name))
            return false;

        if (!int.TryParse(split[1], out var position))
            return false;

        if (split.Length >= 3 && int.TryParse(split[2], out var endPosition))
            marker = new TimelineMarker(name, position, endPosition);
        else
            marker = new TimelineMarker(name, position, -1);

        return true;
    }

    private int currentBar, currentBeat, currentTimelinePos;
    private TempoMarker? currentTempoMarker;
    private TimelineMarker? currentTimelineMarker;

    private void UpdateValues() {
        if (Scene is not Level level)
            return;

        // reset stuff if the event isn't playing
        var music = Audio.CurrentMusicEventInstance;
        if (music is null || (!string.IsNullOrEmpty(eventName) && Audio.CurrentMusic != eventName)) {
            currentTimelinePos = 0;
            currentBar = currentBeat = 0;
            currentTempoMarker = null;
            currentTimelineMarker = null;
        // otherwise update the markers using the timeline position
        } else {
            // get timeline position
            music.getTimelinePosition(out currentTimelinePos);

            // get tempo marker (null means no marker)
            currentTempoMarker = null;
            foreach (var marker in tempoMarkers)
                if (currentTimelinePos >= marker.Position && currentTempoMarker.GetValueOrDefault().Position <= marker.Position)
                    currentTempoMarker = marker;

            // get beat/bar
            if (currentTempoMarker is { } tempoMarker) {
                var beat = (int)MathF.Floor(currentTimelinePos / (60f / (tempoMarker.Tempo * tempoMarker.TimeSigLower / 4f) * 1000f));
                currentBar = 1 + beat / tempoMarker.TimeSigUpper;
                currentBeat = 1 + beat % tempoMarker.TimeSigUpper;
            } else {
                currentBar = currentBeat = 0;
            }

            // get marker (null means no marker)
            currentTimelineMarker = null;
            foreach (var marker in timelineMarkers)       // don't replace the current marker with one earlier  // only care about the end position if it's greater than the start position
                if (currentTimelinePos >= marker.Position && currentTimelineMarker.GetValueOrDefault().Position <= marker.Position && (marker.EndPosition <= marker.Position || currentTimelinePos < marker.EndPosition))
                    currentTimelineMarker = marker;
        }

        // set flags/counters
        var session = level.Session;
        session.SetCounter(sessionPrefix + "_bar", currentBar);
        session.SetCounter(sessionPrefix + "_beat", currentBeat);
        session.SetCounter(sessionPrefix + "_timeline", currentTimelinePos);

        var currentMarkerFlag = currentTimelineMarker.HasValue ? sessionPrefix + "_" + currentTimelineMarker.Value.Name : null;
        foreach (var marker in timelineMarkers) {
            var flagName = sessionPrefix + "_" + marker.Name;
            var isCurrentFlag = flagName == currentMarkerFlag;

            if (session.GetFlag(flagName) != isCurrentFlag)
                session.SetFlag(flagName, isCurrentFlag);
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        UpdateValues();
    }

    public override void Update() {
        base.Update();
        UpdateValues();
    }

    public override void Render() {
        base.Render();

        // im so good at coding
        if (!showDebugUI || (!string.IsNullOrEmpty(eventName) && Audio.CurrentMusic != eventName))
            return;

        var debugText =  $"{(string.IsNullOrEmpty(eventName) ? "Music Sync" : eventName)}\n";

        // tempo
        if (currentTempoMarker is { } tempoMarker)
            debugText += $"""
                          {tempoMarker.TimeSigUpper}/{tempoMarker.TimeSigLower} {tempoMarker.Tempo}bpm
                          Bar {currentBar}
                          Beat {currentBeat}

                          """;
        else
            debugText += "\n\n\n";

        // marker
        if (currentTimelineMarker is { } marker)
            debugText += $"Marker {marker.Name} @ {marker.Position}ms\n";
        else
            debugText += "Marker n/a\n";

        debugText += $"Timeline {currentTimelinePos}ms";


        ActiveFont.DrawOutline(debugText, new Vector2(96, 1020), new Vector2(0f, 1f), Vector2.One, Color.White, 2f, Color.Black);
    }
}