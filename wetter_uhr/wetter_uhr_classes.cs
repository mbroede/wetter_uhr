// =====================================================================
// File:          wetter_uhr_classes.cs
// Author:        Michael Bröde
// Created:       02.04.2019
// =====================================================================
using System;
using rpi_rgb_led_matrix_sharp;
using help_classes;
using System.Threading;
using System.IO;

namespace wetter_uhr_classes
{
    #region - Uhr -
    // =================================================================
    // CClkBlock
    // =================================================================
    class CClkBlock
    {
        private RGBLedCanvas _canvas;

        private int _top;
        private int _left;
        private Color _fillcolor_enabled;
        private readonly Color _fillcolor_disabled = CColor.DarkDarkGrey2;
        private readonly int _width = 2;
        private readonly int _height = 2;

        public override string ToString()
        {
            // Für Debug-Zwecke
            return String.Format("_top:{0}, _left:{1}", _top, _left);
        }

        public CClkBlock(RGBLedCanvas canvas, int top, int left, Color fillcolor)
        {
            _canvas = canvas;
            _top = top;
            _left = left;
            _fillcolor_enabled = fillcolor;
        }

        public void Enable(bool enable)
        {
            for (int i = 0; i < _height; i++)
            {
                _canvas.DrawLine(
                    _left, 
                    _top + i, 
                    _left + _width - 1, 
                    _top + i,
                    enable ? _fillcolor_enabled : _fillcolor_disabled);
            }
        }
    }

    // =================================================================
    // CClkBaseColumn
    // =================================================================
    abstract class CClkBaseColumn
    {
        private int _startY;
        private int _startX;
        private CClkBlock[] _blocks;

        private int _top;
        private int _left;

        public CClkBaseColumn(RGBLedCanvas canvas, CClkBlock[] blocks, int startx, int starty, Color fillcolor, Color fillcolor5)
        {
            _blocks = blocks;
            _startX = startx;
            _startY = starty;
            for (int i = 0; i < _blocks.Length; i++)
            {
                _left = _startX;
                _top = _startY - i * 3;
                if (i > 8)
                {
                    _top -= 3;
                }
                _blocks[i] = new CClkBlock(canvas, _top, _left,
                    i == 4 ? fillcolor5 : fillcolor);
            }
        }

        public void Show(int timepart)
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                if (i <= 8)
                {
                    // Einer-Blöcke
                    _blocks[i].Enable(timepart % 10 > i);
                }
                else
                {
                    // Zehner-Blöcke
                    _blocks[i].Enable(timepart >= (i - 8) * 10);
                }
            }
        }
    }

    // =================================================================
    // CHourColumn
    // =================================================================
    class CHourColumn : CClkBaseColumn
    {
        public CHourColumn(RGBLedCanvas canvas, CClkBlock[] blocks)
            : base(canvas, blocks, 3, 56, CColor.Green, CColor.Green9)
        {
        }
    }

    // =================================================================
    // CMinuteColumn
    // =================================================================
    class CMinuteColumn : CClkBaseColumn
    {
        public CMinuteColumn(RGBLedCanvas canvas, CClkBlock[] blocks)
            : base(canvas, blocks, 6, 56, CColor.Yellow, CColor.White)
        {
        }
    }

    // =================================================================
    // CSecondColumn
    // =================================================================
    class CSecondColumn : CClkBaseColumn
    {
        public CSecondColumn(RGBLedCanvas canvas, CClkBlock[] blocks)
            : base(canvas, blocks, 9, 56, CColor.Blue, CColor.CornflowerBlue)
        {
        }
    }
    #endregion

    #region - Wetter -
    // =================================================================
    // CWeather_ID
    // =================================================================
    class CWeather_ID
    {
        private int _ID;
        private string _MAIN;
        private string _DESCRIPTION;
        private string _ICON;
        private Color _myCloudColor;
        private int _myRainfallIntensity;

        public int ID { get => _ID; set => _ID = value; }
        public string MAIN { get => _MAIN; set => _MAIN = value; }
        public string DESCRIPTION { get => _DESCRIPTION; set => _DESCRIPTION = value; }
        public string ICON { get => _ICON; set => _ICON = value; }
        public int MyRainfallIntensity { get => _myRainfallIntensity; set => _myRainfallIntensity = value; }
        internal Color MyCloudColor { get => _myCloudColor; set => _myCloudColor = value; }

        public CWeather_ID(int id, string main, string description, string icon, Color cloudcolor, int rainfallintensity)
        {
            _ID = id;
            _MAIN = main;
            _DESCRIPTION = description;
            _ICON = icon;
            _myCloudColor = cloudcolor;
            _myRainfallIntensity = rainfallintensity;
        }
    }

    // =================================================================
    // CMyWeather
    // =================================================================
    class CMyWeather
    {

        public string Mode { get; set; } // h=hourly, d=daily
        public DateTime? Time { get; set; }
        public int Temp { get; set; }
        public int Clouds { get; set; }
        public int Pop { get; set; }
        public double Wind_ms { get; set; }
        public int Wind_bft { get; set; }
        public int Wind_gust_bft { get; set; }
        public int Wind_deg { get; set; }
        public int Weather_id { get; set; }

        public override string ToString()
        {
            return string.Format("Modus:{0} Time:{1} Temp:{2} Clouds:{3} Pop:{4} Wind_ms:{5:#0.00} Wind_bft:{6} Wind_gust_bft:{7} Wind_deg:{8} Weather_id:{9}",
                Mode, Time, Temp, Clouds, Pop, Wind_ms, Wind_bft, Wind_gust_bft, Wind_deg, Weather_id);
        }

        public CMyWeather()
        {
            Init();
        }

        public void Init()
        {
            Mode = string.Empty;
            Time = null;
            Temp = 0;
            Clouds = 0;
            Pop = 0;
            Wind_ms = 0d;
            Wind_bft = 0;
            Wind_gust_bft = 0;
            Wind_deg = 0;
            Weather_id = 0;
        }
    }
    #endregion

    // =================================================================
    // CWetterUhr
    // =================================================================
    class CWetterUhr : CBaseClock
    {
        private RGBLedMatrix _matrix;
        private RGBLedCanvas _canvas;
        private RGBLedFont _font;

        private CSecondColumn _secondColumn;
        private CMinuteColumn _minuteColumn;
        private CHourColumn _hourColumn;

        private string _weatherCsvFile;
        private string _weatherLogFile;
        private int _minutesCheckWeatherCycle;
        private int _minuteWeatherChecked;
        private CMyWeather[] _weather;
        private CWeather_ID[] _weather_ids;
        private double[] _t7days;
        private double[] _n7days;
        private double[] _w7days;
        private double[] _ytemp;

        private int _tempMin;
        private int _tempMax;
        private int _windMin;
        private int _windMax;
        private int _rainMin;
        private int _rainMax;

        // Hilfsvariablen
        private int len;
        private string s;
        private int x;
        private int y;
        private int ynull;
        private Color col;
        private int idx;
        private int intensity;
        private double d;
        private double d1;
        private double d2;
        private double diff;
        private int imax;

        public CWetterUhr(RGBLedMatrix matrix, RGBLedCanvas canvas)
        {
            _matrix = matrix;
            _canvas = canvas;
            _font = new RGBLedFont("/home/pi/rpi-rgb-led-matrix/fonts/5x8.bdf");

            _secondColumn = new CSecondColumn(canvas, new CClkBlock[14]);
            _minuteColumn = new CMinuteColumn(canvas, new CClkBlock[14]);
            _hourColumn = new CHourColumn(canvas, new CClkBlock[12]);

            _weatherCsvFile = Path.Combine(CGeneral.OpenWeatherDir, "weather.csv");
            _weatherLogFile = Path.Combine(CGeneral.OpenWeatherDir, "weather_log.txt");
            _minutesCheckWeatherCycle = 2;
            _minuteWeatherChecked = -1;
            _weather = new CMyWeather[20]; // 12 x hourly, 8 x daily
            for (int i = 0; i < _weather.Length; i++)
            {
                _weather[i] = new CMyWeather();
            }
            _weather_ids = new CWeather_ID[]
            {
                new CWeather_ID(200, "Thunderstorm", "thunderstorm with light rain", "11d", CColor.NavyBlue, 33),
                new CWeather_ID(201, "Thunderstorm", "thunderstorm with rain", "11d", CColor.NavyBlue, 66),
                new CWeather_ID(202, "Thunderstorm", "thunderstorm with heavy rain", "11d", CColor.NavyBlue, 100),
                new CWeather_ID(210, "Thunderstorm", "light thunderstorm", "11d", CColor.NavyBlue, 0),
                new CWeather_ID(211, "Thunderstorm", "thunderstorm", "11d", CColor.NavyBlue, 0),
                new CWeather_ID(212, "Thunderstorm", "heavy thunderstorm", "11d", CColor.NavyBlue, 0),
                new CWeather_ID(221, "Thunderstorm", "ragged thunderstorm", "11d", CColor.NavyBlue, 0),
                new CWeather_ID(230, "Thunderstorm", "thunderstorm with light drizzle", "11d", CColor.NavyBlue, 25),
                new CWeather_ID(231, "Thunderstorm", "thunderstorm with drizzle", "11d", CColor.NavyBlue, 50),
                new CWeather_ID(232, "Thunderstorm", "thunderstorm with heavy drizzle", "11d", CColor.NavyBlue, 75),
                new CWeather_ID(300, "Drizzle", "light intensity drizzle", "09d", CColor.RoyalBlue, 10),
                new CWeather_ID(301, "Drizzle", "drizzle", "09d", CColor.RoyalBlue, 10),
                new CWeather_ID(302, "Drizzle", "heavy intensity drizzle", "09d", CColor.RoyalBlue, 20),
                new CWeather_ID(310, "Drizzle", "light intensity drizzle rain", "09d", CColor.RoyalBlue, 30),
                new CWeather_ID(311, "Drizzle", "drizzle rain", "09d", CColor.RoyalBlue, 30),
                new CWeather_ID(312, "Drizzle", "heavy intensity drizzle rain", "09d", CColor.RoyalBlue, 40),
                new CWeather_ID(313, "Drizzle", "shower rain and drizzle", "09d", CColor.RoyalBlue, 40),
                new CWeather_ID(314, "Drizzle", "heavy shower rain and drizzle", "09d", CColor.RoyalBlue, 50),
                new CWeather_ID(321, "Drizzle", "shower drizzle", "09d", CColor.RoyalBlue, 50),
                new CWeather_ID(500, "Rain", "light rain", "10d", CColor.RoyalBlue, 10),
                new CWeather_ID(501, "Rain", "moderate rain", "10d", CColor.RoyalBlue, 20),
                new CWeather_ID(502, "Rain", "heavy intensity rain", "10d", CColor.RoyalBlue, 30),
                new CWeather_ID(503, "Rain", "very heavy rain", "10d", CColor.RoyalBlue, 40),
                new CWeather_ID(504, "Rain", "extreme rain", "10d", CColor.RoyalBlue, 50),
                new CWeather_ID(511, "Rain", "freezing rain", "13d", CColor.RoyalBlue, 60),
                new CWeather_ID(520, "Rain", "light intensity shower rain", "09d", CColor.RoyalBlue, 70),
                new CWeather_ID(521, "Rain", "shower rain", "09d", CColor.NavyBlue, 80),
                new CWeather_ID(522, "Rain", "heavy intensity shower rain", "09d", CColor.NavyBlue, 90),
                new CWeather_ID(531, "Rain", "ragged shower rain", "09d", CColor.NavyBlue, 100),
                new CWeather_ID(600, "Snow", "light snow", "13d", CColor.RoyalBlue, 10),
                new CWeather_ID(601, "Snow", "Snow", "13d", CColor.RoyalBlue, 20),
                new CWeather_ID(602, "Snow", "Heavy snow", "13d", CColor.RoyalBlue, 30),
                new CWeather_ID(611, "Snow", "Sleet", "13d", CColor.RoyalBlue, 40),
                new CWeather_ID(612, "Snow", "Light shower sleet", "13d", CColor.RoyalBlue, 50),
                new CWeather_ID(613, "Snow", "Shower sleet", "13d", CColor.RoyalBlue, 60),
                new CWeather_ID(615, "Snow", "Light rain and snow", "13d", CColor.RoyalBlue, 60),
                new CWeather_ID(616, "Snow", "Rain and snow", "13d", CColor.RoyalBlue, 70),
                new CWeather_ID(620, "Snow", "Light shower snow", "13d", CColor.RoyalBlue, 80),
                new CWeather_ID(621, "Snow", "Shower snow", "13d", CColor.RoyalBlue, 90),
                new CWeather_ID(622, "Snow", "Heavy shower snow", "13d", CColor.NavyBlue, 100),
                new CWeather_ID(701, "Mist", "mist", "50d", CColor.RoyalBlue, 0),
                new CWeather_ID(711, "Smoke", "Smoke", "50d", CColor.RoyalBlue, 0),
                new CWeather_ID(721, "Haze", "Haze", "50d", CColor.RoyalBlue, 0),
                new CWeather_ID(731, "Dust", "sand/ dust whirls", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(741, "Fog", "fog", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(751, "Sand", "sand", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(761, "Dust", "dust", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(762, "Ash", "volcanic ash", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(771, "Squall", "squalls", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(781, "Tornado", "tornado", "50d", CColor.NavyBlue, 0),
                new CWeather_ID(800, "Clear", "clear sky", "01d", CColor.RoyalBlue, 0),
                new CWeather_ID(801, "Clouds", "few clouds: 11-25%", "02d", CColor.RoyalBlue, 0),
                new CWeather_ID(802, "Clouds", "scattered clouds: 25-50%", "03d", CColor.RoyalBlue, 0),
                new CWeather_ID(803, "Clouds", "broken clouds: 51-84%", "04d", CColor.RoyalBlue, 0),
                new CWeather_ID(804, "Clouds", "overcast clouds: 85-100%", "04d", CColor.NavyBlue, 0)
            };
            _t7days = new double[8];
            _n7days = new double[8];
            _w7days = new double[8];
            _ytemp = new double[8];
        }

        public void Run()
        {
            for (; ; )
            {
                // Init
                base.SetTimeParts();
                GetMyWeather();               
                GetMinMaxValues();

                _canvas.Fill(CColor.Black);

                // Wetter
                if (sec >= 0 && sec < 12) DrawTemperature();
                if (sec >= 12 && sec < 24) DrawRainfall();
                if (sec >= 24 && sec < 36) DrawWind();
                if (sec >= 36 && sec < 48) DrawSummary();
                if (sec >= 48 && sec < 60) Draw7DaysPreview();

                // Uhrzeit
                _hourColumn.Show(base.hour);
                _minuteColumn.Show(base.min);
                _secondColumn.Show(base.sec);

                // Anzeige auf LED-Panel
                _canvas = _matrix.SwapOnVsync(_canvas);

                Thread.Sleep(250);
            }
        }

        #region - MyWeather -
        private void GetMyWeather()
        {
            if ((base.min % _minutesCheckWeatherCycle == 0 && _minuteWeatherChecked != base.min) || _minuteWeatherChecked == -1)
            {
                _minuteWeatherChecked = base.min;

                if (PopulateMyWeatherArray())
                {
                    Populate7DaysPreviewData();
                    //for (int i = 0; i < _weather.Length; i++)
                    //{
                    //    DebugPrint(String.Format("{0:00} {1}", i, _weather[i]));
                    //}
                    //DebugPrint("DONE PopulateMyWeatherArray");

                    _minutesCheckWeatherCycle = 15;
                }
                else
                {
                    _minutesCheckWeatherCycle = 2;
                }
            }
        }

        private void Populate7DaysPreviewData()
        {
            //     y
            //
            //     9  |-------
            //        |
            //        |
            // T  17 -+
            //        |
            //        |
            //    25  |-------
            //        |
            //        |
            // N  32 -+
            //        |
            //        |
            //    39  |-------
            //        |
            //        |
            // W  46 -+
            //        |
            //        |
            //    53  |-------

            double maxY;

            // daily-Items Idx: 12 ... 19 
            // T
            maxY = 0;
            for (int i = 12, j = 0; i <= 19; i++, j++)
            {
                _t7days[j] = _weather[i].Temp;
            }
            _ytemp[0] = 0d;
            for (int i = 1; i < 8; i++)
            {
                _ytemp[i] = _t7days[i] - _t7days[0];
            }
            foreach (var v in _ytemp) if (Math.Abs(v) > maxY) maxY = Math.Abs(v);
            for (int i = 0; i < 8; i++)
            {
                _t7days[i] = 17d - 8d / maxY * _ytemp[i]; // T: y=17
            }

            // N
            maxY = 0;
            for (int i = 12, j = 0; i <= 19; i++, j++)
            {
                _n7days[j] = _weather[i].Pop;
            }
            _ytemp[0] = 0d;
            for (int i = 1; i < 8; i++)
            {
                _ytemp[i] = _n7days[i] - _n7days[0];
            }
            foreach (var v in _ytemp) if (Math.Abs(v) > maxY) maxY = Math.Abs(v);
            for (int i = 0; i < 8; i++)
            {
                _n7days[i] = 32d - 5d / maxY * _ytemp[i]; // N: y=32
            }

            // W
            maxY = 0;
            for (int i = 12, j = 0; i <= 19; i++, j++)
            {
                _w7days[j] = _weather[i].Wind_bft;
            }
            _ytemp[0] = 0d;
            for (int i = 1; i < 8; i++)
            {
                _ytemp[i] = _w7days[i] - _w7days[0];
            }
            foreach (var v in _ytemp) if (Math.Abs(v) > maxY) maxY = Math.Abs(v);
            for (int i = 0; i < 8; i++)
            {
                _w7days[i] = 46d - 5d / maxY * _ytemp[i]; // W: y=46
            }
        }

        private bool PopulateMyWeatherArray()
        {
            int start;
            DateTime dt;
            string[] token;
            string[] lines = null;

            for (int i = 0; i < 20; i++)
            {
                _weather[i].Init();
                _weather[i].Mode = i < 12 ? "h" : "d";
            }

            try
            {
                lines = File.ReadAllLines(_weatherCsvFile); // ca. 5000 Byte
            }
            catch (Exception)
            {
            }
            if (lines == null || lines.Length == 0)
            {
                return false;
            }

            try
            {
                start = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("hourly") == 0)
                    {
                        token = lines[i].Split(';');
                        dt = CGeneral.StrToDateTime(token[1]); // UTC
                        if (System.DateTime.Now <= dt.ToLocalTime())
                        {
                            start = i;  // Die nächste Stunde
                            break;
                        }
                    }
                }
                if (start == 0)
                {
                    return false;
                }

                // hourly
                for (int i = 0; i < 12; i++)
                {
                    if ((start + i) >= lines.Length || lines[start + i].IndexOf("hourly") == -1)
                    {
                        break;
                    }
                    token = lines[start + i].Split(';');
                    CsvTokenToWeather("m", token, ref _weather[i]);
                }

                // daily
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("daily") == 0)
                    {
                        for (int j = 0; j < 8 && (i + j) < lines.Length; j++)
                        {
                            token = lines[i + j].Split(';');
                            CsvTokenToWeather("d", token, ref _weather[12 + j]);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugPrint("PopulateMyWeatherArray went wrong: " + ex.Message);
                return false;
            }
            return true;
        }

        private void CsvTokenToWeather(string mode, string[] token, ref CMyWeather weather)
        {
            // 0        1                    2      3       4     5         6          7           8           9             10
            // name;    dt_utc;              temp;  clouds; pop;  wind_speed; wind_deg; wind_gust; weather_id; weather_main; weather_description
            // current; 03.08.2021 06:06:14; 15,23; 82;     0,71; 3,96;       261;      6,71;      803;        Clouds;       Überwiegend bewölkt
            // hourly;  03.08.2021 06:00:00; 15,23; 82;     0,72; 3,96;       261;      6,71;      803;        Clouds;       Überwiegend bewölkt
            // hourly;  03.08.2021 07:00:00; 15,11; 86;     0,72; 3,96;       268;      6,16;      804;        Clouds;       Bedeckt
            // ...
            DateTime dt;
            double d;
            // Mode
            weather.Mode = mode;
            // time
            dt = CGeneral.StrToDateTime(token[1]); // UTC
            weather.Time = dt.ToLocalTime();
            // temp
            d = Convert.ToDouble(token[2]);
            weather.Temp = Convert.ToInt32(Math.Round(d, 0));
            // cloud
            weather.Clouds = Convert.ToInt32(token[3]);
            // pop
            d = Convert.ToDouble(token[4]);
            weather.Pop = Convert.ToInt32(Math.Round(d * 10)) * 10; // auf nächstliegenden 10er gerundet
            // wind_ms;
            d = Convert.ToDouble(token[5]);
            weather.Wind_ms = d;
            // wind_bft;
            weather.Wind_bft = ConvertMetrePerSecondToBeaufort(d);
            // wind_gust_bft;
            d = Convert.ToDouble(token[7]);
            weather.Wind_gust_bft = ConvertMetrePerSecondToBeaufort(d);
            // wind_deg;
            weather.Wind_deg = Convert.ToInt32(token[6]);
            // weather_id;
            weather.Weather_id = Convert.ToInt32(token[8]);
        }

        private int ConvertMetrePerSecondToBeaufort(double windspeed)
        {
            if (windspeed < 0.51) return 0;
            if (windspeed >= 0.51 && windspeed < 2.06) return 1;
            if (windspeed >= 2.06 && windspeed < 3.60) return 2;
            if (windspeed >= 3.60 && windspeed < 5.66) return 3;
            if (windspeed >= 5.66 && windspeed < 8.23) return 4;
            if (windspeed >= 8.23 && windspeed < 11.32) return 5;
            if (windspeed >= 11.32 && windspeed < 14.40) return 6;
            if (windspeed >= 14.40 && windspeed < 17.49) return 7;
            if (windspeed >= 17.49 && windspeed < 21.09) return 8;
            if (windspeed >= 21.09 && windspeed < 24.69) return 9;
            if (windspeed >= 24.69 && windspeed < 28.81) return 10;
            if (windspeed >= 28.81 && windspeed < 32.92) return 11;
            if (windspeed >= 32.29) return 12;
            return 0;
        }
        #endregion

        #region - GetMinMaxValues -
        private void GetMinMaxValues()
        {
            _tempMin = Int32.MaxValue;
            _tempMax = Int32.MinValue;
            _windMin = Int32.MaxValue;
            _windMax = Int32.MinValue;
            _rainMin = Int32.MaxValue;
            _rainMax = Int32.MinValue;

            for (int i = 0; i < 12; i++)
            {
                if (_weather[i].Temp < _tempMin) _tempMin = _weather[i].Temp;
                if (_weather[i].Temp > _tempMax) _tempMax = _weather[i].Temp;
                if (_weather[i].Pop < _rainMin) _rainMin = _weather[i].Pop;
                if (_weather[i].Pop > _rainMax) _rainMax = _weather[i].Pop;
                if (_weather[i].Wind_bft < _windMin) _windMin = _weather[i].Wind_bft;
                if (_weather[i].Wind_bft > _windMax) _windMax = _weather[i].Wind_bft;
            }
        }
        #endregion

        #region - DrawTemperature -
        private void DrawTemperature()
        {
            //_weather[0].Temp = -13;
            //_weather[1].Temp = -10;
            //_weather[2].Temp = -9;
            //_weather[3].Temp = -6;
            //_weather[4].Temp = -5;
            //_weather[5].Temp = -4;
            //_weather[6].Temp = -2;
            //_weather[7].Temp = -1;
            //_weather[8].Temp = 0;
            //_weather[9].Temp = 1;
            //_weather[10].Temp = 2;
            //_weather[11].Temp = 3;
            //GetMinMaxValues();

            // Header
            _canvas.DrawText(_font, 8, 8, CColor.Blue, "TEMPERATUR");

            s = string.Format("{0}°", _weather[0].Temp);
            len = _canvas.DrawText(_font, 0, 16, CColor.Black, s);
            x = 33 - len;
            _canvas.DrawText(_font, x, 16, CColor.Blue, s);
            _canvas.DrawText(_font, 35, 16, CColor.Blue, string.Format("{0:HH}", _weather[0].Time));
            _canvas.DrawText(_font, 46, 16, CColor.Blue, "Uhr");

            s = string.Format("{0}°", _weather[11].Temp);
            len = _canvas.DrawText(_font, 0, 23, CColor.Black, s);
            x = 33 - len;
            _canvas.DrawText(_font, x, 23, CColor.Blue, s);
            _canvas.DrawText(_font, 35, 23, CColor.Blue, string.Format("{0:HH}", _weather[11].Time));
            _canvas.DrawText(_font, 46, 23, CColor.Blue, "Uhr");

            // Null-Linie
            ynull = 57 - (_tempMin < 0 ? -_tempMin : 0);
            _canvas.DrawLine(14, ynull, 59, ynull, CColor.DarkDarkGrey);

            // Verlauf in Säulen
            for (int i = 0; i < 12; i++)
            {
                x = 14 + (i * 4);
                if (_weather[i].Temp > 0)
                {
                    for (int t = 0; t <= _weather[i].Temp; t++)
                    {
                        col = CColor.Black;
                        if (t >= 0 && t < 10) col = CColor.Green;
                        if (t >= 10 && t < 20) col = CColor.Yellow;
                        if (t >= 20) col = CColor.Red;
                        y = ynull - t;
                        _canvas.DrawLine(x, y, x + 1, y, col);
                    }
                }
                else
                {
                    for (int t = 0; t >= _weather[i].Temp; t--)
                    {
                        col = CColor.Black;
                        if (t <= 0 && t > -5) col = CColor.PowderBlue;
                        if (t <= -5 && t > -10) col = CColor.RoyalBlue;
                        if (t <= -10) col = CColor.NavyBlue;
                        y = ynull - t;
                        _canvas.DrawLine(x, y, x + 1, y, col);
                    }
                }
            }

            DrawPageMarker(1);
        }
        #endregion

        #region - DrawRainfall -
        private void DrawRainfall()
        {
            //_weather[0].Pop = 0;
            //_weather[1].Pop = 1;
            //_weather[2].Pop = 2;
            //_weather[3].Pop = 3;
            //_weather[4].Pop = 4;
            //_weather[5].Pop = 5;
            //_weather[6].Pop = 6;
            //_weather[7].Pop = 7;
            //_weather[8].Pop = 8;
            //_weather[9].Pop = 9;
            //_weather[10].Pop = 10;
            //_weather[11].Pop = 11;

            // Header
            _canvas.DrawText(_font, 3, 8, CColor.Blue, "NIEDERSCHLAG");

            s = string.Format("{0}%", _weather[0].Pop);
            len = _canvas.DrawText(_font, 0, 16, CColor.Black, s);
            x = 33 - len;
            _canvas.DrawText(_font, x, 16, CColor.Blue, s);
            _canvas.DrawText(_font, 36, 16, CColor.Blue, string.Format("{0:HH}", _weather[0].Time));
            _canvas.DrawText(_font, 47, 16, CColor.Blue, "Uhr");

            s = string.Format("{0}%", _weather[11].Pop);
            len = _canvas.DrawText(_font, 0, 23, CColor.Black, s);
            x = 33 - len;
            _canvas.DrawText(_font, x, 23, CColor.Blue, s);
            _canvas.DrawText(_font, 36, 23, CColor.Blue, string.Format("{0:HH}", _weather[11].Time));
            _canvas.DrawText(_font, 47, 23, CColor.Blue, "Uhr");

            // Pop
            x = 14;
            y = 57;
            _canvas.DrawLine(x, y, 59, y, CColor.DarkDarkGrey);
            for (int i = 0; i < 12; i++)
            {
                imax = Convert.ToInt32(Math.Round(Convert.ToDouble(_weather[i].Pop * 2 / 10)));
                for (int j = 1; j <= imax; j++)
                {
                    col = CColor.Blue;
                    if (j >= 0 && j <= 7) col = CColor.Green;
                    if (j > 7 && j <= 14) col = CColor.Yellow;
                    if (j > 14) col = CColor.Red;
                    y = 57 - j + 1;
                    _canvas.DrawLine(x + (i * 4), y, x + (i * 4) + 1, y, col);
                }
            }

            // 6 Clouds
            for (int i = 0; i < 12; i += 2)
            {
                //col = CColor.RoyalBlue;
                col = GetMyCloudColor(_weather[i].Weather_id);
                d = 0.5D * (_weather[i].Clouds + _weather[i + 1].Clouds);
                DrawClouds(14 + (i * 4), 25, d, col);
            }

            // 12 Clouds
            //for (int i = 0; i < 12; i++)
            //{
            //    x = 14 + (i * 4);
            //    y = 25;
            //    for (int j = 0; j < 10; j++)
            //    {
            //        _canvas.SetPixel(x, y, _weather[i].Clouds <= j * 10 ? CColor.DarkDarkGrey2 : CColor.PowderBlue);
            //        if (j % 2 == 0)
            //        {
            //            x++;
            //        }
            //        else
            //        {
            //            x--;
            //            y++;
            //        }
            //    }
            //}

            // Intensity
            x = 14;
            for (int i = 0; i < 12; i++)
            {
                intensity = GetMyRainfallIntensity(_weather[i].Weather_id);
                for (int j = 0; j < 5; j++)
                {
                    y = 36;
                    col = (_weather[i].Pop == 0 || (intensity <= j * 20)) ? CColor.DarkDarkGrey2 : CColor.NavyBlue;
                    _canvas.DrawLine(x + (i * 4), y - j, x + (i * 4) + 1, y - j, col);
                }
            }

            DrawPageMarker(2);
        }
        private void DrawClouds(int x, int y, double percent, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                _canvas.DrawLine(x + i, y, x + i, y + 5, CColor.DarkDarkGrey2);
            }
            if (percent >= 10D)
            {
                _canvas.DrawLine(x + 5, y, x + 5, y, color);
                _canvas.DrawLine(x + 4, y, x + 5, y + 1, color);
            }
            if (percent >= 20D)
            {
                _canvas.DrawLine(x + 3, y, x + 5, y + 2, color);
            }
            if (percent >= 30D)
            {
                _canvas.DrawLine(x + 2, y, x + 5, y + 3, color);
            }
            if (percent >= 40D)
            {
                _canvas.DrawLine(x + 1, y, x + 5, y + 4, color);
            }
            if (percent >= 50D)
            {
                _canvas.DrawLine(x, y, x + 5, y + 5, color);
            }
            if (percent >= 60D)
            {
                _canvas.DrawLine(x, y + 1, x + 4, y + 5, color);
            }
            if (percent >= 70D)
            {
                _canvas.DrawLine(x, y + 2, x + 3, y + 5, color);
            }
            if (percent >= 80D)
            {
                _canvas.DrawLine(x, y + 3, x + 2, y + 5, color);
            }
            if (percent >= 90D)
            {
                _canvas.DrawLine(x, y + 4, x + 1, y + 5, color);
                _canvas.DrawLine(x, y + 5, x, y + 5, color);
            }
        }

        private int GetMyRainfallIntensity(int weather_id)
        {
            idx = Array.FindIndex(_weather_ids, itm => itm.ID == weather_id);
            return idx >= 0 ? _weather_ids[idx].MyRainfallIntensity : 0;
        }

        private Color GetMyCloudColor(int weather_id)
        {
            idx = Array.FindIndex(_weather_ids, itm => itm.ID == weather_id);
            return idx >= 0 ? _weather_ids[idx].MyCloudColor : CColor.DarkDarkGrey2;
        }
        #endregion

        #region - DrawWind -
        private void DrawWind()
        {
            // Header
            _canvas.DrawText(_font, 8, 8, CColor.Blue, "WIND (Bft)");

            _canvas.DrawText(_font, 17, 16, CColor.Blue,
                (_weather[0].Wind_bft < 10 ? " " : "") + string.Format("{0}", _weather[0].Wind_bft));
            _canvas.DrawText(_font, 32, 16, CColor.Blue, string.Format("{0:HH}", _weather[0].Time));
            _canvas.DrawText(_font, 45, 16, CColor.Blue, "Uhr");

            _canvas.DrawText(_font, 17, 23, CColor.Blue,
                (_weather[11].Wind_bft < 10 ? " " : "") + string.Format("{0}", _weather[11].Wind_bft));
            _canvas.DrawText(_font, 32, 23, CColor.Blue, string.Format("{0:HH}", _weather[11].Time));
            _canvas.DrawText(_font, 45, 23, CColor.Blue, "Uhr");

            // Bft
            x = 14;
            y = 57;
            _canvas.DrawLine(x, y, 59, y, CColor.DarkDarkGrey);
            for (int i = 0; i < 12; i++)
            {
                imax = Convert.ToInt32(_weather[i].Wind_bft * 2);
                for (int j = 1; j <= imax; j++)
                {
                    col = CColor.Blue;
                    if (j >= 0 && j <= 8) col = CColor.Green; // bis 4 Bft
                    if (j > 8 && j <= 16) col = CColor.Yellow; // bis 8 Bft
                    if (j > 16) col = CColor.Red; // ab 9 Bft
                    y = 57 - j + 1;
                    _canvas.DrawLine(x + (i * 4), y, x + (i * 4) + 1, y, col);
                }
                // Windspitzen (Böen)
                y = Convert.ToInt32(_weather[i].Wind_gust_bft * 2) - imax;
                if (y > 0)
                {
                    _canvas.DrawLine(
                        x + (i * 4) + 1, 
                        57 - imax, 
                        x + (i * 4) + 1,
                        57 - imax - y + 1, 
                        CColor.DarkDarkGrey);
                }
            }

            // Windpfeile anhand Degree-Schnitt aus 2 Werten
            for (int i = 0; i < 12; i += 2)
            {
                //~ https://qastack.com.de/programming/491738/how-do-you-calculate-the-average-of-a-set-of-circular-data
                //~ Beispiele:
                //~ 0, 180 -> 90 (zwei Antworten dafür: Diese Gleichung übernimmt die Antwort im Uhrzeigersinn von a)
                //~ 180, 0 -> 270 (siehe oben)
                //~ 180, 1 -> 90,5
                //~ 1, 180 -> 90,5
                //~ 20, 350 -> 5
                //~ 350, 20 -> 5 (alle folgenden Beispiele kehren sich auch richtig um)
                //~ 10, 20 -> 15
                //~ 350, 2 -> 356
                //~ 359, 0 -> 359,5
                //~ 180, 180 -> 180
                d1 = _weather[i].Wind_deg;
                d2 = _weather[i + 1].Wind_deg;
                diff = (Convert.ToInt32((d1 - d2 + 180 + 360)) % 360) - 180;
                d = Convert.ToInt32(360 + d2 + (diff / 2)) % 360;

                // N, NO, O, SO, S, SW, W, NW
                if (d >= 0D && d < 22.5D) s = "N";
                if (d >= 22.5D && d < 67.5D) s = "NO";
                if (d >= 67.5D && d < 112.5D) s = "O";
                if (d >= 112.5D && d < 157.5D) s = "SO";
                if (d >= 157.5D && d < 202.5D) s = "S";
                if (d >= 202.5D && d < 247.5D) s = "SW";
                if (d >= 247.5D && d < 292.5D) s = "W";
                if (d >= 292.5D && d < 337.5D) s = "NW";
                if (d >= 337.5D && d <= 360.0D) s = "N";
                DrawWindArrow(14 + (i * 4), 25, s);
            }

            DrawPageMarker(3);
        }

        private void DrawWindArrow(int x, int y, string direction)
        {
            if (direction == "S")
            {
                // - - O - -
                // - O O O -
                // O - O - O
                // - - O - -
                // - - O - -
                _canvas.DrawLine(x + 2, y, x + 2, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 2, y, x, y + 2, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 2, y, x + 4, y + 2, CColor.LightSkyBlue);
            }
            if (direction == "SW")
            {
                // - O O O O
                // - - - O O
                // - - O - O
                // - O - - O
                // O - - - -
                _canvas.DrawLine(x + 4, y, x, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y, x + 1, y, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y, x + 4, y + 3, CColor.LightSkyBlue);
            }
            if (direction == "W")
            {
                // - - O - -
                // - - - O -
                // O O O O O
                // - - - O -
                // - - O - -
                _canvas.DrawLine(x + 4, y + 2, x, y + 2, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y + 2, x + 2, y, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y + 2, x + 2, y + 4, CColor.LightSkyBlue);
            }
            if (direction == "NW")
            {
                // O - - - -
                // - O - - O
                // - - O - O
                // - - - O O
                // - O O O O
                _canvas.DrawLine(x + 4, y + 4, x, y, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y + 4, x + 1, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 4, y + 4, x + 4, y + 1, CColor.LightSkyBlue);
            }
            if (direction == "N")
            {
                // - - O - -
                // - - O - -
                // O - O - O
                // - O O O -
                // - - O - -
                _canvas.DrawLine(x + 2, y + 4, x + 2, y, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 2, y + 4, x + 4, y + 2, CColor.LightSkyBlue);
                _canvas.DrawLine(x + 2, y + 4, x, y + 2, CColor.LightSkyBlue);
            }
            if (direction == "NO")
            {
                // - - - - O
                // O - - O -
                // O - O - -
                // O O - - -
                // O O O O -
                _canvas.DrawLine(x, y + 4, x + 4, y, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y + 4, x + 3, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y + 4, x, y + 1, CColor.LightSkyBlue);
            }
            if (direction == "O")
            {
                // - - O - -
                // - O - - -
                // O O O O O
                // - O - - -
                // - - O - -
                _canvas.DrawLine(x, y + 2, x + 4, y + 2, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y + 2, x + 2, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y + 2, x + 2, y, CColor.LightSkyBlue);
            }
            if (direction == "SO")
            {
                // O O O O -
                // O O - - -
                // O - O - -
                // O - - O -
                // - - - - O
                _canvas.DrawLine(x, y, x + 4, y + 4, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y, x, y + 3, CColor.LightSkyBlue);
                _canvas.DrawLine(x, y, x + 3, y, CColor.LightSkyBlue);
            }
        }
        #endregion

        #region - DrawSummary -
        private void DrawSummary()
        {
            _canvas.DrawText(_font, 15, 8, CColor.Blue, "MIN / MAX");
            _canvas.DrawText(_font, 15, 17, CColor.Blue, "T (°C):");
            _canvas.DrawText(_font, 20, 24, CColor.LightSkyBlue, string.Format("{0,3}/{1}", _tempMin, _tempMax));
            _canvas.DrawText(_font, 15, 33, CColor.Blue, "N (Proz):");
            _canvas.DrawText(_font, 20, 40, CColor.LightSkyBlue, string.Format("{0,3}/{1}", _rainMin, _rainMax));
            _canvas.DrawText(_font, 15, 49, CColor.Blue, "W (Bft):");
            _canvas.DrawText(_font, 20, 56, CColor.LightSkyBlue, string.Format("{0,3}/{1}", _windMin, _windMax));

            DrawPageMarker(4);
        }
        #endregion

        private void Draw7DaysPreview()
        {           
            // Header
            _canvas.DrawText(_font, 21, 8, CColor.Blue, "7 TAGE");

            // T
            _canvas.DrawText(_font, 15, 20, CColor.Blue, "T");
            _canvas.DrawLine(21, 17, 22, 17, CColor.DarkDarkGrey2);

            // N
            _canvas.DrawText(_font, 15, 35, CColor.Blue, "N");
            _canvas.DrawLine(21, 32, 22, 32, CColor.DarkDarkGrey2);

            // W
            _canvas.DrawText(_font, 15, 49, CColor.Blue, "W");
            _canvas.DrawLine(21, 46, 22, 46, CColor.DarkDarkGrey2);

            // Vertikales Gitter
            x = 22;
            for (int i = 0; i < 8; i++)
            {
                _canvas.DrawLine(
                    x + (i * 5),
                    57,
                    x + (i * 5),
                    9,
                    CColor.DarkDarkGrey2);
            }

            // T + N + W
            x = 22;
            for (int i = 1; i < 8; i++)
            {
                _canvas.DrawLine(
                    x + ((i - 1) * 5),
                    Convert.ToInt32(Math.Round(_t7days[i - 1])),
                    x + (i * 5),
                    Convert.ToInt32(Math.Round(_t7days[i])),
                    CColor.RoyalBlue);
                _canvas.DrawLine(
                    x + ((i - 1) * 5),
                    Convert.ToInt32(Math.Round(_n7days[i - 1])),
                    x + (i * 5),
                    Convert.ToInt32(Math.Round(_n7days[i])),
                    CColor.RoyalBlue);
                _canvas.DrawLine(
                    x + ((i - 1) * 5),
                    Convert.ToInt32(Math.Round(_w7days[i - 1])),
                    x + (i * 5),
                    Convert.ToInt32(Math.Round(_w7days[i])),
                    CColor.RoyalBlue);
            }

            // Windrichtung
            for (int i = 12, j = 0; i <= 19; i++, j++)
            {
                DrawDailyWindDirection(j, _weather[i].Wind_deg);
            }

            DrawPageMarker(5);
        }


        private void DrawDailyWindDirection(int day, int d)
        {
            col = CColor.RoyalBlue;
            // N
            if ((d >= 0D && d < 22.5D) || (d >= 337.5D && d <= 360.0D))
            {
                // - - -
                // O - O
                // - O -
                _canvas.DrawLine(
                    21 + (day * 5),
                    56,
                    22 + (day * 5),
                    57,
                    col);
                _canvas.DrawLine(
                    23 + (day * 5),
                    56,
                    22 + (day * 5),
                    57,
                    col);
            }
            // NO
            if (d >= 22.5D && d < 67.5D)
            {
                // - - -
                // O - -
                // O O -
                _canvas.DrawLine(
                    21 + (day * 5),
                    56,
                    21 + (day * 5),
                    57,
                    col);
                _canvas.DrawLine(
                    22 + (day * 5),
                    57,
                    21 + (day * 5),
                    57,
                    col);
            }
            // O
            if (d >= 67.5D && d < 112.5D)
            {
                // - O -
                // O - -
                // - O -
                _canvas.DrawLine(
                    22 + (day * 5),
                    55,
                    21 + (day * 5),
                    56,
                    col);
                _canvas.DrawLine(
                    22 + (day * 5),
                    57,
                    21 + (day * 5),
                    56,
                    col);
            }
            // SO
            if (d >= 112.5D && d < 157.5D)
            {
                // O O -
                // O - -
                // - - -
                _canvas.DrawLine(
                    22 + (day * 5),
                    55,
                    21 + (day * 5),
                    55,
                    col);
                _canvas.DrawLine(
                    21 + (day * 5),
                    56,
                    21 + (day * 5),
                    55,
                    col);
            }
            // S
            if (d >= 157.5D && d < 202.5D)
            {
                // - O -
                // O - O
                // - - -
                _canvas.DrawLine(
                    21 + (day * 5),
                    56,
                    22 + (day * 5),
                    55,
                    col);
                _canvas.DrawLine(
                    23 + (day * 5),
                    56,
                    22 + (day * 5),
                    55,
                    col);
            }
            // SW
            if (d >= 202.5D && d < 247.5D)
            {
                // - O O
                // - - O
                // - - -
                _canvas.DrawLine(
                    22 + (day * 5),
                    55,
                    23 + (day * 5),
                    55,
                    col);
                _canvas.DrawLine(
                    23 + (day * 5),
                    56,
                    23 + (day * 5),
                    55,
                    col);
            }
            // W
            if (d >= 247.5D && d < 292.5D)
            {
                // - O -
                // - - O
                // - O -
                _canvas.DrawLine(
                    22 + (day * 5),
                    55,
                    23 + (day * 5),
                    56,
                    col);
                _canvas.DrawLine(
                    22 + (day * 5),
                    57,
                    23 + (day * 5),
                    56,
                    col);
            }
            // SW
            if (d >= 292.5D && d < 337.5D)
            {
                // - - -
                // - - O
                // - O O
                _canvas.DrawLine(
                    23 + (day * 5),
                    56,
                    23 + (day * 5),
                    57,
                    col);
                _canvas.DrawLine(
                    22 + (day * 5),
                    57,
                    23 + (day * 5),
                    57,
                    col);
            }
        }

        #region - DrawPageMarker -
        private void DrawPageMarker(int number)
        {
            x = 28;
            y = 60;
            for (int i = 1; i <= 5; i++)
            {
                col = number == i ? CColor.Blue : CColor.DarkDarkGrey;
                _canvas.DrawLine(x, y, x + 1, y, col);
                _canvas.DrawLine(x, y - 1, x + 1, y - 1, col);
                x += 3;
            }
        }
        #endregion

        #region - private else -
        private void DebugPrint(string aText)
        {
            CGeneral.SetTextToFile(_weatherLogFile, string.Format("*** {0:yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, aText), true);
        }
        #endregion
    }
}
