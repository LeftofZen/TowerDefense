using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace TowerDefense
{
	public class InputManager : GameComponent
	{
		public InputManager(Game game) : base(game)
		{
		}

		Keys[] keysDown;
		Keys[] prevKeysDown;

		public bool IsNewKeyPress(Keys key)
		{
			return keysDown.Contains(key) && !prevKeysDown.Contains(key);
		}

		public override void Update(GameTime gameTime)
		{
			prevKeysDown = keysDown;

			var mouse = Mouse.GetState();
			var keyboard = Keyboard.GetState();

			keysDown = keyboard.GetPressedKeys();

			base.Update(gameTime);
		}
	}
}
