using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace TowerDefense
{
	public static class GameServices
	{
		public static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(); // TextureCollection??
		public static readonly Dictionary<string, SpriteFont> Fonts = new Dictionary<string, SpriteFont>(); //
		public static readonly Dictionary<string, SoundEffect> SoundEffects = new Dictionary<string, SoundEffect>(); // SoundBank??
		public static readonly Dictionary<string, Song> Songs = new Dictionary<string, Song>(); // SongCollection??
																								//public static InputManager InputManager;
	}

	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		Tile[,] map;
		int mapWidth = 10;
		int mapHeight = 10;
		Texture2D pixel;
		Dictionary<TileType, Color> colourLookup;
		List<Point> enemyPath;
		bool hasPath;
		double lastSpawnTime;
		double spawnInterval = 1.0;
		List<Enemy> enemies;
		float speed = 2f;
		float goalHealth = 1000;
		float goalMaxHealth = 1000;
		bool mapDrawingMode = true;
		int enemyInitialHealth = 100;
		List<Tower> towers;
		int score = 0;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			pixel = new Texture2D(_graphics.GraphicsDevice, 1, 1);
			pixel.SetData(new Color[] { Color.White });

			enemyPath = new List<Point>();
			enemies = new List<Enemy>();
			towers = new List<Tower>();

			colourLookup = new Dictionary<TileType, Color>();
			colourLookup.Add(TileType.Blocked, Color.Black);
			colourLookup.Add(TileType.Free, Color.LightGray);
			colourLookup.Add(TileType.Goal, Color.Red);
			colourLookup.Add(TileType.Path, Color.SandyBrown);
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

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			var fontNames = new List<string> { "SergoeUI" };
			foreach (var v in fontNames)
			{
				GameServices.Fonts.Add(v, Content.Load<SpriteFont>("Fonts\\" + v));
			}

			var sfxNames = new List<string> { "laser1", "explosion1", "explosion2", "blip1" };
			foreach (var v in sfxNames)
			{
				GameServices.SoundEffects.Add(v, Content.Load<SoundEffect>("SoundEffects\\" + v));
			}
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			var mouse = Mouse.GetState();
			var translated = mouse.Position.ToVector2() - new Vector2(64, 64);
			var mouseTile = new Point((int)(translated.X / 32), (int)(translated.Y / 32));

			var keyboard = Keyboard.GetState();
			if (keyboard.IsKeyDown(Keys.Space))
			{
				mapDrawingMode = !mapDrawingMode;
			}


			if (mapDrawingMode)
			{
				// drawing path
				if (ContainedInMap(map, mouseTile))
				{
					var tile = map[mouseTile.Y, mouseTile.X];
					if (mouse.LeftButton == ButtonState.Pressed)
					{
						if (tile.type != TileType.Goal && tile.type != TileType.Spawn)
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
							towers.Add(tile.tower);
						}
					}
					if (mouse.RightButton == ButtonState.Pressed)
					{
						if (tile.type == TileType.Free && tile.tower != null)
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

			base.Update(gameTime);
		}

		void UpdateTowers(GameTime gameTime)
		{
			foreach (var t in towers)
			{
				var time = (float)gameTime.TotalGameTime.TotalSeconds;
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
						closestEnemy.health -= t.dps;
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
			// spawn enemies
			var curr = gameTime.TotalGameTime.TotalSeconds;
			if (curr - lastSpawnTime > spawnInterval)
			{
				//if (goalHealth > 0)
				{
					// spawn
					var spawn = FindTile(map, TileType.Spawn);
					enemies.Add(new Enemy((spawn.ToVector2() * new Vector2(32)) + new Vector2(16), enemyInitialHealth));
					lastSpawnTime = curr;
					PlaySound("blip1");
				}
			}

			// update enemies

			var toDelete = new List<Enemy>();
			foreach (var e in enemies)
			{
				if (e.health <= 0)
				{
					// enemy died
					score += enemyInitialHealth;
					toDelete.Add(e);
					PlaySound("explosion1");
					continue;
				}

				var etile = (e.position / new Vector2(32)).ToPoint();

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
				var dst = nextTile.ToVector2() * new Vector2(32) + new Vector2(16);
				var dir = dst - e.position;
				dir.Normalize();
				e.position += dir * new Vector2(speed);
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

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// map
			_spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(64, 64, 0));

			for (var y = 0; y < mapHeight; y++)
			{
				for (var x = 0; x < mapWidth; x++)
				{
					_spriteBatch.Draw(pixel, new Rectangle(x * 32, y * 32, 32, 32), Color.Black);
					_spriteBatch.Draw(pixel, new Rectangle(x * 32 + 1, y * 32 + 1, 30, 30), colourLookup[map[y, x].type]);
				}
			}

			foreach (var p in enemyPath)
			{
				_spriteBatch.Draw(pixel, new Rectangle(p.X * 32 + 12, p.Y * 32 + 12, 8, 8), Color.Yellow);
			}

			foreach (var e in enemies)
			{
				_spriteBatch.Draw(pixel, new Rectangle((int)e.position.X - 4, (int)e.position.Y - 4, 8, 8), Color.Blue);

				// health bar
				var barWidth = 32;
				var percent = (float)e.health / enemyInitialHealth;
				_spriteBatch.Draw(pixel, new Rectangle((int)e.position.X - barWidth / 2, (int)e.position.Y - 4 - 8, barWidth, 4), Color.Red);
				_spriteBatch.Draw(pixel, new Rectangle((int)e.position.X - barWidth / 2, (int)e.position.Y - 4 - 8, (int)(barWidth * percent), 4), Color.Green);
			}

			foreach (var t in towers)
			{
				_spriteBatch.Draw(pixel, new Rectangle(t.Tile.X * 32 + 4, t.Tile.Y * 32 + 4, 24, 24), Color.Purple);

				// bullet line
				if (t.BulletLine.HasValue)
				{
					var src = t.BulletLine.Value.Item1;
					var dst = t.BulletLine.Value.Item2;
					DrawLine(_spriteBatch, src, dst);
				}
			}

			_spriteBatch.End();

			///
			// gui
			///

			_spriteBatch.Begin();

			// base health bar
			_spriteBatch.Draw(pixel, new Rectangle(0, 0, 16, 200), Color.Red);
			_spriteBatch.Draw(pixel, new Rectangle(0, 0, 16, (int)(goalHealth / goalMaxHealth * 200f)), Color.Green);

			// hasPath status
			_spriteBatch.Draw(pixel, new Rectangle(16, 0, 16, 16), hasPath ? Color.Green : Color.Red);

			// drawing mode status
			_spriteBatch.Draw(pixel, new Rectangle(32, 0, 16, 16), mapDrawingMode ? Color.Green : Color.Red);

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end)
		{
			Vector2 edge = end - start;
			// calculate angle to rotate line
			float angle = (float)Math.Atan2(edge.Y, edge.X);

			sb.Draw(pixel,
				new Rectangle(// rectangle defines shape of line and position of start of line
					(int)start.X,
					(int)start.Y,
					(int)edge.Length(), //sb will stretch the texture to fill this rectangle
					3), //width of line, change this to make thicker line
				null,
				Color.Aqua, //colour of line
				angle,     //angle of line (calulated above)
				new Vector2(0, 0), // point in line about which to rotate
				SpriteEffects.None,
				0);
		}
	}

	class Tower
	{
		public int dps = 25;
		public float cooldown = 0.5f;
		public float lastFireTime;
		public float range = 96;
		public float bulletDrawFadeTime = 0.2f;
		public (Vector2, Vector2)? BulletLine { get; set; }
		public Point Tile { get; set; }

		public Vector2 Center => (Tile.ToVector2() * new Vector2(32)) + new Vector2(16);

		public Tower(Point tile) => Tile = tile;

		public float distToEnemy(Enemy e) => Vector2.Distance(e.position, Center);
	}

	enum TileType
	{
		Path, Free, Blocked, Spawn, Goal
	}

	class Tile
	{
		public Tower tower;
		public TileType type;
	}

	class Enemy
	{
		public Vector2 position;
		public int health;

		public Enemy(Vector2 position, int health)
		{
			this.position = position;
			this.health = health;
		}
	}
}
