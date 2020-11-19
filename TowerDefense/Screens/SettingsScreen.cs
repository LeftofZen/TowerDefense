using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;

namespace TowerDefense.Screens
{
	class SettingsScreen : ScreenBase
	{
		public SettingsScreen(Game game) : base(game)
		{
		}

		public override void Draw(GameTime gameTime)
		{
			Game.GraphicsDevice.Clear(Color.LightGray);
		}

		public override void Update(GameTime gameTime)
		{
			if (Game.Services.GetService<InputManager>().IsNewKeyPress(Keys.Escape))
			{
				Game.Services.GetService<ScreenManager>().LoadScreen(new MainMenuScreen(Game));
				return;
			}
		}
	}
}
