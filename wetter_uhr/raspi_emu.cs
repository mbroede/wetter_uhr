// =====================================================================
// File:          raspi_emu.cs
// Author:        Michael Bröde
// Created:       02.04.2019
// =====================================================================
// Dieses ist ein Hilfsmodul zum Kompilieren des Projektes unter MS-Windows.
// Auf dem Raspberry wird die LED-Panel-Bibliothek RGBLedMatrix.dll eingebunden,
// die die hier definierten Klassen/Funktionen dann vollständig implementiert.
namespace rpi_rgb_led_matrix_sharp
{
    class RGBLedMatrixOptions
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int GpioSlowdown { get; set; }
        public int LimitRefreshRateHz { get; set; }
        public int Brightness { get; set; }
        public bool DisableHardwarePulsing { get; set; }
    }

    class RGBLedMatrix
    {
        public RGBLedMatrix(RGBLedMatrixOptions o)
        {
        }

        public RGBLedCanvas CreateOffscreenCanvas()
        {
            return null;
        }

        public RGBLedCanvas SwapOnVsync(RGBLedCanvas c)
        {
            return null;
        }
    }

    class RGBLedCanvas
    {
        public void SetPixel(int x, int y, Color col)
        {
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color col)
        {
        }

        public void Fill(Color col)
        {
        }

        public int DrawText(RGBLedFont font, int x, int y, Color col, string text)
        {
            return 0;
        }
    }

    class RGBLedFont
    { 
        public RGBLedFont(string fontfile)
        {

        }
    }

    class Color
    {
        public Color(int r, int g, int b)
        {
        }
    }
}
