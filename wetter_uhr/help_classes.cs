// =====================================================================
// File:          help_classes.cs
// Author:        Michael Bröde
// Created:       02.04.2019
// =====================================================================
using System;
using System.IO;
using rpi_rgb_led_matrix_sharp;

namespace help_classes
{
    // =================================================================
    // CGeneral
    // =================================================================
    static class CGeneral
    {
        static public string OpenWeatherDir
        {
            get 
            {                
                return Environment.OSVersion.VersionString
                    // z.B. "Microsoft Windows NT 6.2.9200.0"
                    .ToLower().IndexOf("windows") >= 0
                    ? @"c:\Projekte\wetter_uhr\rgb-weather\"
                    : @"/home/pi/openweather/";
            }
        }

        static public bool SetTextToFile(string aFileName, string aText, bool aAppend = false)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(aFileName, aAppend))
                {
                    sw.WriteLine(aText);
                }
                return true;
            }
            catch
            {
                //throw;
                return false;
            }
        }

        public static DateTime StrToDateTime(string s)
        {
            // s = "30.12.2018 23:15:05"
            string[] parts = s.Split(' ');
            string[] dateparts = parts[0].Split('.');
            string[] timeparts = parts[1].Split(':');
            return new DateTime(
                Convert.ToInt32(dateparts[2]),   // Jahr
                Convert.ToInt32(dateparts[1]),   // Monat
                Convert.ToInt32(dateparts[0]),   // Tag
                Convert.ToInt32(timeparts[0]),   // Stunde
                Convert.ToInt32(timeparts[1]),   // Minute
                Convert.ToInt32(timeparts[2]));  // Sekunde
        }
    }

    // =================================================================
    // CColor
    // =================================================================
    static class CColor
    {
        // "Color" kommt nicht aus dem .Net-Namespace System.Drawing
        // sondern aus der rpi_rgb_led_matrix_sharp-Bibliothek
        static public readonly Color White = new Color(255, 255, 255);
        static public readonly Color Yellow = new Color(255, 255, 0);
        static public readonly Color Red = new Color(255, 0, 0);
        static public readonly Color Orange = new Color(255, 183, 7);
        static public readonly Color Tomato = new Color(255, 99, 71);
        static public readonly Color OrangeRed = new Color(255, 69, 0);
        static public readonly Color SandyBrown = new Color(244, 164, 96);
        static public readonly Color SaddleBrown = new Color(139, 69, 19);
        static public readonly Color Magenta = new Color(255, 0, 255);
        static public readonly Color Green = new Color(0, 255, 0);
        static public readonly Color DarkSeaGreen = new Color(143, 188, 143);
        static public readonly Color Blue = new Color(0, 0, 255);
        static public readonly Color Cyan = new Color(0, 255, 255);
        static public readonly Color Black = new Color(0, 0, 0);
        static public readonly Color LightGoldenrod = new Color(238, 221, 130);
        static public readonly Color LightCoral = new Color(240, 128, 128);

        static public readonly Color LightGrey = new Color(211, 211, 211);
        static public readonly Color DimGrey = new Color(105, 105, 105);
        static public readonly Color DarkSlateGrey = new Color(47, 79, 79);
        static public readonly Color DarkGrey = new Color(169, 169, 169);
        static public readonly Color DarkDarkGrey = new Color(25, 25, 25);
        static public readonly Color DarkDarkGrey2 = new Color(10, 10, 10);

        static public readonly Color CornflowerBlue = new Color(100, 149, 237);
        static public readonly Color LightSkyBlue = new Color(135, 206, 250);
        static public readonly Color PowderBlue = new Color(176, 224, 230);
        static public readonly Color DodgerBlue = new Color(30, 144, 255);
        static public readonly Color DeepSkyBlue = new Color(0, 191, 255);
        static public readonly Color MediumBlue = new Color(0, 0, 205);
        static public readonly Color RoyalBlue = new Color(65, 105, 225);

        static public readonly Color DarkBlue = new Color(0, 0, 139);
        static public readonly Color NavyBlue = new Color(0, 0, 128);

        // Needed by Matrix Rain
        static public readonly Color Green0 = new Color(0, 0, 0);
        static public readonly Color Green1 = new Color(0, 25, 0);
        static public readonly Color Green2 = new Color(10, 60, 10);
        static public readonly Color Green3 = new Color(20, 90, 20);
        static public readonly Color Green4 = new Color(30, 120, 30);
        static public readonly Color Green6 = new Color(45, 160, 45);
        static public readonly Color Green8 = new Color(60, 200, 60);
        static public readonly Color Green9 = new Color(210, 255, 210);
    }

    // =================================================================
    // CPoint
    // =================================================================
    class CPoint
    {
        private int _x;
        private int _y;

        public int x
        {
            get { return _x; }
            set { _x = value; }
        }

        public int y
        {
            get { return _y; }
            set { _y = value; }
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", _x, _y);
        }

        // ----------------------
        // ctor
        // ----------------------
        public CPoint(int x, int y)
        {
            SetCoord(x, y);
        }

        public void SetCoord(int x, int y)
        {
            _x = x;
            _y = y;
        }
    }

    // =================================================================
    // CBaseClock
    // =================================================================
    abstract class CBaseClock
    {
        internal DateTime dtnow { get; set; }
        internal int msec { get; set; }
        internal int sec { get; set; }
        internal int min { get; set; }
        internal int hour { get; set; }
        internal int day { get; set; }
        internal int month { get; set; }
        internal int year { get; set; }

        /// <summary>
        /// Setzt die Datums- und Zeit-Properties dieser Klasse auf aktuelle Werte
        /// </summary>
        internal void SetTimeParts()
        {
            dtnow = DateTime.Now;
            msec = dtnow.Millisecond;
            sec = dtnow.Second;
            min = dtnow.Minute;
            hour = dtnow.Hour;
            day = dtnow.Day;
            month = dtnow.Month;
            year = dtnow.Year;
        }
    }
}
