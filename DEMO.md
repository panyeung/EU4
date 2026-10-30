# M2 Play Demo

## Start

Open `Assets/Scenes/FoundationScene.unity` in Unity 6000.6.6f1 and press **Play**. Use the mouse to select a destination, then choose **Confirm Travel** or **Cancel**. In a town menu, choose **Return to Map**.

## Try

1. Select Atsuta and inspect the travel time and arrival date, then cancel. The date and current location should stay the same.
2. Select Atsuta again and confirm. The date advances one day and the town menu opens.
3. Return to the map and confirm Atsuta is marked as the current location.
4. Select Nagoya from Atsuta and confirm the direct route is unavailable.
5. From Atsuta, select Kiyosu and inspect its one-day preview, then cancel. The date and location should remain unchanged.

## What's new

M2 adds the Kiyosu, Atsuta, and Nagoya world map, route previews, one-day travel, current-location feedback, and a town menu. The three town facilities appear as locked placeholders.

## Verification

Unity 6000.6.6f1 compiled the project. All 73 EditMode tests passed. The Editor Console showed 0 errors and 0 warnings after testing and the Play Mode walkthrough. Play Mode verified Kiyosu → Atsuta travel, the arrival town menu, return to map, a valid travel cancellation, and the unavailable Atsuta → Nagoya route.

## Known limits

Town facilities, characters, missions, and save/load are not part of M2. The Phase 1 roadmap schedules those for later milestones.

## Next

After this demo is accepted, continue with M3 characters and relationships. The demo candidate is on `feature/m2-world-map`; the final commit hash is in the handoff.
