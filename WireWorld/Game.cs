﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using GdiApi;

namespace WireWorld
{
    public class Game
    {
        Context context;
        Context controlWindow;
        FrameRateManager frameRate = new FrameRateManager();
        WireWorldMap map;

        Pen borderColor = new Pen(Color.White, 1f);
        WireWorldState selected = WireWorldState.Dead;  
        bool autoRunning = false;
        float cyclesPerSecond = 3;
        float adjuster = 0.2f;
        bool selfExiting = false;

        int mapWidth = 0;
        int mapHeight = 0;

        Point[] borderLines = new Point[5];
        Point[] mouseLineMarkers = new Point[5];
        Point[] mouseLines = new Point[5];

        public Game(int squareSize, int width, int height)
        {
            mapWidth = width;
            mapHeight = height;

            map = new WireWorldMap(width, height, WireWorldState.Dead)
            {
                SquareSize = squareSize,
                BootingSquareSize = squareSize
            };

            context = new Context(new Size(1000, 1000), "Fun Time!", false);
            controlWindow = new Context(new Size(200, 1000), "Controls", false);

            borderLines[0] = new Point(0, 0);
            borderLines[1] = new Point(map.Width * squareSize, 0);
            borderLines[2] = new Point(map.Width * squareSize, map.Height * squareSize);
            borderLines[3] = new Point(0, map.Height * squareSize);
            borderLines[4] = new Point(0, 0);

            mouseLineMarkers[0] = new Point(0,0);
            mouseLineMarkers[1] = new Point(squareSize, 0);
            mouseLineMarkers[2] = new Point(squareSize, squareSize);
            mouseLineMarkers[3] = new Point(0, squareSize);
            mouseLineMarkers[4] = new Point(0, 0);

            context.Load += Load;
            context.Render += Render;
            context.KeyDown += Context_KeyDown;
            context.Closing += Context_Closing;

            controlWindow.ClearScreen = false;
            controlWindow.ManageFrameDraw = false;

            var cycleBtn = new Button()
            {
                Size = new Size(200, 20),
                Visible = true,
                Location = new Point(0,0),
                Text = "Cycle",
            };
            cycleBtn.Click += CycleBtn_Click;
            cycleBtn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            controlWindow.Controls.Add(cycleBtn);

            var autoToggle = new Button()
            {
                Size = new Size(200, 20),
                Visible = true,
                Location = new Point(0, 25),
                Text = "Toggle Auto",
            };
            autoToggle.Click += AutoToggle_Click;
            autoToggle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            controlWindow.Controls.Add(autoToggle);

            var increaseAuto = new Button()
            {
                Size = new Size(200, 20),
                Visible = true,
                Location = new Point(0, 50),
                Text = "Increase Auto Speed",
            };
            increaseAuto.Click += IncreaseAuto_Click;
            increaseAuto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            controlWindow.Controls.Add(increaseAuto);

            var decreaseAuto = new Button()
            {
                Size = new Size(200, 20),
                Visible = true,
                Location = new Point(0, 75),
                Text = "Decrease Auto Speed",
            };
            decreaseAuto.Click += DecreaseAuto_Click;
            decreaseAuto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            controlWindow.Controls.Add(decreaseAuto);

            controlWindow.Closing += ControlWindow_Closing;

            context.Begin(false);
        }

        private FormClosingEventArgs Context_Closing(FormClosingEventArgs fcea) => Close(fcea);

        public FormClosingEventArgs Close(FormClosingEventArgs f)
        {
            if (selfExiting)
            {
                f.Cancel = false;
                return f;
            }

            var d = MessageBox.Show("Unsaved changes will be lost.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (d == DialogResult.Yes)
            {
                selfExiting = true;
                controlWindow.Exit();
                context.Exit();
                f.Cancel = false;
                return f;
            }
            else
            {
                f.Cancel = true;
                return f;
            }
        }

        public void SaveMap(WireWorldMap map, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(map.Width +  "|" + map.Height);

                for (var x = 0; x < map.Width; x++)
                {
                    for (var y = 0; y < map.Height; y++)
                    {
                        var id = (int)map[x, y];
                        var final = x + "|" + y + "|" + id;
                        writer.WriteLine(final);
                    }
                }
            }
        }

        public WireWorldMap LoadMap(string path)
        {
            var lines = File.ReadAllLines(path);
            int width = Convert.ToInt16(lines[0].Split('|')[0]);
            int height = Convert.ToInt16(lines[0].Split('|')[1]);
            var toReturn = new WireWorldMap(width, height, WireWorldState.Dead);

            for (var i = 1; i < lines.Length; i++)
            {
                var str = lines[i];
                var parts = str.Split('|');
                var intParts = Array.ConvertAll<string, int>(parts, int.Parse);
                var x = intParts[0];
                var y = intParts[1];
                var id = intParts[2];
                toReturn.SetState(x, y, (WireWorldState)id);
            }

            return toReturn;
        }

        private void DecreaseAuto_Click(object sender, EventArgs e) => cyclesPerSecond *= (1 - adjuster);
        private void IncreaseAuto_Click(object sender, EventArgs e) => cyclesPerSecond *= (1 + adjuster);
        private void AutoToggle_Click(object sender, EventArgs e) => autoRunning = !autoRunning;
        private FormClosingEventArgs ControlWindow_Closing(FormClosingEventArgs fcea) => Close(fcea);
        private void CycleBtn_Click(object sender, EventArgs e) => map.Cycle();

        private void Context_KeyDown(KeyEventArgs kea)
        {
            switch (kea.KeyCode)
            {
                case Keys.Space:
                    map.Cycle();
                    break;
                case Keys.NumPad1:
                case Keys.D1:
                    selected = WireWorldState.Dead;
                    if (kea.Alt)
                    {
                        Reset(WireWorldState.Dead);
                    }
                    break;
                case Keys.NumPad3:
                case Keys.D3:
                    selected = WireWorldState.Head;
                    if (kea.Alt)
                    {
                        Reset(WireWorldState.Head);
                    }
                    break;
                case Keys.NumPad4:
                case Keys.D4:
                    selected = WireWorldState.Tail;
                    if (kea.Alt)
                    {
                        Reset(WireWorldState.Tail);
                    }
                    break;
                case Keys.NumPad2:
                case Keys.D2:
                    selected = WireWorldState.Wire;
                    if (kea.Alt)
                    {
                        Reset(WireWorldState.Wire);
                    }
                    break;
                case Keys.A:
                    autoRunning = !autoRunning;
                    break;
                case Keys.OemPeriod:
                    cyclesPerSecond *= (1 + adjuster);
                    break;
                case Keys.Oemcomma:
                    cyclesPerSecond *= (1 - adjuster);
                    break;
                case Keys.S:
                    if (kea.Control)
                    {
                        var save = new SaveFileDialog()
                        {
                            Filter = "Wire Map|*.wiremap",
                        };

                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            SaveMap(map, save.FileName);
                        }
                        
                    }
                    break;
                case Keys.O:
                    if (kea.Control)
                    {
                        var open = new OpenFileDialog()
                        {
                            Filter = "Wire Map|*.wiremap",
                        };

                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            map = LoadMap(open.FileName);
                        }
                    }
                    break;
            }
        }

        public void Reset(WireWorldState state)
        {
            var sizeToUse = map.BootingSquareSize;
            map = new WireWorldMap(mapWidth, mapHeight, state)
            {
                BootingSquareSize = sizeToUse,
                SquareSize = sizeToUse
            };
        }
        public void Load()
        {
            BitmapBuffer.Clear();
            BitmapBuffer.Load(@"Assets\");

            controlWindow.Begin(true);
        }

        int MilliSinceLastAuto = 0;

        public void Render(Graphics graphics, TimeSpan delta)
        {
            //Auto
            if (autoRunning)
            {
                MilliSinceLastAuto += (int)delta.TotalMilliseconds;
                if (MilliSinceLastAuto > (int)(1000f / cyclesPerSecond))
                {
                    MilliSinceLastAuto = 0;
                    map.Cycle();
                }
            }
            
            //Get Framerate
            frameRate.Frame(delta);

            //Set square if mouse clicked
            if (context.MouseClicked)
            {
                map.SetState(context.MouseLocation.X / map.SquareSize,
                    context.MouseLocation.Y / map.SquareSize, selected);
            }

            //Draw Map
            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var state = map.GetState(new Point(x, y));
                    map.DrawState(graphics, new Point(x, y), state, StateDrawMode.Tile);
                }
            }
            
            //Draw border around map
            graphics.DrawLines(borderColor, borderLines);

            //Draw tile grid
            for (var x = 0; x < context.Size.Width; x += map.SquareSize)
            {
                graphics.DrawLine(borderColor, x, 0, x, context.Size.Height);
            }
            for (var y = 0; y < context.Size.Height; y += map.SquareSize)
            {
                graphics.DrawLine(borderColor, 0, y, context.Size.Width, y);
            }

            //Cursor tile
            graphics.DrawRectangle(new Pen(Color.White, 5f), new Rectangle(
                context.MouseLocation.X / map.SquareSize * map.SquareSize,
                context.MouseLocation.Y / map.SquareSize * map.SquareSize,
                map.SquareSize,
                map.SquareSize));

            //graphics.DrawString("Selected Tile Type: " + selected.ToString(), new Font("Times New Roman", 15.0f), Brushes.Orange, new Point(12,12));

            graphics.FillRectangle(Brushes.Orange, new Rectangle(0,0,100,50));
            graphics.FillRectangle(map.BlankColor, new Rectangle(0, 1, 25, 25));
            graphics.FillRectangle(map.WireColor, new Rectangle(25, 1, 25, 25));
            graphics.FillRectangle(map.HeadColor, new Rectangle(50, 1, 25, 25));
            graphics.FillRectangle(map.TailColor, new Rectangle(75, 1, 25, 25));

            var rec = Rectangle.Empty;

            switch (selected)
            {
                case WireWorldState.Dead:
                    rec = new Rectangle(0, 25, 25, 25);
                    break;
                case WireWorldState.Wire:
                    rec = new Rectangle(25, 25, 25, 25);
                    break;
                case WireWorldState.Head:
                    rec = new Rectangle(50, 25, 25, 25);
                    break;
                case WireWorldState.Tail:
                    rec = new Rectangle(75, 25, 25, 25);
                    break;
            }
            
            graphics.DrawBitmap(0, rec);

            //Set title to framerate
            context.Title = "Framerate: " + frameRate.ToString();
        }
    }
}