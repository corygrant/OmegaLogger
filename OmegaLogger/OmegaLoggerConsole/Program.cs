using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaLoggerConsole
{
    internal class Program
    {
        static SerialPort _sp;

        static void Main(string[] args)
        {
            _sp = new SerialPort();
            _sp.PortName = "COM6";
            _sp.BaudRate = 19200;
            _sp.Parity = Parity.None;
            _sp.DataBits = 8;
            _sp.StopBits = StopBits.One;
            _sp.Handshake = Handshake.None;

            _sp.ReadTimeout = 3000;
            _sp.WriteTimeout = 1000;

            _sp.Open();

            Reading[] readings = new Reading[] { new Reading(), new Reading(), new Reading(), new Reading() };
            var allReadings = new AllReadings();

            var startTick = DateTime.Now.Ticks;

            var filename = String.Format("TempReadings_{0}{1}{2}_{3}{4}{5}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            using (var writer = new StreamWriter(String.Format("D:\\Dingo Electronics\\Omega_HH1394_Thermometer\\Logs\\{0}.csv", filename)))
            using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<AllReadings>();
                csv.NextRecord();

                int index = 0;
                byte[] bytes = new byte[14];
                while (true)
                {
                    _sp.NewLine = Convert.ToString(0x03);
                    var thisByte = _sp.ReadByte();

                    if (thisByte != -1)
                    {
                        bytes[index] = Convert.ToByte(thisByte);
                        index++;

                        if (index > bytes.Length - 1) index = 0;

                        //EOL
                        if (thisByte == 0x03)
                        {
                            int sensor = bytes[1] & 0x03;
                            double dec = bytes[6];
                            dec = dec / 10;
                            double temp = bytes[5] + dec;

                            Console.WriteLine(String.Format("T{0}: {1:F} @ {2}", sensor, temp, DateTime.Now.ToString()));

                            if ((sensor >= 0) && (sensor < 4))
                            {
                                readings[sensor].Id = sensor;
                                readings[sensor].Value = temp;
                            }

                            var elapsedTime = new TimeSpan(DateTime.Now.Ticks - startTick);
                            allReadings.Ticks = elapsedTime.Ticks;
                            allReadings.Hour = elapsedTime.Hours;
                            allReadings.Minute = elapsedTime.Minutes;
                            allReadings.Second = elapsedTime.Seconds;
                            allReadings.T1 = readings[0].Value;
                            allReadings.T2 = readings[1].Value;
                            allReadings.T3 = readings[2].Value;
                            allReadings.T4 = readings[3].Value;
                            csv.WriteRecord(allReadings);
                            csv.NextRecord();
                            csv.Flush();

                            index = 0;
                        }
                    }
                }
            }
        }

        public class Reading
        {
            public int Id { get; set; }
            public double Value { get; set; }
        }

        public class AllReadings
        {
            public long Ticks { get; set; }
            public int Hour { get; set; }
            public int Minute { get; set; }
            public int Second { get; set; }
            public double T1 { get; set; }
            public double T2 { get; set; }
            public double T3 { get; set; }
            public double T4 { get; set; }
        }
    }
}
