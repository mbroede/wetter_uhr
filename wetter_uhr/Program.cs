using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using rpi_rgb_led_matrix_sharp;
using wetter_uhr_classes;
using help_classes;
using openweather_classes;

namespace wetter_uhr
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread threadWeather = new Thread(GetOpenWeather);
            threadWeather.Start();

            var matrix = new RGBLedMatrix(new RGBLedMatrixOptions
            {
                Rows = 64,
                Cols = 64,
                GpioSlowdown = 2,
                Brightness = 25,
                DisableHardwarePulsing = false
            });
            var canvas = matrix.CreateOffscreenCanvas();
            CWetterUhr wetteruhr = new CWetterUhr(matrix, canvas);
            wetteruhr.Run();
        }

        static void GetOpenWeather()
        {
            COpenWeather ow = new COpenWeather();
            ow.Run();
        }
    }
}
