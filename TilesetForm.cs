﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MLLE
{
    public partial class TilesetForm : Form
    {
        J2TFile Tileset;
        List<J2TFile> Tilesets;
        uint MaxTilesSupportedByLevel;
        uint NumberOfTilesInThisLevelBesidesThisTileset;
        bool Result = false;
        public TilesetForm()
        {
            InitializeComponent();
        }
        internal bool ShowForm(J2TFile tileset, List<J2TFile> tilesets, int max, uint number)
        {
            Tileset = tileset;
            Tilesets = tilesets;
            MaxTilesSupportedByLevel = (uint)max;
            NumberOfTilesInThisLevelBesidesThisTileset = number;
            inputLast.Maximum = Tileset.TotalNumberOfTiles;
            inputLast.Value = Tileset.FirstTile + Tileset.TileCount;
            inputFirst.Maximum = inputLast.Value - 1;
            inputFirst.Value = Tileset.FirstTile;
            inputLast.Minimum = inputFirst.Value + 1;
            ButtonDelete.Visible = Tilesets.Contains(Tileset);
            CreateImageFromTileset();
            UpdatePreviewControls();
            ShowDialog();
            return Result;
        }

        internal static void GetColorPaletteFromTileset(J2TFile tileset, Bitmap image)
        {
            var palette = image.Palette;
            var entries = palette.Entries;
            for (uint i = 0; i < 256; ++i)
            {
                var color = tileset.Palette.Colors[i];
                entries[i] = Color.FromArgb(color[0], color[1], color[2]);
            }
            image.Palette = palette;
        }

        private void CreateImageFromTileset()
        {
            pictureBox1.Height = (int)(Tileset.TotalNumberOfTiles + 9) / 10 * 32;
            var image = new Bitmap(pictureBox1.Width, pictureBox1.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] bytes = new byte[data.Height * data.Stride];
            //Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            for (uint i = 0; i < Tileset.TotalNumberOfTiles; ++i)
            {
                var tileImage = Tileset.Images[Tileset.ImageAddress[i]];
                var xOrg = (i % 10) * 32;
                var yOrg = i / 10 * 32;
                for (uint x = 0; x < 32; ++x)
                    for (uint y = 0; y < 32; ++y)
                    {
                        bytes[xOrg + x + (yOrg + y) * data.Stride] = tileImage[x + y*32];
                    }
            }
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            image.UnlockBits(data);
            pictureBox1.Image = image;

            GetColorPaletteFromTileset(Tileset, image);
        }

        private void UpdatePreviewControls()
        {
            var proposedTileCount = inputLast.Value - inputFirst.Value;
            var proposedTotal = NumberOfTilesInThisLevelBesidesThisTileset + proposedTileCount;
            outputMath.Text = String.Format("{0} + {1} =\n{2}/{3}", NumberOfTilesInThisLevelBesidesThisTileset, proposedTileCount, proposedTotal, MaxTilesSupportedByLevel);
            outputMath.ForeColor = (OKButton.Enabled = proposedTotal <= MaxTilesSupportedByLevel) ? Color.Black : Color.Red;
            EdgePanelLeft.Location = GetPointFromTileID((int)inputFirst.Value, 0);
            EdgePanelRight.Location = GetPointFromTileID((int)inputLast.Value - 1, 32);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Tileset.FirstTile = (uint)inputFirst.Value;
            Tileset.TileCount = (uint)(inputLast.Value - inputFirst.Value);
            if (!Tilesets.Contains(Tileset))
                Tilesets.Add(Tileset);
            Result = true;
            Dispose();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            Tilesets.Remove(Tileset);
            Result = true;
            Dispose();
        }

        private void inputFirst_ValueChanged(object sender, EventArgs e)
        {
            inputLast.Minimum = inputFirst.Value + 1;
            UpdatePreviewControls();
        }

        private void inputLast_ValueChanged(object sender, EventArgs e)
        {
            inputFirst.Maximum = inputLast.Value - 1;
            UpdatePreviewControls();
        }

        int GetMouseTileIDFromTileset(MouseEventArgs e)
        {
            var pictureOrigin = pictureBox1.AutoScrollOffset;
            return ((e.X - pictureOrigin.X) / 32 + (e.Y - pictureOrigin.Y) / 32 * 10);
        }
        Point GetPointFromTileID(int tileID, int xAdjust)
        {
            return new Point(
                pictureBox1.Left + (tileID % 10) * 32 - EdgePanelLeft.Width / 2 + xAdjust,
                pictureBox1.Top + (tileID / 10) * 32 - (EdgePanelLeft.Height - 32) / 2
            );
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Text = "Setup Extra Tileset - " + GetMouseTileIDFromTileset(e);
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            Text = "Setup Extra Tileset";
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                    inputLast.Value = GetMouseTileIDFromTileset(e) + 1;
                else
                    inputFirst.Value = GetMouseTileIDFromTileset(e);
            }
            catch { } //invalid value because of Minimum/Maximum of that textbox; do nothing
        }
    }
}
