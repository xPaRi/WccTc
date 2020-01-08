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

namespace WccTcDevConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //var pl = GetPorts();

            //foreach (var item in pl)
            //{
            //    Console.WriteLine(item);
            //}

            var setting = new TcPluginBase.Settings();

            var wccTc = new WccFs(setting);


            Console.WriteLine("--- GetFiles() ---");
            foreach (var item in wccTc.GetFiles(new RemotePath("/")))
            {
                Console.WriteLine(item.FileName);
            }


            Console.ReadKey();

            
        }

    }
}
