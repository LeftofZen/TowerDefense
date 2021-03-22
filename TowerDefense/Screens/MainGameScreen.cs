using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Screens;
using MonoGame.ImGui.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerDefense.Screens
{
	class MainGameScreen : ScreenBase
	{
		private SpriteBatch _spriteBatch;
		public ImGUIRenderer GuiRenderer;
		Tile[,] map;
		int mapWidth = 10;
		int mapHeight = 10;
		Dictionary<TileType, Color> colourLookup;
		List<Point> enemyPath;
		bool hasPath;
		int tileSize = 64;

		List<Enemy> enemies;
		Spawner spawner;

		float goalHealth = 1000;
		float goalMaxHealth = 1000;
		bool mapDrawingMode = true;
		List<Tower> towers;
		int score = 0;

		public MainGameScreen(Game game) : base(game)
		{
		}

		public override void Initialize()
		{
			var pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
			pixel.SetData(new Color[] { Color.White });
			GameServices.Textures.Add("pixel", pixel);

			enemyPath = new List<Point>();
			enemies = new List<Enemy>();
			towers = new List<Tower>();
			spawner = new Spawner();

			colourLookup = new Dictionary<TileType, Color>();
			colourLookup.Add(TileType.Blocked, Color.Black);
			colourLookup.Add(TileType.Free, Color.LightGray);
			colourLookup.Add(TileType.Goal, Color.Red);
			colourLookup.Add(TileType.Path, Color.SaddleBrown);
			colourLookup.Add(TileType.Spawn, Color.Green);


			map = new Tile[mapHeight, mapWidth];
			for (var y = 0; y < mapHeight; y++)
			{
				for (var x = 0; x < mapWidth; x++)
				{
					map[y, x] = new Tile();
					map[y, x].type = TileType.Free;
				}
			}

			map[1, 1].type = TileType.Spawn;
			map[8, 8].type = TileType.Goal;

			GuiRenderer = new ImGUIRenderer(Game).Initialize().RebuildFontAtlas();

			base.Initialize();
		}
		public override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

			var sfxNames = new List<string> { "laser1", "explosion1", "explosion2", "blip1" };
			foreach (var v in sfxNames)
			{
				if (!GameServices.SoundEffects.ContainsKey(v))
				{
					GameServices.SoundEffects.Add(v, Game.Content.Load<SoundEffect>("SoundEffects\\" + v));
				}
			}

			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			var mouse = Mouse.GetState();
			var translated = mouse.Position.ToVector2() - new Vector2(tileSize * 2, tileSize * 2);
			var mouseTile = new Point((int)(translated.X / tileSize), (int)(translated.Y / tileSize));

			if (Game.Services.GetService<InputManager>().IsNewKeyPress(Keys.Escape))
			{
				Game.Services.GetService<ScreenManager>().LoadScreen(new MainMenuScreen(Game));
				return;
			}

			if (mapDrawingMode)
			{
				// drawing path
				if (ContainedInMap(map, mouseTile))
				{
					var tile = map[mouseTile.Y, mouseTile.X];
					if (mouse.LeftButton == ButtonState.Pressed)
					{
						if (tile.type != TileType.Goal && tile.type != TileType.Spawn && tile.tower == null)
						{
							tile.type = TileType.Path;
						}
					}
					if (mouse.RightButton == ButtonState.Pressed)
					{
						if (tile.type != TileType.Goal && tile.type != TileType.Spawn)
						{
							tile.type = TileType.Free;
						}
					}
				}
				UpdatePath();
			}
			else
			{
				// drawing towers
				if (ContainedInMap(map, mouseTile))
				{
					var tile = map[mouseTile.Y, mouseTile.X];
					if (mouse.LeftButton == ButtonState.Pressed)
					{
						if (tile.type == TileType.Free && tile.tower == null)
						{
							tile.tower = new Tower(mouseTile);
							tile.tower.tileSize = tileSize;
							towers.Add(tile.tower);
						}
					}
					if (mouse.RightButton == ButtonState.Pressed)
					{
						if (tile.tower != null)
						{
							_ = towers.Remove(tile.tower);
							tile.tower = null;
						}
					}
				}
			}

			UpdateTowers(gameTime);
			UpdateEnemies(gameTime);
			UpdateWinLoseCondition();
		}

		void UpdateTowers(GameTime gameTime)
		{
			foreach (var t in towers)
			{
				var time = (float)gameTime.TotalGameTime.TotalMilliseconds;
				if (time - t.lastFireTime > t.cooldown)
				{
					Enemy closestEnemy = null;
					foreach (var e in enemies)
					{
						if (t.distToEnemy(e) < t.range)
						{
							if (closestEnemy == null || t.distToEnemy(e) < t.distToEnemy(closestEnemy))
							{
								closestEnemy = e;
							}
						}
					}

					if (closestEnemy != null)
					{
						// fire at enemy
						t.BulletLine = (t.Center, closestEnemy.position);
						t.ShootAt(closestEnemy);

						t.lastFireTime = time;
						PlaySound("laser1");
					}
				}

				// fade time for bullet line
				if (gameTime.TotalGameTime.TotalSeconds - t.bulletDrawFadeTime > t.lastFireTime)
				{
					t.BulletLine = null;
				}
			}
		}

		void UpdateWinLoseCondition()
		{
			if (enemies.Count == 0)
			{
				// win
			}
			else if (goalHealth <= 0)
			{
				// lose
			}

		}

		void PlaySound(string soundName)
		{
			var sfx = GameServices.SoundEffects[soundName].CreateInstance();
			sfx.Volume = 0.1f;
			sfx.Play();
		}

		void UpdateEnemies(GameTime gameTime)
		{
			if (!hasPath)
			{
				enemies.Clear();
				return;
			}

			var goal = FindTile(map, TileType.Goal);

			if (spawner.IsActive && spawner.ShouldSpawn(gameTime))
			{
				var spawn = FindTile(map, TileType.Spawn);
				var vec = (spawn.ToVector2() * new Vector2(tileSize)) + new Vector2(tileSize / 2);
				var enemy = spawner.Spawn(gameTime, vec);
				enemies.Add(enemy);
				PlaySound("blip1");
			}

			// update enemies

			var toDelete = new List<Enemy>();
			foreach (var e in enemies)
			{
				if (e.IsDead)
				{
					// enemy died
					score += e.value;
					toDelete.Add(e);
					PlaySound("explosion1");
					continue;
				}

				var etile = (e.position / new Vector2(tileSize)).ToPoint();

				if (etile == goal)
				{
					// enemy got to goal tile
					score -= e.health;
					goalHealth -= e.health;
					toDelete.Add(e);
					PlaySound("explosion2");
					continue;
				}

				var index = enemyPath.FindIndex(ee => ee == etile);
				if (index == -1)
				{
					throw new Exception("Index for enemy path wasn't found");
				}

				var nextTileIndex = index + 1;
				var nextTile = enemyPath[nextTileIndex];
				var dst = (nextTile.ToVector2() * new Vector2(tileSize)) + new Vector2(tileSize / 2);
				var dir = dst - e.position;
				dir.Normalize();
				e.position += dir * new Vector2(e.speed);
			}

			// 'kill' enemies that reached the base
			foreach (var v in toDelete)
			{
				_ = enemies.Remove(v);
			}
		}

		static bool ContainedInMap(Tile[,] map, Point p)
		{
			var ydim = map.GetLength(0);
			var xdim = map.GetLength(1);

			return p.X >= 0 && p.X < xdim && p.Y >= 0 && p.Y < ydim;
		}

		void UpdatePath()
		{
			// update enemy path
			var spawn = FindTile(map, TileType.Spawn);
			var goal = FindTile(map, TileType.Goal);
			enemyPath.Clear();
			enemyPath.Add(spawn);
			var current = spawn;
			while (current != goal)
			{
				var count = enemyPath.Count;

				var p1 = new Point(current.X, current.Y - 1);
				var p2 = new Point(current.X, current.Y + 1);
				var p3 = new Point(current.X - 1, current.Y);
				var p4 = new Point(current.X + 1, current.Y);

				void CheckTile(Point p)
				{
					if (ContainedInMap(map, p))
					{
						if (map[p.Y, p.X].type == TileType.Path || map[p.Y, p.X].type == TileType.Goal)
						{
							if (!enemyPath.Contains(p))
							{
								enemyPath.Add(p);
								current = p;
							}
						}
					}
				}

				CheckTile(p1);
				CheckTile(p2);
				CheckTile(p3);
				CheckTile(p4);

				if (enemyPath.Count == count)
					break;
			}

			hasPath = enemyPath.Last() == goal;

		}

		static Point FindTile(Tile[,] map, TileType toFind)
		{
			for (var y = 0; y < map.GetLength(0); y++)
			{
				for (var x = 0; x < map.GetLength(1); x++)
				{
					if (map[y, x].type == toFind)
						return new Point(x, y);
				}
			}

			return new Point(-1, -1);
		}

		public override void Draw(GameTime gameTime)
		{
			Game.GraphicsDevice.Clear(Color.CornflowerBlue);
			// map
			_spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(tileSize * 2, tileSize * 2, 0), blendState: BlendState.NonPremultiplied);

			for (var y = 0; y < mapHeight; y++)
			{
				for (var x = 0; x < mapWidth; x++)
				{
					_spriteBatch.FillRectangle(new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize), Color.Black);
					_spriteBatch.FillRectangle(new Rectangle(x * tileSize + 1, y * tileSize + 1, tileSize - 1, tileSize - 1), colourLookup[map[y, x].type]);
				}
			}

			var pathMarkerSize = 8;
			foreach (var p in enemyPath)
			{

				_spriteBatch.FillRectangle(
					new Rectangle(
						p.X * tileSize + tileSize / 2 - (pathMarkerSize / 2),
						p.Y * tileSize + tileSize / 2 - (pathMarkerSize / 2),
						pathMarkerSize,
						pathMarkerSize),
					Color.BlanchedAlmond);
			}

			foreach (var e in enemies)
			{
				e.Draw(gameTime, _spriteBatch, tileSize);
			}

			foreach (var t in towers)
			{
				t.Draw(_spriteBatch, gameTime);
			}

			_spriteBatch.End();

			///
			// gui
			///

			_spriteBatch.Begin();

			// base health bar
			_spriteBatch.FillRectangle(new Rectangle(0, 0, tileSize / 2, 200), Color.Red);
			_spriteBatch.FillRectangle(new Rectangle(0, 0, tileSize / 2, (int)(goalHealth / goalMaxHealth * 200f)), Color.Green);

			_spriteBatch.DrawString(GameServices.Fonts["SegoeUI"], score.ToString(), new Vector2(10, 10), Color.White);

			_spriteBatch.End();

			DrawImGui(gameTime);
		}

		void DrawImGui(GameTime gameTime)
		{
			// ImGUI
			GuiRenderer.BeginLayout(gameTime);
			if (ImGui.CollapsingHeader("Program Settings", ImGuiTreeNodeFlags.DefaultOpen))
			{
				var mode = mapDrawingMode ? "Tiles" : "Towers";
				_ = ImGui.Checkbox($"DrawMode={mode}", ref mapDrawingMode);
				ImGui.Text($"HasPath={hasPath}");
				ImGui.Text($"UseHalfPixelOffset={Game.GraphicsDevice.UseHalfPixelOffset}");
			}

			GuiRenderer.EndLayout();
		}

	}
}
