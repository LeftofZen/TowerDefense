using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;

namespace TowerDefense.Screens
{
	abstract class ScreenBase : Screen
	{
		public ScreenBase(Game game)
		{
			Game = game;
		}

		public Game Game { get; }

		public abstract override void Draw(GameTime gameTime);
		public abstract override void Update(GameTime gameTime);
	}

	//interface IScreen
	//{
	//}
}
