using System;
using BepInEx.Logging;

namespace OffscreenIndicators {
	internal static class Mod {
		internal static ManualLogSource Logger;
		private static bool initialized;

		public static void Initialize(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
			orig(self);

			if (initialized) {
				return;
			}
			initialized = true;

			Logger.LogInfo("Hooking to game state changes events..");

			On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom_HK;
			On.RoomCamera.Update += RoomCamera_Update_HK;
			On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate_HK;

			// Game state change events
			On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud_HK;
			On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud_HK;

			MachineConnector.SetRegisteredOI(OffscreenIndicatorsPlugin.GUID, Options.instance);
		}

		static void RoomCamera_ChangeRoom_HK(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition) {
			orig(self, newRoom, cameraPosition);
			RoomCameraExtensions.ChangeRoom(self, newRoom, cameraPosition);
		}

		static void RoomCamera_Update_HK(On.RoomCamera.orig_Update orig, RoomCamera self) {
			orig(self);
			RoomCameraExtensions.Update(self);
		}

		static void RoomCamera_DrawUpdate_HK(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed) {
			orig(self, timeStacker, timeSpeed);
			RoomCameraExtensions.DrawUpdate(self, timeStacker, timeSpeed);
		}

		static void HUD_InitSinglePlayerHud_HK(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam) {
			self.AddPart(new OffscreenIndicators(self, self.fContainers[0]));
			orig(self, cam);
		}

		static void HUD_InitMultiplayerHud_HK(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session) {
			self.AddPart(new OffscreenIndicators(self, self.fContainers[0]));
			orig(self, session);
		}

	}
}
