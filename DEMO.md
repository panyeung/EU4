# M2 Play Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Start

Open `Assets/Scenes/FoundationScene.unity` in Unity 6000.6.6f1 and press **Play**. Use the mouse to select a destination (清洲, 热田, or 那古野), then click **确认出发** to travel or **取消** to return to the map. In a town, click **返回地图**.

## Try

1. Select Atsuta and inspect the travel time and arrival date, then cancel. The date and current location should stay the same.
2. Select Atsuta again and confirm. The date advances one day and the town screen opens.
3. Check that the town screen shows no world-map travel controls underneath it. Return to the map and confirm Atsuta is marked as the current location.
4. Select Nagoya from Atsuta and confirm the direct route is unavailable.
5. From Atsuta, select Kiyosu and inspect its one-day preview, then cancel. The date and location should remain unchanged.

## What's new

The M2 map and travel controls now sit in a separate lower panel. Entering a town hides the map-only controls and shows an opaque town panel, so the two screens no longer overlap. The three town facilities remain locked placeholders.

## Verification

Unity 6000.6.6f1 compiled the updated project. All 73 EditMode tests passed with 0 failures. The Editor Console showed 0 errors after the updated tests and Play Mode walkthrough. Play Mode verified Kiyosu-to-Atsuta travel, the town screen without map controls beneath it, return to map, cancellation, and the unavailable Atsuta-to-Nagoya route.

## Known limits

Town facilities, characters, missions, and save/load are not part of M2. The Phase 1 roadmap schedules those for later milestones.

## Next

- Branch: `feature/m2-ui-overlap-fix`
- Overlap fix commit: `9c2ac69`

After this demo is accepted, merge and tag M2, then continue with M3 characters and relationships.
