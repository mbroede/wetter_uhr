// =====================================================================
// File:          openweather_classes.cs
// Author:        Michael Bröde
// Created:       02.04.2019
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using help_classes;

namespace openweather_classes
{
    // =================================================================
    // CWeather
    // =================================================================
    class CWeather
    {
        public string name { get; set; }
        public DateTime dt { get; set; }
        public double temp { get; set; }
        public double clouds { get; set; }
        public double pop { get; set; } // Probability of precipitation = Niederschlagswahrscheinlichkeit
        public double wind_speed { get; set; }
        public double wind_deg { get; set; }
        public double wind_gust { get; set; }
        public int weather_id { get; set; }
        public string weather_main { get; set; }
        public string weather_description { get; set; }

        public string csv
        {
            get
            {
                return string.Format("{0};{1:dd.MM.yyyy HH:mm:ss};{2};{3};{4};{5};{6};{7};{8};{9};{10}",
                    name, dt, temp, clouds, pop, wind_speed, wind_deg, wind_gust, weather_id, weather_main, weather_description);
            }
        }
    }

    // =================================================================
    // COpenWeather
    // =================================================================
    class COpenWeather
    {
        private string _csvfile;
        private string _csvbakfile;
        private string _logfile;
        private string _errjsonfile;
        private string _responsejsonfile;
        private string _url;
        private string _json; // Response
        private string _err;
        private readonly int _waitseconds = 1000 * 60 * 10; // call/check cycle

        public COpenWeather()
        {
            _csvfile = Path.Combine(CGeneral.OpenWeatherDir, "weather.csv");
            _csvbakfile = Path.Combine(CGeneral.OpenWeatherDir, "weather_{0:yyyyMMdd_HHmmss}.csv");
            _logfile = Path.Combine(CGeneral.OpenWeatherDir, "weather_log.txt");
            _responsejsonfile = Path.Combine(CGeneral.OpenWeatherDir, "response_{0:yyyyMMdd_HHmmss}.json");
            _errjsonfile = Path.Combine(CGeneral.OpenWeatherDir, "err_{0:yyyyMMdd_HHmmss}.json");
            _err = string.Empty;
            string call = "https://api.openweathermap.org/data/2.5/onecall";
            string lat = "54.088490080295884";
            string lon = "12.129193471429586";
            string appid = "< API key >";
            string exclude = "alerts,minutely";
            string units = "metric";
            string lang = "de";
            _url = string.Format(@"{0}?lat={1}&lon={2}&exclude={3}&appid={4}&units={5}&lang={6}",
                call, lat, lon, exclude, appid, units, lang);
        }

        public void Run()
        {
            for (; ; )
            {
                if (GetWeatherRequired())
                {
                    GetWeatherFromURL();
                    if (!string.IsNullOrEmpty(_json))
                    {
                        CreateCSV();
                        if (string.IsNullOrEmpty(_err))
                        {
                            DebugPrint("csv created");
                            //CGeneral.SetTextToFile(string.Format(_responsejsonfile, DateTime.Now), _json);
                        }
                        else
                        {
                            DebugPrint(string.Concat("Creating csv went wrong: ", _err));
                            CGeneral.SetTextToFile(string.Format(_errjsonfile, DateTime.Now), _json);
                        }
                    }
                    else
                    {
                        DebugPrint(string.Concat("Reading url went wrong: ", _err));
                    }
                }
                Thread.Sleep(_waitseconds);
            }
        }

        private bool GetWeatherRequired()
        {
            try
            {
                FileInfo fi = new FileInfo(_csvfile);
                if (fi.LastWriteTime.Hour == DateTime.Now.Hour && fi.LastWriteTime.Day == DateTime.Now.Day)
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
            return true;
        }

        private void GetWeatherFromURL()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;
                    if (response.CharacterSet == null)
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    _json = readStream.ReadToEnd();
                    response.Close();
                    readStream.Close();
                }
            }
            catch (Exception ex)
            {
                _err = ex.Message;
                _json = string.Empty;
            }
        }

        private void CreateCSV()
        {
            _err = string.Empty;
            try
            {
                string sCurrent = GetInnerJson(_json, "current", '{', '}');
                if (string.IsNullOrEmpty(sCurrent))
                {
                    throw new Exception("Json Error: 'current' is empty");
                }
                string sHourly = GetInnerJson(_json, "hourly", '[', ']');
                if (string.IsNullOrEmpty(sHourly))
                {
                    throw new Exception("Json Error: 'hourly' is empty");
                }
                string sDaily = GetInnerJson(_json, "daily", '[', ']');
                if (string.IsNullOrEmpty(sDaily))
                {
                    throw new Exception("Json Error: 'daily' is empty");
                }
                string s;

                List<string> lstHourly = new List<string>();
                for (int i = 0; i < 10000; i++)
                {
                    s = GetInnerJson(sHourly, "", '{', '}');
                    if (!string.IsNullOrEmpty(s))
                    {
                        lstHourly.Add(s);
                        sHourly = sHourly.Replace(s, "");
                        sHourly = sHourly.Replace("{}", "");
                    }
                    else
                    {
                        break;
                    }
                }
                List<string> lstDaily = new List<string>();
                for (int i = 0; i < 10000; i++)
                {
                    s = GetInnerJson(sDaily, "", '{', '}');
                    if (!string.IsNullOrEmpty(s))
                    {
                        lstDaily.Add(s);
                        sDaily = sDaily.Replace(s, "");
                        sDaily = sDaily.Replace("{}", "");
                    }
                    else
                    {
                        break;
                    }
                }

                List<CWeather> lstWeather = new List<CWeather>();
                CWeather w = new CWeather();
                SetWeather(sCurrent, "current", ref w);
                lstWeather.Add(w);

                foreach (string hour in lstHourly)
                {
                    w = new CWeather();
                    SetWeather(hour, "hourly", ref w);
                    lstWeather.Add(w);
                }

                foreach (string day in lstDaily)
                {
                    w = new CWeather();
                    SetWeather(day, "daily", ref w);
                    lstWeather.Add(w);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("name;dt_utc;temp;clouds;pop;wind_speed;wind_deg;wind_gust;weather_id;weather_main;weather_description");
                foreach (var item in lstWeather)
                {
                    sb.AppendLine(item.csv);
                }
                CGeneral.SetTextToFile(_csvfile, sb.ToString());
                CGeneral.SetTextToFile(string.Format(_csvbakfile, DateTime.Now), sb.ToString());
            }
            catch (Exception ex)
            {
                _err = ex.Message;
            }
        }

        private void SetWeather(string json, string name, ref CWeather w)
        {
            string s;
            double d, d2;
            // name
            w.name = name;
            // dt
            s = GetStringValue(json, "dt");
            w.dt = UnixTimestampToDateTime(Convert.ToDouble(s));
            // temp
            if (name == "current" || name == "hourly")
            {
                w.temp = GetDoubleValue(json, "temp");
            }
            else
            {
                // "daily"
                s = GetInnerJson(json, "temp", '{', '}');
                d = GetDoubleValue(s, "min");
                d2 = GetDoubleValue(s, "max");
                w.temp = (d + d2) / 2D;
            }
            // clouds;
            w.clouds = GetDoubleValue(json, "clouds");
            // pop;
            if (name == "hourly" || name == "daily")
            {
                w.pop = GetDoubleValue(json, "pop");
            }
            else
            {
                w.pop = 0;
            }
            // wind_speed;
            w.wind_speed = GetDoubleValue(json, "wind_speed");
            // wind_deg;
            w.wind_deg = GetDoubleValue(json, "wind_deg");
            // wind_gust;
            w.wind_gust = GetDoubleValue(json, "wind_gust");
            // weather_id;
            s = GetInnerJson(json, "weather", '{', '}');
            w.weather_id = GetIntValue(s, "id");
            // weather_main;
            w.weather_main = GetStringValue(s, "main");
            // weather_description;
            w.weather_description = GetStringValue(s, "description");
        }

        private double GetDoubleValue(string json, string tag)
        {
            string s = GetStringValue(json, tag);
            return Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture); // "." als Dezimalzeichen
        }

        private int GetIntValue(string json, string tag)
        {
            string s = GetStringValue(json, tag);
            return Convert.ToInt32(s);
        }

        private string GetStringValue(string json, string tag)
        {
            int pos = json.IndexOf((char)34 + tag + (char)34);
            if (pos == -1)
            {
                return string.Empty;
            }
            int start = json.IndexOf(':', pos);
            if (start == -1)
            {
                return string.Empty;
            }
            start += 1;
            string s = json.Substring(start);
            pos = s.IndexOf(',');
            if (pos == -1) pos = s.IndexOf('}');
            if (pos == -1) pos = s.IndexOf(']');
            if (pos > 0)
            {
                return s.Substring(0, pos).Trim(new char[] { ' ', '"' });
            }
            else
            {
                return s.Substring(0).Trim(new char[] { ' ', '"' });
            }
        }

        private string GetInnerJson(string json, string tag, char cleft, char cright)
        {
            int pos = string.IsNullOrEmpty(tag) ? 0 : json.IndexOf((char)34 + tag + (char)34);
            if (pos == -1)
            {
                return string.Empty;
            }
            int start = json.IndexOf(cleft, pos);
            if (start == -1)
            {
                return string.Empty;
            }
            int stop = 0;
            int n = 1;
            start += 1;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == cleft)
                {
                    n++;
                }
                if (json[i] == cright)
                {
                    n--;
                    if (n == 0)
                    {
                        stop = i;
                        break;
                    }
                }
            }
            return stop > start ? json.Substring(start, stop - start) : json.Substring(start);
        }

        private DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
        }

        private void DebugPrint(string aText)
        {
            CGeneral.SetTextToFile(_logfile, string.Format("*** {0:yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, aText), true);
        }
    }
}
