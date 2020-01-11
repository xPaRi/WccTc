using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcPluginBase.FileSystem;
using WccTC;
using System.IO.Ports;

namespace WccTcDevConsole
{
    internal class Program
    {
        static SerialPort _SerialPort;
        static bool _Continue;

        private static void Main(string[] args)
        {
            //Test1("/com7");
            //Test1("/com7/www");
            //Test1("/com7/test");
            //Test2();
            //Test3("test6");
            //Test4("test6");

            Console.ReadKey();
        }

        private static void Test1(string path)
        {
            var setting = new TcPluginBase.Settings();

            var wccTc = new WccFs(setting);

            Console.WriteLine($"--- GetFiles({path}) ---");
            foreach (var item in wccTc.GetFiles(new RemotePath(path)).OrderBy(it => it.Attributes).ThenBy(it => it.FileName))
            {
                if (item.Attributes == FileAttributes.Directory)
                    Console.WriteLine($"[{item.FileName}]");
                else
                    Console.WriteLine($"{item.FileSize,6} {item.FileName}");
            }
            Console.WriteLine("---");
        }

        private static void Test2()
        {
            TermCall("COM7", 115200);
        }

        private static void TermCall(string portName, int baudRate)
        {
            string message;
            Thread readThread = new Thread(Read);

            _SerialPort = new SerialPort("COM7", 115200)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                Handshake = Handshake.XOnXOff
            };

            _Continue = true;
            _SerialPort.Open();
            
            readThread.Start();

            Console.WriteLine("Type QUIT to exit");

            while (_Continue)
            {
                message = Console.ReadLine();

                if (message.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    _Continue = false;
                }
                else
                {
                    _SerialPort.WriteLine(message + "\r");
                }
            }

            readThread.Join();
            _SerialPort.Close();
            _SerialPort.Dispose();
        }


        public static void Read()
        {
            while (_Continue)
            {
                try
                {
                    var message = _SerialPort.ReadLine();
                    Console.WriteLine(message);
                }
                catch (TimeoutException) 
                {
                    //Console.WriteLine("TimeoutException");
                }
            }
        }

        private static void Test3(string dir)
        {
            var serialPort = new SerialPort("COM7", 115200)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                Handshake = Handshake.XOnXOff
            };

            serialPort.Open();

            SendToEsp(serialPort, $"os.mkdir('{dir}')\r\r");

            serialPort.Close();
            serialPort.Dispose();
        }

        private static void Test4(string dir)
        {
            var serialPort = new SerialPort("COM7", 115200)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                Handshake = Handshake.XOnXOff
            };

            serialPort.Open();

            SendToEsp(serialPort, $"os.remove('{dir}')\r\r");

            serialPort.Close();
            serialPort.Dispose();
        }

        private static void SendToEsp(SerialPort serialPort, string sndText)
        {
            serialPort.WriteLine(sndText);
            
            //--- Odpověď z ESP
            for(var i=0; i<10; i++)
            {
                try
                {
                    var recText = serialPort.ReadLine();
                    Console.WriteLine(recText);
                    if (recText.StartsWith("true", StringComparison.OrdinalIgnoreCase) || recText.StartsWith("nil", StringComparison.OrdinalIgnoreCase))
                        return;
                }
                catch(TimeoutException)
                {
                    Console.Write(".");
                }
            }
            //---
        }
    }
}
