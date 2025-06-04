using System.Collections.Generic;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace OffscreenIndicators {
	class Options : OptionInterface {
		public enum TunnelRadarMode {
			Dynamic,
			Minimal,
			Expanded
		}

		public static Options instance = new Options();

		public static Configurable<string> offscreenDisplayType = instance.config.Bind("offscreenDisplayType", "pulse", new ConfigurableInfo(
			"The type of display that off-screen creatures would be represented as.",
			tags: "Offscreen Indicator Display Mode"));
		
		public static Configurable<bool> showOffscreenThrownItems = instance.config.Bind("showOffscreenThrownItems", true, new ConfigurableInfo(
			"Whether or not to show a quick sharp glow indicating an item is thrown offscreen, regardless if it is away or towards. Yellow is non-lethal. Red is lethal.",
			tags: "Show Thrown Items Offscreen"));
		
		public static Configurable<string> radarMode = instance.config.Bind("radarMode", "default", new ConfigurableInfo(
			"Set how shortcut radars will display the information."
			+ " (Expanded) Shows locations of creatures in range, relative to the shortcut exit."
			+ " (Minimal) Shows creatures in range of the shortcut exit and how close they are to entering it."
			+ " (Dynamic) Changes between Expanded and Minimal modes depending if you are near one.",
			tags: "Radar Mode"));

		public static Configurable<int> scanTileRange = instance.config.Bind("scanTileRange", 16, new ConfigurableInfo(
			"How many tiles far the shortcut radar can sense other creatures nearby.", new ConfigAcceptableRange<int>(2, 64),
			tags: "Shortcut Radar Range (Tiles)"));
		
		public static Configurable<float> minimapScale = instance.config.Bind("minimapScale", 0.2f, new ConfigurableInfo(
			"How big the radar display will be compared to the real scale of this section of the map.", new ConfigAcceptableRange<float>(0.01f, 1f),
			tags: "Shortcut Minimap Scale (Factor)"));

		public override void Initialize() {
			base.Initialize();

			Debug.Log("Initializing Config...");

			Tabs = new OpTab[]{ new OpTab(this, "Options") };

			Vector2 position = new Vector2(50, 600);

			List<UIelement> elements = new List<UIelement>();

			position.y -= 40;
			elements.Add(new OpComboBox(offscreenDisplayType, position, 90, new List<ListItem>{
				new ListItem("hidden", "Hidden"),
				new ListItem("pulse", "Pulse"),
				new ListItem("icon", "Icon")}
			) {description = offscreenDisplayType.info.description});
			elements.Add(new OpLabel(position.x + 100, position.y + 3, offscreenDisplayType.info.Tags[0] as string) {description = offscreenDisplayType.info.description});

			position.y -= 40;
			elements.Add(new OpCheckBox(showOffscreenThrownItems, position) {description = showOffscreenThrownItems.info.description});
			elements.Add(new OpLabel(position.x + 40, position.y + 3, showOffscreenThrownItems.info.Tags[0] as string) {description = showOffscreenThrownItems.info.description});

			position.y -= 40;
			elements.Add(new OpComboBox(radarMode, position, 90, new List<ListItem>{
				new ListItem("disabled", "Disabled"),
				new ListItem("default", "Dynamic"),
				new ListItem("expand", "Expanded"),
				new ListItem("mini", "Minimal")}
			) {description = radarMode.info.description});
			elements.Add(new OpLabel(position.x + 100, position.y + 3, radarMode.info.Tags[0] as string) {description = radarMode.info.description});

			position.y -= 40;
			elements.Add(new OpUpdown(scanTileRange, position, 70) {description = scanTileRange.info.description});
			elements.Add(new OpLabel(position.x + 80, position.y + 3, scanTileRange.info.Tags[0] as string) {description = scanTileRange.info.description});

			position.y -= 40;
			elements.Add( new OpUpdown(minimapScale, position, 70) {description = minimapScale.info.description});
			elements.Add(new OpLabel(position.x + 80, position.y + 3, minimapScale.info.Tags[0] as string) {description = minimapScale.info.description});

			for (int i = elements.Count - 1; i >= 0 ; i--) {			
				Tabs[0]._AddItem(elements[i]);
			}
		}
	}
}
