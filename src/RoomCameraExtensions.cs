using System.Collections.Generic;
using UnityEngine;

namespace OffscreenIndicators {
	static class RoomCameraExtensions {
		static List<ShortcutMinimap> shortcutMinimaps = new ();

		public static void ChangeRoom(RoomCamera self, Room newRoom, int cameraPosition) {
			foreach (var shortcutMinimap in shortcutMinimaps) {
				shortcutMinimap.RemoveSprites();
			}

			shortcutMinimaps.Clear();
		}
		
		public static void Update(RoomCamera self) {
			foreach (var shortcutMinimap in shortcutMinimaps) {
				shortcutMinimap.Update(self);
			}
		}

		public static void DrawUpdate(RoomCamera self, float timeStacker, float timeSpeed) {
			if (self.room == null) {
				return;
			}

			if (Options.radarMode.Value != "disabled") {
				if (shortcutMinimaps.Count == 0 && self.room.shortCutsReady) {
					for (int i = 0; i < self.room.shortcuts.Length; i++) {
						if (self.room.shortcuts[i].shortCutType == ShortcutData.Type.RoomExit) {
							shortcutMinimaps.Add(new ShortcutMinimap(i, self.room, self.ReturnFContainer("Bloom")));
						}
					}
				}
			}

			foreach (var shortcutMinimap in shortcutMinimaps) {
				shortcutMinimap.Draw(self, timeStacker, timeSpeed);
			}
		}

		public static Vector2 ApplyDepth(this RoomCamera self, Vector3 pos) {
			return self.ApplyDepth(pos, pos.z);
		}

		public static bool IsPointInView(this RoomCamera self, Vector2 pos, float margin = 0) {
			Vector2 screenSize = self.game.rainWorld.options.ScreenSize;
			pos -= self.pos;

			return pos.x > margin && pos.y > margin && pos.x < screenSize.x - margin && pos.y < screenSize.y - margin;
		}
	}
}