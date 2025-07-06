using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;

using BepInEx;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace OffscreenIndicators {
	[BepInPlugin(GUID, MOD_NAME, VERSION)]
	public class Plugin : BaseUnityPlugin {
		public const string VERSION = "1.0.4";
		public const string MOD_NAME = "Offscreen Indicators";
		public const string MOD_ID = "offscreenindicators";
		public const string AUTHOR = "franklygd";
		public const string GUID = AUTHOR + "." + MOD_ID;

		public void OnEnable() {
			Mod.Logger = Logger;
			On.RainWorld.OnModsInit += Mod.Initialize;
		}
	}
}