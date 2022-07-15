﻿using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace GoldenTrainer
{
    public class CompleteDisplay : Entity
    {
        private const float TextPadLeft = 144;
        private const float TextPadRight = 6;

        private readonly MTexture _bg;
        private readonly MTexture _skull;
        private readonly MTexture _x;
        private readonly Level _level;

        private string _text;
        private float _lerp;

        private float _width;

        public CompleteDisplay(Level level)
        {
            _level = level;
            _bg = GFX.Gui["strawberryCountBG"];

            var mode = _level.Session.Area.Mode;

            if (mode == AreaMode.Normal)
            {
                _skull = GFX.Gui["collectables/skullBlue"];
            }
            else if (mode == AreaMode.BSide)
            {
                _skull = GFX.Gui["collectables/skullRed"];
            }
            else
            {
                _skull = GFX.Gui["collectables/skullGold"];
            }

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
            var posY = 10f * 16;

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
            base.Update();

            Y = Calc.Approach(Y, GetYPosition(), Engine.DeltaTime * 800f);

            if (!_level.Paused)
            {
                _lerp = Calc.Approach(_lerp, 1f, 1.2f * Engine.RawDeltaTime);
            }
            else
            {
                _lerp = Calc.Approach(_lerp, 0f, 2f * Engine.RawDeltaTime);
            }
        }

        public override void Render()
        {
            if (GoldenTrainerModule.Settings.ActivateMod) {
                var basePos = Vector2.Lerp(new Vector2(0 - _width, Y), new Vector2(0, Y), Ease.CubeOut(_lerp)).Round();

                _bg.Draw(new Vector2(_width - _bg.Width + basePos.X, Y));


                if (_width > _bg.Width + basePos.X)
                {
                    Draw.Rect(0, Y, _width - _bg.Width + basePos.X, 38f, Color.Black);
                }
                _skull.Draw(new Vector2(basePos.X + 26, Y - 24));
                _x.Draw(new Vector2(basePos.X + 94, Y - 15));

                ActiveFont.DrawOutline(_text, new Vector2(basePos.X + TextPadLeft, Y - 25f), Vector2.Zero, Vector2.One, Color.White, 2f, Color.Black);
            }
        }
    }
}
