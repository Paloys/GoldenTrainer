using System;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;

namespace GoldenTrainer
{
    public class CompletionDisplay : Entity
    {
        private const float TextPadLeft = 144;
        private const float TextPadRight = 6;

        private readonly MTexture _bg;
        private readonly MTexture _berry;
        private readonly MTexture _x;
        private readonly Level _level;

        private string _text;
        private float _lerp;

        private float _width;

        public CompletionDisplay(Level level)
        {
            _level = level;
            _bg = GFX.Gui["strawberryCountBG"];

            _berry = GFX.Gui["collectables/goldberry"];

            _x = GFX.Gui["x"];

            Y = GetYPosition();

            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        public void SetDisplayText(string text)
        {
            _text = text;
            _width = ActiveFont.Measure(_text).X + TextPadLeft + TextPadRight;
        }

        public float GetYPosition()
        {
            var posY = 10f * 16 + 10f;

            if (!_level.TimerHidden)
            {
                if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter)
                {
                    posY += 58f;
                }
                else if (Settings.Instance.SpeedrunClock == SpeedrunType.File)
                {
                    posY += 78f;
                }
            }

            return posY;
        }

        public override void Update()
        {
            if (GoldenTrainerModule.Settings.ActivateButton.Pressed && !_level.Paused)
            {
                GoldenTrainerModule.Instance.CompletionCount = 0;
                GoldenTrainerModule.Settings.ActivateMod = !GoldenTrainerModule.Settings.ActivateMod;
            }
            if (GoldenTrainerModule.Settings.ActivateMod)
            {
                if (GoldenTrainerModule.Settings.IncrementButton.Pressed && GoldenTrainerModule.Settings.NumberOfCompletions < 10 && !_level.Paused)
                {
                    GoldenTrainerModule.Settings.NumberOfCompletions++;
                }

                else if (GoldenTrainerModule.Settings.DecrementButton.Pressed && GoldenTrainerModule.Settings.NumberOfCompletions > 2 && !_level.Paused)
                {
                    GoldenTrainerModule.Settings.NumberOfCompletions--;
                }
            }
            base.Update();

            Y = Calc.Approach(Y, GetYPosition(), Engine.DeltaTime * 800f);

            _lerp = GoldenTrainerModule.Settings.ActivateMod && !(_level.Paused ^ _level.PauseMainMenuOpen) ? Calc.Approach(_lerp, 1f, 1.2f * Engine.RawDeltaTime) : Calc.Approach(_lerp, 0f, 2f * Engine.RawDeltaTime);
        }

        public override void Render()
        {
            var basePos = Vector2.Lerp(new Vector2(0 - _width, Y), new Vector2(0, Y), Ease.CubeOut(_lerp)).Round();

            _bg.Draw(new Vector2(_width - _bg.Width + basePos.X, Y));

            if (_width > _bg.Width + basePos.X)
            {
                Draw.Rect(0, Y, _width - _bg.Width + basePos.X, 38f, Color.Black);
            }
            _berry.Draw(new Vector2(basePos.X, Y - 40));
            _x.Draw(new Vector2(basePos.X + 94, Y - 15));

            ActiveFont.DrawOutline(_text, new Vector2(basePos.X + TextPadLeft, Y - 25f), Vector2.Zero, Vector2.One, Color.White, 2f, Color.Black);
        }
    }
}