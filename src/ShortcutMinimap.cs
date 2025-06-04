using System;
using System.Collections.Generic;

using UnityEngine;
using RWCustom;

namespace OffscreenIndicators {
	class ShortcutMinimap {
		const int TILE_SIZE = 20;
		FContainer container;
		Vector2 targetOffset;
		Vector2 pos, lastPos;
		float compact, lastCompact;
		bool initialized;
		
		int index;
		Room room;
		ShortcutData shortcut;
		Room otherRoom;
		ShortcutData otherShortcut;

		float activeTime;
		float warmUp, lastWarmUp;
		float power, lastPower;
		float transition, lastTransition;
		bool hidden;

		FSprite glow = new("Futile_White") {
			shader = RWCustom.Custom.rainWorld.Shaders["FlatLight"]
		};
		
		class Line {
			FSprite sprite = new("pixel") {
				shader = RWCustom.Custom.rainWorld.Shaders["Hologram"],
				anchorY = 0f,
				scaleX = 1,
			};
			public Vector3 start, end;
			Vector3 vstart, vend, lvstart, lvend;
			
			public float alpha;
			float valpha, lvalpha;

			public Line(FContainer container) {
				container.AddChild(sprite);
			}

			public void Update(Vector3 origin, Vector3 source, float power, float warmUp) {
				lvstart = vstart;
				lvend = vend;

				vstart = Vector3.Lerp(source, start + (Vector3)(UnityEngine.Random.insideUnitCircle * (1 - power)) * 5 + origin, Mathf.Pow(power * power, Mathf.Pow(2, UnityEngine.Random.Range(-2f,2f))));
				vend = Vector3.Lerp(source, end + (Vector3)(UnityEngine.Random.insideUnitCircle * (1 - power)) * 5 + origin, Mathf.Pow(power * power, Mathf.Pow(2, UnityEngine.Random.Range(-2f,2f))));
			
				lvalpha = valpha;
				valpha = alpha * power * warmUp;
			}

			public void Draw(RoomCamera camera, Color color, float timeStacker) {
				if (valpha == 0) {
					sprite.isVisible = false;
					return;
				} else {
					sprite.isVisible = true;
				}

				Vector2 start = camera.ApplyDepth(Vector3.Lerp(lvstart, vstart, timeStacker)) - camera.pos;
				Vector2 end = camera.ApplyDepth(Vector3.Lerp(lvend, vend, timeStacker)) - camera.pos;

				sprite.x = start.x;
				sprite.y = start.y;
				sprite.scaleY = Vector2.Distance(start, end);
				sprite.rotation = Custom.AimFromOneVectorToAnother(start, end);
				sprite.color = color;
				sprite.alpha = Mathf.Lerp(lvalpha, valpha, timeStacker);
			}

			public void Remove() {
				sprite.RemoveFromContainer();
			}
		}

		List<Line> lines = new();

		class Blip {
			public Creature creature;
			FSprite dot = new FSprite("pixel") {
				shader = RWCustom.Custom.rainWorld.Shaders["Hologram"],
				scale = 15 * Options.minimapScale.Value,
			};
			FSprite glow = new("Futile_White") {
				shader = RWCustom.Custom.rainWorld.Shaders["FlatLight"]
			};
			FSprite line = new FSprite("pixel") {
				shader = RWCustom.Custom.rainWorld.Shaders["Hologram"],
				anchorY = 0f,
				scaleX = 1,
				color = Color.gray,
				alpha = 0.75f,
			};
			public Vector3 pos, lastPos;
			public float alpha, lastAlpha;
			public Blip(Creature creature, FContainer container) {
				this.creature = creature;
				dot.color = glow.color = OffscreenIndicators.GetCreatureSymbolicColor(creature.abstractCreature);
				
				container.AddChild(line);
				container.AddChild(glow);
				container.AddChild(dot);
			}

			public void Update(Vector2 radarPos, Vector3 origin, Vector3 source, float power, float warmUp) {
			}

			public void Draw(RoomCamera camera, float timeStacker) {
				Vector3 pos = Vector3.Lerp(lastPos, this.pos, timeStacker);
				Vector2 blipPos = camera.ApplyDepth(pos) - camera.pos;

				dot.x = blipPos.x;
				dot.y = blipPos.y;
				dot.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);

				glow.x = blipPos.x;
				glow.y = blipPos.y;
				glow.alpha = dot.alpha * 0.25f;
				glow.scale = (0.5f + 0.5f * dot.alpha) * 10 * Options.minimapScale.Value;

				Vector2 rootPos = camera.ApplyDepth((Vector2)pos, -5) - camera.pos;

				line.x = rootPos.x;
				line.y = rootPos.y;
				line.scaleY = Vector2.Distance(rootPos, blipPos);
				line.rotation = Custom.AimFromOneVectorToAnother(rootPos, blipPos);
				line.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
			}

			public void RemoveSprites() {
				dot.RemoveFromContainer();
				glow.RemoveFromContainer();
				line.RemoveFromContainer();
			}
		}

		ObjectTracker<Creature, Blip> trackedCreatures;

		class HologramRing {
			List<Line> lines = new();
			public Vector3 pos;
			public float radius;
			public float alpha;

			public HologramRing(FContainer container, int points) {
				for (int i = 0; i < points; i++) {
					float rad = 2 * Mathf.PI * i / points;
					float rad2 = 2 * Mathf.PI * (i + 1) / points;

					lines.Add(new Line(container));
				}
			}

			public void Update(Vector3 origin, Vector3 source, float power, float warmUp) {
				for (int i = 0; i < lines.Count; i++) {
					float rad = 2 * Mathf.PI * i / lines.Count;
					float rad2 = 2 * Mathf.PI * (i + 1) / lines.Count;

					Line line = lines[i];
					line.start = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius) + pos;
					line.end = new Vector3(Mathf.Cos(rad2) * radius, Mathf.Sin(rad2) * radius) + pos;
					line.alpha = alpha;

					line.Update(origin, source, power, warmUp);
				}
			}

			public void Draw(RoomCamera camera, Color color, float timeStacker) {
				for (int i = 0; i < lines.Count; i++) {
					Line line = lines[i];
					line.Draw(camera, color, timeStacker);
				}
			}

			public void RemoveSprites() {
				lines.ForEach(x => x.Remove());
			}
		}

		HologramRing mainRing;
		HologramRing mainRing2;
		HologramRing halfRing;

		public ShortcutMinimap(int shortcutIndex, Room room, FContainer container) {
			container.AddChild(glow);

			this.container = container;
			this.index = shortcutIndex;
			this.room = room;
			this.shortcut = room.shortcuts[shortcutIndex];

			float radius = Options.scanTileRange.Value * TILE_SIZE * Options.minimapScale.Value;

			mainRing = new HologramRing(container, 8) {
				pos = new Vector3(0,0,-10),
				radius = radius,
				alpha = 1,
			};

			mainRing2 = new HologramRing(container, 8) {
				pos = new Vector3(0,0,-5),
				radius = radius,
				alpha = 0.75f,
			};

			halfRing = new HologramRing(container, 8) {
				pos = new Vector3(0,0,-5),
				radius = TILE_SIZE,
				alpha = 0.75f,
			};

			PlacedObject placedObj = room.roomSettings.placedObjects.Find(x => this.room.GetTilePosition(x.pos) == shortcut.StartTile);
			hidden = placedObj != null && placedObj.type == PlacedObject.Type.ExitSymbolHidden && placedObj.active;
		}

		void Initialize(Room otherRoom) {
			this.otherRoom = otherRoom;

			if (initialized) // There is a case where the room wants to reinitialize, without this we get fireflies artifact
				return;
			initialized = true;

			otherShortcut = otherRoom.ShortcutLeadingToNode(otherRoom.abstractRoom.ExitIndex(room.abstractRoom.index));
			float rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, IntVector2.ToVector2(otherRoom.ShorcutEntranceHoleDirection(otherShortcut.StartTile)));

			float scale = TILE_SIZE * Options.minimapScale.Value;

			AddConnectedLines(container,
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(1.5f, 1.5f), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(0.5f, 1.5f), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(0.5f, 0), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(0, -0.5f), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(-0.5f, 0), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(-0.5f, 1.5f), rotation) * scale) + new Vector3(0,0,-10),
				(Vector3)(Custom.RotateAroundOrigo(new Vector2(-1.5f, 1.5f), rotation) * scale) + new Vector3(0,0,-10)
			);

			Vector2 otherEntrancePosition = otherRoom.MiddleOfTile(otherShortcut.startCoord);
			
			trackedCreatures = new() {
				OnGetCollection = () => this.otherRoom.abstractRoom.creatures.ConvertAll(x => x.realizedCreature),
				OnExistCheck = x => {
					if (x == null || x.dead || x.inShortcut) {
						return false;
					}

					Vector2 creaturePosition = x.mainBodyChunk.pos;
					float dist = Vector2.Distance(creaturePosition, otherEntrancePosition);
					
					return dist < Options.scanTileRange.Value * TILE_SIZE;
				},
				OnCreate = x => new Blip(x, container),
				OnDestroy = x => x.RemoveSprites(),
			};
		}

		public void Update(RoomCamera camera) {
			Vector2 shortcutOrigin = room.MiddleOfTile(shortcut.startCoord);
			Creature followedCreature = camera?.followAbstractCreature?.realizedCreature;
			//lastTransition = transition;
			
			bool playerNearby = camera.game.Players.Exists(
				x => x.realizedCreature != null && // is physical
				x.realizedCreature is Player && // is slugcat
				(((Player)x.realizedCreature).controller != null || !(((Player)x.realizedCreature).controller is Player.NullController)) && // is controlled
				Vector2.Distance(shortcutOrigin, x.realizedCreature.mainBodyChunk.pos) < 60 // is near
			);

			lastCompact = compact;
			compact = Options.radarMode.Value switch {
				"mini" => 1,
				"expand" => 0,
				_ => room.abstractRoom.shelter || hidden ? 0 : Mathf.MoveTowards(compact, playerNearby ? 0 : 1, 4f / 40f),
			};
			
			float compactSmooth = Mathf.SmoothStep(0, 1, compact);

			Vector2 holeDirection = IntVector2.ToVector2(room.ShorcutEntranceHoleDirection(shortcut.StartTile));
			
			float radius = Options.scanTileRange.Value * TILE_SIZE * Options.minimapScale.Value;

			targetOffset = holeDirection * -radius;
			if (!camera.IsPointInView(shortcutOrigin + targetOffset, radius)) {
				targetOffset = holeDirection * (radius + 4 * TILE_SIZE);
			}

			lastPos = pos;
			pos = shortcutOrigin + Vector2.Lerp(targetOffset, holeDirection * TILE_SIZE, compactSmooth);

			lastWarmUp = warmUp;
			lastPower = power;

			// Force realize room if there are creatures in it, since the shortcut data is not ready to be obtained
			AbstractRoom otherAbstractRoom = room.game.world.GetAbstractRoom(room.abstractRoom.connections[shortcut.destNode]);
			if (trackedCreatures == null) {
				if (otherAbstractRoom == null || otherAbstractRoom.creatures.Count == 0) {
					return;
				}
			}

			if (otherAbstractRoom != null) {
				if (otherAbstractRoom.realizedRoom == null) {
					otherAbstractRoom.RealizeRoom(room.world, room.game);
					otherRoom = null;
				}
				
				if (otherRoom == null) {
					Room otherRoom = otherAbstractRoom.realizedRoom;
					if (otherRoom.shortCutsReady) {
						Initialize(otherRoom);
					}
				}
			} else {
				otherRoom = null;
			}
			
			if (otherRoom == null) {
				return;
			}

			// When the other room connected to this shortcut is loaded
				
			// Display animation
			if ((!hidden || playerNearby) && trackedCreatures.Count > 0) {
				warmUp = Mathf.MoveTowards(warmUp, 1, Time.deltaTime / 1);
				power = Mathf.MoveTowards(power, 1, Time.deltaTime / 0.1f);
			} else {
				warmUp = Mathf.MoveTowards(warmUp, 0, Time.deltaTime / 10);
				power = Mathf.MoveTowards(power, 0, Time.deltaTime / 0.1f);
			}

			trackedCreatures.Update();
			Vector2 otherEntrancePosition = otherRoom.MiddleOfTile(otherShortcut.startCoord);
			
			float netSense = 0;
			float[] senses = new float[trackedCreatures.Count];
			float[] distances = new float[trackedCreatures.Count];
			int i = 0;
			foreach (var tracker in trackedCreatures.GetTrackers()) {
				Vector2 creaturePosition = tracker.creature.mainBodyChunk.pos;
				float dist = Vector2.Distance(creaturePosition, otherEntrancePosition);
				
				float senseRange = Options.scanTileRange.Value * TILE_SIZE; // Max sense range
				// Reduce the range based on certain factors
				//senseRange *= (1 - 0.8f * room.Darkness(creaturePosition)); // Doesn't work if camera is not in same room
				senseRange *= (1 - 0.4f * tracker.creature.Submersion);

				float senseStrength = Mathf.InverseLerp(senseRange, senseRange / 2, dist);
				//spriteAlpha *= Mathf.InverseLerp(1.8f, 0f, player.Submersion); // Not observed by player
				senseStrength *= OffscreenIndicators.GetCreatureVisibilityFactor(tracker.creature);
				senseStrength = Mathf.Min(senseStrength - tracker.creature.Submersion * 0.8f, 1);

				senses[i] = senseStrength;
				distances[i] = dist;
				netSense += senseStrength;
				i++;
			}

			activeTime += 1f / 40 / Math.Max(trackedCreatures.Count, 1);

			float sensePos = 0;
			i = 0;
			foreach (var tracker in trackedCreatures.GetTrackers()) {
				//tracker.Update(otherEntrancePosition, pos, shortcutOrigin, power, warmUp);
				Vector2 creaturePosition = tracker.creature.mainBodyChunk.pos;

				tracker.lastAlpha = tracker.alpha;
				tracker.lastPos = tracker.pos;
				float senseStrength = senses[i];
				
				Vector3 trackedPos = creaturePosition - otherEntrancePosition;
				trackedPos *= Options.minimapScale.Value;
				trackedPos += (UnityEngine.Random.insideUnitSphere * 10) * (1 - senseStrength) + new Vector3(0,0,(1 - senseStrength) * -10 - 10);
				trackedPos += (Vector3)(UnityEngine.Random.insideUnitCircle * (1 - power) * 5);

				float rad = Mathf.Clamp01(sensePos / netSense) * Mathf.PI * 2 + activeTime;
				float senseRadius = 15 * (2 - senseStrength) * Mathf.Min(distances[i] / (4 * TILE_SIZE), 1);

				Vector3 compactOrbitPos = new Vector3(
					Mathf.Cos(rad) * senseRadius,
					Mathf.Sin(rad) * senseRadius,
					-10
				);

				tracker.pos = Vector3.Lerp(shortcutOrigin, Vector3.Lerp(trackedPos, compactOrbitPos, compactSmooth) + (Vector3)pos, Mathf.Pow(power * power, Mathf.Pow(2, UnityEngine.Random.Range(-2f,2f))));
				tracker.alpha = senseStrength * power * warmUp;

				i++;
				sensePos += senseStrength;
			}

			mainRing.radius = Mathf.Lerp(Options.scanTileRange.Value * TILE_SIZE * Options.minimapScale.Value, TILE_SIZE, compactSmooth);
			mainRing2.alpha = Math.Max(1 - compactSmooth * 8, 0) * 0.75f;
			halfRing.alpha = Math.Max(1 - compactSmooth, 0) * 0.75f;

			lines.ForEach(x => x.Update(pos, shortcutOrigin, power, warmUp));
			mainRing.Update(pos, shortcutOrigin, power, warmUp);
			mainRing2.Update(pos, shortcutOrigin, power, warmUp);
			halfRing.Update(pos, shortcutOrigin, power, warmUp);
		}

		public void Draw(RoomCamera camera, float timeStacker, float timeSpeed) {
			float warmUp = Mathf.Lerp(lastWarmUp, this.warmUp, timeStacker);
			float power = Mathf.Lerp(lastPower, this.power, timeStacker);
			float transition = Mathf.Lerp(lastTransition, this.transition, timeStacker);
			float compact = Mathf.Lerp(lastCompact, this.compact, timeStacker);
			float compactSmooth = compact * compact * (3 - 2 * compact);
			
			FSprite[,] entranceSprites = camera.shortcutGraphics.entranceSprites;
			Color entranceColor = 
				index >= entranceSprites.GetLength(0) ||
				entranceSprites[index, 0] == null ? Color.white : entranceSprites[index, 0].color;

			lines.ForEach(x => {
				x.alpha = 1 - compactSmooth;
				x.Draw(camera, entranceColor, timeStacker);
			});
			mainRing.Draw(camera, entranceColor, timeStacker);
			mainRing2.Draw(camera, entranceColor, timeStacker);
			halfRing.Draw(camera, entranceColor, timeStacker);

			if (trackedCreatures != null) {
				foreach (var tracker in trackedCreatures.GetTrackers()) {
					tracker.Draw(camera, timeStacker);
				}
			}

			Vector2 shortcutOrigin = room.MiddleOfTile(shortcut.startCoord);
			Vector2 glowPos = camera.ApplyDepth(Vector2.Lerp(shortcutOrigin, pos, power * power), -5) - camera.pos;
			glow.x = glowPos.x;
			glow.y = glowPos.y;
			glow.scale = Mathf.Lerp(3, Mathf.Lerp(3 * Options.scanTileRange.Value * Options.minimapScale.Value, 5, compactSmooth), power) ;
			glow.alpha = warmUp * power * 0.1f;
		}

		public void RemoveSprites() {
			glow.RemoveFromContainer();
			trackedCreatures?.Cleanup();
			lines.ForEach(x => x.Remove());
			
			mainRing.RemoveSprites();
			mainRing2.RemoveSprites();
			halfRing.RemoveSprites();
		}

		public void AddConnectedLines(FContainer container, params Vector3[] points) {
			for (int i = 0; i < points.Length - 1; i++) {
				lines.Add(new Line(container) {
					start = points[i], 
					end = points[i + 1], 
					alpha = 1
				});
			}
		}
	}
}