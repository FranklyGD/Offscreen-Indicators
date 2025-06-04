using System.Collections.Generic;

using UnityEngine;
using RWCustom;
using MoreSlugcats;
using HUD;

namespace OffscreenIndicators {
	class OffscreenIndicators : HudPart {
		public FContainer container;
		Vector2 screenSize;
		const float INNER_MARGIN = 40;
		const float OUTER_MARGIN = 10;

		class CreaturePulser {
			public Creature creature;
			float pulseProgression;

			public CreaturePulser (Creature creature) {
				this.creature = creature;
			}

			public void Update(OffscreenIndicators hudpart, RoomCamera camera, Vector2 screenSize) {
				Vector2 creaturePos = creature.mainBodyChunk.pos;
				Vector2 creatureScreenPos = creaturePos - camera.pos;
				
				float senseRange = screenSize.x; // Max sense range
				// Reduce the range based on certain factors
				senseRange *= (1 - 0.8f * camera.room.Darkness(creaturePos));
				senseRange *= (1 - 0.4f * creature.Submersion);
					
				Vector2 closestPointOnEdge = new(
					Mathf.Clamp(creatureScreenPos.x, 0, screenSize.x),
					Mathf.Clamp(creatureScreenPos.y, 0, screenSize.y)
				);
					
				float distance = Vector2.Distance(closestPointOnEdge, creatureScreenPos);

				float naturalPulseRate = Time.deltaTime / 4;
				float stepPulseRate = Vector2.Distance(creature.mainBodyChunk.lastPos, creaturePos);
				float pulseSpeed = Mathf.Lerp(1, 5, Mathf.InverseLerp(senseRange, 0, distance));
				
				Player player = hudpart.hud.owner as Player;
				if (player != null) {
					pulseSpeed *= Mathf.InverseLerp(1.8f, 0f, player.Submersion);
				}

				naturalPulseRate *= GetCreatureVisibilityFactor(creature);
				stepPulseRate *= GetCreatureAudibleFactor(creature); 

				pulseProgression -= naturalPulseRate + stepPulseRate;

				if (pulseProgression < 0) {
					hudpart.hud.fadeCircles.Add(new (hudpart.hud, creature.Template.smallCreature ? 10 : 20, pulseSpeed, 0.8f, 20, 4, new Vector2(Mathf.Clamp(creatureScreenPos.x, 0, screenSize.x), Mathf.Clamp(creatureScreenPos.y, 0, screenSize.y)), hudpart.container));
					pulseProgression += 100;
				}
			}
		}

		ObjectTracker<AbstractCreature, CreaturePulser> trackedCreatures;

		class WeaponWarning {
			Weapon weapon;
			Vector2 scale = new Vector2(5, 5);
			Color color;

			FSprite glow = new("Futile_White") {
				shader = RWCustom.Custom.rainWorld.Shaders["FlatLight"],
				color = Color.white,
			};

			public WeaponWarning(Weapon weapon, FContainer container) {
				this.weapon = weapon;
				container.AddChild(glow);

				color = weapon is Rock || weapon is FlareBomb || weapon is SporePlant || weapon is PuffBall ? Color.yellow : Color.red;
			}

			public void Update(OffscreenIndicators hudpart, RoomCamera camera, Vector2 screenSize, float timeStacker) {
				Vector2 weaponPos = Vector2.Lerp(weapon.firstChunk.lastPos, weapon.firstChunk.pos, timeStacker);
				Vector2 weaponScreenPos = weaponPos - camera.pos;
				
				float senseRange = screenSize.x; // Max sense range
				// Reduce the range based on certain factors
				senseRange *= (1 - 0.8f * camera.room.Darkness(weaponPos));
				senseRange *= (1 - 0.4f * weapon.Submersion);
					
				Vector2 closestPointOnEdge = new(
					Mathf.Clamp(weaponScreenPos.x, 0, screenSize.x),
					Mathf.Clamp(weaponScreenPos.y, 0, screenSize.y)
				);
					
				float distance = Vector2.Distance(closestPointOnEdge, weaponScreenPos);

				glow.x = Mathf.Clamp(weaponScreenPos.x, 0, screenSize.x);
				glow.y = Mathf.Clamp(weaponScreenPos.y, 0, screenSize.y);

				
				if (weapon.mode == Weapon.Mode.Thrown) {
					this.scale = Vector2.Lerp(new Vector2(10, 0.5f), this.scale, Mathf.Exp(-Time.deltaTime * 8));
					glow.rotation = weapon.throwDir.ToVector2().GetAngle();
				} else {
					this.scale = Vector2.Lerp(new Vector2(5, 5), this.scale, Mathf.Exp(-Time.deltaTime * 8));
				}
				glow.color = Color.Lerp(color, glow.color, Mathf.Exp(-Time.deltaTime * 8));

				float scale = 1 + distance / senseRange;
				glow.scaleX = this.scale.x * scale;
				glow.scaleY = this.scale.y * scale;
				glow.alpha = 1 - distance / senseRange;
			}

			public void RemoveSprites() {
				glow.RemoveFromContainer();
			}
		}

		ObjectTracker<AbstractWorldEntity, WeaponWarning> trackedObjects;
		List<CreatureSymbol> creatureSymbols = new();

		public OffscreenIndicators(HUD.HUD hud, FContainer container) : base(hud) {
			this.container = container;

			screenSize = hud.rainWorld.options.ScreenSize;

			RoomCamera camera = ((RainWorldGame)hud.rainWorld.processManager.currentMainLoop).cameras[0];
			
			if (Options.offscreenDisplayType.Value == "pulse") {
                trackedCreatures = new()
                {
                    OnGetCollection = () => camera.room.abstractRoom.creatures,
                    OnExistCheck = x =>
                    {
                        Creature creature = x.realizedCreature;
                        if (creature == null || creature.dead || creature.inShortcut)
                        {
                            return false;
                        }

                        return !camera.IsPointInView(creature.mainBodyChunk.pos);
                    },

                    OnCreate = x => new CreaturePulser(x.realizedCreature)
                };
            }

			if (Options.showOffscreenThrownItems.Value) {
				trackedObjects = new()
				{
					OnGetCollection = () => camera.room.abstractRoom.entities,
					OnExistCheck = x =>
					{
						AbstractPhysicalObject abstractPhysicalObject = x as AbstractPhysicalObject;
						Weapon weapon = abstractPhysicalObject?.realizedObject as Weapon;
						if (weapon == null ||
						!(
							weapon is not FirecrackerPlant
						) ||
						!(
							weapon.mode == Weapon.Mode.Thrown ||
							weapon is ExplosiveSpear spear && spear.Ignited ||
							weapon is ScavengerBomb bomb && bomb.ignited ||
							weapon is SingularityBomb singularity && singularity.ignited
						))
						{
							return false;
						}

						return !camera.IsPointInView(weapon.firstChunk.pos);
					},

					OnCreate = x => new WeaponWarning((Weapon)((AbstractPhysicalObject)x).realizedObject, container),
					OnDestroy = x => x.RemoveSprites()
				};
			}
        }

		// 40 tps
		public override void Update() {
			if (Options.offscreenDisplayType.Value == "pulse") {
				UpdatePulses();
			}

			if (Options.showOffscreenThrownItems.Value) {
				trackedObjects.Update();
			}
		}

		public override void Draw(float timeStacker) {
			if (Options.offscreenDisplayType.Value == "icon") {
				DrawIcons(timeStacker);
			}

			if (Options.showOffscreenThrownItems.Value) {
				RoomCamera camera = ((RainWorldGame)hud.rainWorld.processManager.currentMainLoop).cameras[0];
				foreach (WeaponWarning tracker in trackedObjects.GetTrackers()) {
					tracker.Update(this, camera, screenSize, timeStacker);
				}
			}
		}

		void DrawIcons(float timeStacker) {
			RoomCamera camera = ((RainWorldGame)hud.rainWorld.processManager.currentMainLoop).cameras[0];
			Room room = camera.room;

			Vector2 halfScreenSize = screenSize / 2;

			creatureSymbols.ForEach(x => x.RemoveSprites());
			creatureSymbols.Clear();

			foreach (AbstractCreature abstractCreature in room.abstractRoom.creatures) {
				Creature creature = abstractCreature?.realizedCreature;
				if (creature == null || creature.inShortcut || creature.dead) {
					continue; // Disclude if creature is not physically existing
				}

				Vector2 creaturePosition = Vector2.Lerp(creature.mainBodyChunk.lastPos, creature.mainBodyChunk.pos, timeStacker);
				Vector2 creatureScreenPosition = creaturePosition - camera.pos;
				Vector2 creatureRelativePosition = creatureScreenPosition - halfScreenSize;

				Vector2 distance = new (
					Mathf.Abs(creatureRelativePosition.x) - halfScreenSize.x,
					Mathf.Abs(creatureRelativePosition.y) - halfScreenSize.y
				);
				if (distance.x < 0 && distance.y < 0 || distance.x > screenSize.x || distance.y > screenSize.y) {
					continue; // Disclude if creature is in-screen
				}

				var creatureSymbolData = CreatureSymbol.SymbolDataFromCreature(abstractCreature);
				CreatureSymbol creatureSymbol = new(creatureSymbolData, container);
				creatureSymbols.Add(creatureSymbol);
				creatureSymbol.Show(true);

				// Don't flash
				creatureSymbol.lastShowFlash = 0f;
				creatureSymbol.showFlash = 0f;

				float senseRange = screenSize.x; // Max sense range
				// Reduce the range based on certain factors
				senseRange *= (1 - 0.8f * room.Darkness(creaturePosition));
				senseRange *= (1 - 0.4f * creature.Submersion);

				Vector2 margin = new (
					Mathf.Lerp(INNER_MARGIN, OUTER_MARGIN, distance.x / screenSize.x),
					Mathf.Lerp(INNER_MARGIN, OUTER_MARGIN, distance.y / screenSize.x)
				);

				Vector2 pointOnMargin = new(
					Mathf.Clamp(creatureScreenPosition.x, margin.x, screenSize.x - margin.x),
					Mathf.Clamp(creatureScreenPosition.y, margin.y, screenSize.y - margin.y)
				);
				
				float dist = Mathf.Max(distance.x, distance.y);
				float spriteAlpha = Mathf.InverseLerp(senseRange, 50f, dist);
				
				Player player = hud.owner as Player;
				if (player != null) {
					spriteAlpha *= Mathf.InverseLerp(1.8f, 0f, player.Submersion);
				}

				spriteAlpha *= GetCreatureVisibilityFactor(creature);

				float spriteScale = Mathf.InverseLerp(screenSize.x, 0, dist);

				spriteAlpha = Mathf.Min(spriteAlpha - creature.Submersion * 0.8f, 1);

				creatureSymbol.myColor = GetCreatureSymbolicColor(abstractCreature);

				creatureSymbol.symbolSprite.scale = spriteScale;
				creatureSymbol.shadowSprite1.scale = spriteScale;
				creatureSymbol.shadowSprite2.scale = spriteScale;

				creatureSymbol.symbolSprite.alpha = spriteAlpha;
				creatureSymbol.shadowSprite1.alpha = spriteAlpha;
				creatureSymbol.shadowSprite2.alpha = spriteAlpha;

				creatureSymbol.Draw(timeStacker, pointOnMargin);
			}
		}

		void UpdatePulses() {
			trackedCreatures.Update();

			RoomCamera camera = ((RainWorldGame)hud.rainWorld.processManager.currentMainLoop).cameras[0];
			foreach (CreaturePulser tracker in trackedCreatures.GetTrackers()) {
				tracker.Update(this, camera, screenSize);
			}
		}

		public static float GetCreatureVisibilityFactor(Creature creature) {
			if (creature.Template.type == CreatureTemplate.Type.WhiteLizard) {
				LizardGraphics lizardGraphics = creature.graphicsModule as LizardGraphics;
				return lizardGraphics == null ? 1 : 1 - lizardGraphics.Camouflaged;
				
			} else if (ModManager.MSC && creature.Template.type == MoreSlugcatsEnums.CreatureTemplateType.StowawayBug) {
				return Mathf.InverseLerp(-1f, 0f, creature.VisibilityBonus);

			} else if (creature.Template.type == CreatureTemplate.Type.PoleMimic) {
				return 1 - ((PoleMimic)creature).mimic;

			} else if (creature.Template.type == CreatureTemplate.Type.DropBug) {
				return 1 - ((DropBug)creature).inCeilingMode;
			
			} else if (creature.Template.type == CreatureTemplate.Type.Spider || creature.Template.type == CreatureTemplate.Type.TempleGuard) {
				return 0;
			}

			return 1;
		}

		public static float GetCreatureAudibleFactor(Creature creature) {
			if (creature.Template.type == CreatureTemplate.Type.Centipede) {
				return 2;
			
			} else if (creature.Template.type == CreatureTemplate.Type.Spider || creature.Template.type == CreatureTemplate.Type.TempleGuard) {
				return 0;
			}

			return 1;
		}

		public static Color GetCreatureSymbolicColor(AbstractCreature abstractCreature) {
			if (abstractCreature.IsVoided()) {
				return RainWorld.SaturatedGold;
			}

			CreatureTemplate template = abstractCreature.creatureTemplate;
			if (template.TopAncestor().type == CreatureTemplate.Type.Centipede && abstractCreature.superSizeMe) {
				return Custom.HSL2RGB(0.33f, 0.5f, 0.5f);
			
			}
			
			Creature creature = abstractCreature.realizedCreature;
			if (template.type == CreatureTemplate.Type.Slugcat) {
				return RainWorld.PlayerObjectBodyColors[((Player)creature).playerState.playerNumber];
			
			} else if (template.type == CreatureTemplate.Type.BrotherLongLegs && ((DaddyLongLegs)creature).colorClass) {
				return new Color(0f, 0f, 1f);
			
			} else if (template.type == CreatureTemplate.Type.Overseer && creature.graphicsModule != null) {
				return ((OverseerGraphics)creature.graphicsModule).MainColor;
			}
			
			return CreatureSymbol.ColorOfCreature(CreatureSymbol.SymbolDataFromCreature(abstractCreature));
		}
	}
}
