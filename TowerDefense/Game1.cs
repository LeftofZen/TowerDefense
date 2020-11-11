using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TowerDefense
{
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
		float speed = 1f;
		float goalHealth = 1000;
		float goalMaxHealth = 1000;

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

			// TODO: use this.Content to load your game content here
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			var mouse = Mouse.GetState();
			var translated = mouse.Position.ToVector2() - new Vector2(64, 64);
			var tile = new Point((int)(translated.X / 32), (int)(translated.Y / 32));

			// drawing path
			if (ContainedInMap(map, tile))
			{
				if (mouse.LeftButton == ButtonState.Pressed)
				{
					map[tile.Y, tile.X].type = TileType.Path;
				}
				if (mouse.RightButton == ButtonState.Pressed)
				{
					map[tile.Y, tile.X].type = TileType.Free;
				}
			}

			UpdatePath();
			UpdateEnemies(gameTime);
			UpdateWinLoseCondition();


			base.Update(gameTime);
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

		void UpdateEnemies(GameTime gameTime)
		{
			if (!hasPath)
			{
				return;
			}

			var goal = FindTile(map, TileType.Goal);
			// spawn enemies
			var curr = gameTime.TotalGameTime.TotalSeconds;
			if (curr - lastSpawnTime > spawnInterval)
			{
				var spawn = FindTile(map, TileType.Spawn);
				// spawn
				enemies.Add(new Enemy(spawn.ToVector2() * new Vector2(32), goal, 100));
				lastSpawnTime = curr;
			}

			// update enemies

			var toDelete = new List<Enemy>();
			foreach (var e in enemies)
			{
				var etile = (e.position / new Vector2(32)).ToPoint();

				if (etile == goal)
				{
					goalHealth -= e.health;
					toDelete.Add(e);
					continue;
					// enemy got to goal tile
				}

				var nextTileIndex = enemyPath.FindIndex(ee => ee == etile) + 1;
				if (nextTileIndex < 0)
					continue;
				var nextTile = enemyPath[nextTileIndex];
				var dst = nextTile.ToVector2() * new Vector2(32);
				var dir = dst - e.position;
				dir.Normalize();
				e.position += dir * new Vector2(speed);
			}

			// 'kill' enemies that reached the base
			foreach (var v in toDelete)
			{
				enemies.Remove(v);
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

			// gui

			_spriteBatch.Begin();

			// base health bar
			_spriteBatch.Draw(pixel, new Rectangle(0, 0, 16, 16), hasPath ? Color.Green : Color.Red);

			// hasPath status
			_spriteBatch.Draw(pixel, new Rectangle(0, 0, 16, 200), Color.DarkGray);
			_spriteBatch.Draw(pixel, new Rectangle(0, 0, 16, (int)(goalHealth / goalMaxHealth * 200f)), Color.LightGoldenrodYellow);

			_spriteBatch.End();

			// map

			_spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(64, 64, 0));

			for (var y = 0; y < mapHeight; y++)
			{
				for (var x = 0; x < mapWidth; x++)
				{
					_spriteBatch.Draw(pixel, new Rectangle(x * 32, y * 32, 32, 32), colourLookup[map[y, x].type]);
				}
			}

			foreach (var p in enemyPath)
			{
				_spriteBatch.Draw(pixel, new Rectangle(p.X * 32 + 12, p.Y * 32 + 12, 8, 8), Color.Yellow);
			}

			foreach (var e in enemies)
			{
				_spriteBatch.Draw(pixel, new Rectangle((int)e.position.X, (int)e.position.Y, 8, 8), Color.Blue);
			}

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}

	interface ITower
	{ }

	class Tower : ITower
	{ }

	enum TileType
	{
		Path, Free, Blocked, Spawn, Goal
	}

	class Tile
	{
		public ITower tower;
		public TileType type;
	}

	class Enemy
	{
		public Vector2 position;
		Point goalTile;
		public int health;

		public Enemy(Vector2 position, Point goalTile, int health)
		{
			this.position = position;
			this.goalTile = goalTile;
			this.health = health;
		}
	}
}
