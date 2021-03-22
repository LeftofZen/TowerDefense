using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace TowerDefense
{
	class Spawner
	{
		public uint numberOfEnemiesToSpawn;
		public uint numberOfEnemiesSpawnedSoFar;
		public EnemyType enemyTypeToSpawn;
		public List<EnemyType>? enemySpawnPattern;
		static Random rnd = new Random();
		double spawnInterval = 1.0;
		double lastSpawnTime;

		static Dictionary<EnemyType, EnemyPrototype> EnemyPrototypes = new Dictionary<EnemyType, EnemyPrototype>()
		{
			{ EnemyType.Regular, new EnemyPrototype(100, 100, 2f, Color.Blue, 0.45f) },
			{ EnemyType.Fast, new EnemyPrototype(50, 50, 3f, Color.Yellow, 0.3f) },
			{ EnemyType.Tank, new EnemyPrototype(200, 200, 1f, Color.Gray, 0.6f) },
		};

		public bool ShouldSpawn(GameTime gameTime) => (gameTime.TotalGameTime.TotalSeconds - lastSpawnTime) > spawnInterval;

		public bool IsActive => numberOfEnemiesToSpawn >= numberOfEnemiesSpawnedSoFar;

		public Enemy Spawn(GameTime gameTime, Vector2 spawnAt)
		{
			if (!IsActive)
			{
				return null;
			}

			var enemyType = rnd.Next(0, EnemyPrototypes.Count);
			var enemy = new Enemy(EnemyPrototypes[(EnemyType)enemyType])
			{
				position = spawnAt
			};

			lastSpawnTime = gameTime.TotalGameTime.TotalSeconds;

			return enemy;
		}
	}

	struct EnemyPrototype
	{
		public int health;
		public int value;
		public float speed;
		public Color color;
		public float size;

		public EnemyPrototype(int health, int value, float speed, Color color, float size)
		{
			this.health = health;
			this.value = value;
			this.speed = speed;
			this.color = color;
			this.size = size;
		}
	}

	public enum EnemyType
	{
		Fast, Tank, Regular
	}

	class Enemy
	{
		public Vector2 position;
		public int health;
		public int value;
		public float speed;
		public Color color;
		public float size;

		public Enemy(EnemyPrototype proto)
		{
			this.health = proto.health;
			this.value = proto.value;
			this.speed = proto.speed;
			this.color = proto.color;
			this.size = proto.size;
		}

		public Enemy(Vector2 position, int health)
		{
			this.position = position;
			this.health = health;
			this.value = health;
		}

		public void Draw(GameTime gameTime, SpriteBatch sb, int tileSize)
		{
			var rect = new RectangleF(0, 0, tileSize * size, tileSize * size);
			rect.Position = position.ToPoint() - rect.Center;

			sb.FillRectangle(rect, color);
			sb.DrawRectangle(rect, Color.Black, 2);

			// health bar
			var barWidth = tileSize;
			var percent = (float)health / value;
			sb.FillRectangle(new Rectangle((int)position.X - barWidth / 2, (int)position.Y - 4 - 8, barWidth, 4), Color.Red);
			sb.FillRectangle(new Rectangle((int)position.X - barWidth / 2, (int)position.Y - 4 - 8, (int)(barWidth * percent), 4), Color.Green);

			sb.DrawString(GameServices.Fonts["SegoeUI"], value.ToString(), rect.Position, Color.Black);
			sb.DrawString(GameServices.Fonts["SegoeUI"], value.ToString(), rect.Position - Vector2.One, Color.White);
		}

		public bool IsDead => health <= 0;
	}
}
