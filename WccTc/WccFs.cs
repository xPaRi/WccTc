using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcPluginBase;
using TcPluginBase.FileSystem;
using WccTc;

namespace WccTC
{
    /// <summary>
    /// File System for White Cat Console (ESP32 with LUA)
    /// </summary>
    /// <remarks>
    /// https://github.com/r-Larch/TcBuild
    /// </remarks>
    public class WccFs : FsPlugin
    {
        public WccFs(Settings pluginSettings)
            : base(pluginSettings)
        {
            if (string.IsNullOrEmpty(Title))
                Title = "WccFs";

            MyLog($"{Title}, ver. {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
        }


        public override IEnumerable<FindData> GetFiles(RemotePath path)
        {
            MyLog("GetFiles()", path);

            try
            {
                switch (path)
                {
                    case var pth when !path.HasValue:
                        return new List<FindData> { new FindData("<Path not specified>") };

                    case var pth when path.Level == 0:
                        var portList = GetPorts();

                        if (portList.Count > 0)
                            return portList;

                        return new List<FindData> { new FindData("<No COM port found>", FileAttributes.Offline) };

                    default:
                        {
                            var result = GetFileAndDirectories(path);

                            if (result.Count == 0)
                            {
                                result.Add(new FindData("")); //musí to tu být, jinak nejde přepnout do adresáře, ani jej smazat
                            }

                            return result;
                        }
                }
            }
            catch (Exception ex)
            {
                MyLog(ex);
                return new List<FindData> { new FindData("<Error>", FileAttributes.ReparsePoint) };
            }
        }


        /// <summary>
        /// Copy to ESP
        /// </summary>
        /// <param name="remoteName"></param>
        /// <param name="localName"></param>
        /// <param name="copyFlags"></param>
        /// <param name="remoteInfo"></param>
        /// <returns></returns>
        public override FileSystemExitCode GetFile(RemotePath remoteName, string localName, CopyFlags copyFlags, RemoteInfo remoteInfo)
        {
            MyLog($"GetFile ('{localName}')", remoteName);

            WccCall($"-p {remoteName.GetPort()} -down {remoteName.GetPathWithoutPort()} {localName}");

            return FileSystemExitCode.OK; //return base.GetFile(remoteName, localName, copyFlags, remoteInfo);
        }

        /// <summary>
        /// Download from ESP
        /// </summary>
        /// <param name="localName"></param>
        /// <param name="remoteName"></param>
        /// <param name="copyFlags"></param>
        /// <returns></returns>
        public override FileSystemExitCode PutFile(string localName, RemotePath remoteName, CopyFlags copyFlags)
        {
            MyLog($"PutFile ('{localName}')", remoteName);

            WccCall($"-p {remoteName.GetPort()} -up {localName} {remoteName.GetPathWithoutPort()}");

            return FileSystemExitCode.OK; //return base.PutFile(localName, remoteName, copyFlags);
        }


        public override bool MkDir(RemotePath dir)
        {
            MyLog("MkDir()", dir);

            var result = ComCall(dir.GetPort(), $"os.mkdir('{dir.GetPathWithoutPort()}')\r\r");

            return result.StartsWith("true", StringComparison.OrdinalIgnoreCase);
        }

        public override bool DeleteFile(RemotePath fileName)
        {
            MyLog("DeleteFile()", fileName);

            if (string.IsNullOrEmpty(fileName))
            {
                MyLog("DeleteFile (no exists file");
                return true;
            }

            var result = ComCall(fileName.GetPort(), $"os.remove('{fileName.GetPathWithoutPort()}')\r\r");

            return result.StartsWith("true", StringComparison.OrdinalIgnoreCase);
        }

        public override bool RemoveDir(RemotePath dirName)
        {
            MyLog("RemoveDir()", dirName);

            var result = ComCall(dirName.GetPort(), $"os.remove('{dirName.GetPathWithoutPort()}')\r\r");

            return result.StartsWith("true", StringComparison.OrdinalIgnoreCase);
        }


        #region Override stack

        public override string RootName
        {
            get => base.RootName;
            set => base.RootName = value;
        }

        public override Task<FileSystemExitCode> GetFileAsync(RemotePath remoteName, string localName, CopyFlags copyFlags, RemoteInfo remoteInfo, Action<int> setProgress, CancellationToken token)
        {
            return base.GetFileAsync(remoteName, localName, copyFlags, remoteInfo, setProgress, token);
        }

        public override Task<FileSystemExitCode> PutFileAsync(string localName, RemotePath remoteName, CopyFlags copyFlags, Action<int> setProgress, CancellationToken token)
        {
            return base.PutFileAsync(localName, remoteName, copyFlags, setProgress, token);
        }

        public override FileSystemExitCode RenMovFile(RemotePath oldName, RemotePath newName, bool move, bool overwrite, RemoteInfo remoteInfo)
        {
            return base.RenMovFile(oldName, newName, move, overwrite, remoteInfo);
        }

        public override ExecResult ExecuteOpen(TcWindow mainWin, RemotePath remoteName)
        {
            return base.ExecuteOpen(mainWin, remoteName);
        }

        public override ExecResult ExecuteProperties(TcWindow mainWin, RemotePath remoteName)
        {
            return base.ExecuteProperties(mainWin, remoteName);
        }

        public override ExecResult ExecuteCommand(TcWindow mainWin, RemotePath remoteName, string command)
        {
            return base.ExecuteCommand(mainWin, remoteName, command);
        }

        public override bool SetAttr(RemotePath remoteName, FileAttributes attr)
        {
            return base.SetAttr(remoteName, attr);
        }

        public override bool SetTime(RemotePath remoteName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
        {
            return base.SetTime(remoteName, creationTime, lastAccessTime, lastWriteTime);
        }

        public override bool Disconnect(RemotePath disconnectRoot)
        {
            MyLog("Disconnect (disconnectRoot)", disconnectRoot);

            return base.Disconnect(disconnectRoot);
        }

        public override void StatusInfo(string remoteDir, InfoStartEnd startEnd, InfoOperation infoOperation)
        {
            MyLog($"StatusInfo ({remoteDir}; {startEnd}; {infoOperation})");

            base.StatusInfo(remoteDir, startEnd, infoOperation);
        }

        public override ExtractIconResult ExtractCustomIcon(RemotePath remoteName, ExtractIconFlags extractFlags)
        {
            return base.ExtractCustomIcon(remoteName, extractFlags);
        }

        public override PreviewBitmapResult GetPreviewBitmap(RemotePath remoteName, int width, int height)
        {
            return base.GetPreviewBitmap(remoteName, width, height);
        }

        public override string GetLocalName(RemotePath remoteName, int maxLen)
        {
            MyLog($"GetLocalName({maxLen})", remoteName);

            return base.GetLocalName(remoteName, maxLen);
        }

        public override object InitializeLifetimeService()
        {
            return base.InitializeLifetimeService();
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            return base.CreateObjRef(requestedType);
        }

        public override int OnTcPluginEvent(PluginEventArgs e)
        {
            return base.OnTcPluginEvent(e);
        }

        public override string TraceTitle { get; }

        protected override bool ProgressProc(string source, string destination, int percentDone)
        {
            MyLog($"ProgressProc {source}; {destination}; {percentDone}");

            return base.ProgressProc(source, destination, percentDone);
        }

        public override object FindFirst(RemotePath path, out FindData findData)
        {
            return base.FindFirst(path, out findData);
        }

        public override bool FindNext(ref object o, out FindData findData)
        {
            return base.FindNext(ref o, out findData);
        }

        public override int FindClose(object o)
        {
            return base.FindClose(o);
        }

        public override string ToString()
        {
            MyLog($"ToString: {base.ToString()}");
            return base.ToString();
        }

        #endregion

        #region Helpers

        private string ComCall(string port, string command)
        {
            MyLog($"ComCall (port: '{port}'; command: '{command}')");

            using (var serialPort = new SerialPort(port, 115200)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                Handshake = Handshake.XOnXOff
            })
            {
                var result = string.Empty;

                serialPort.Open();

                serialPort.WriteLine($"\r{command}')\r");

                //--- Odpověď z ESP
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        result = serialPort.ReadLine();
                        
                        if (result.StartsWith("true", StringComparison.OrdinalIgnoreCase) || result.StartsWith("nil", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {}
                }
                //---

                serialPort.Close();

                MyLog($"ComCall (result: '{result}'");

                return result;
            }
        }

        private string WccCall(string arguments)
        {
            MyLog($"WccCall('{arguments}')");

            const int TIMEOUT = 180000; //vyčasování 180 sec

            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "wcc.exe",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

            if (process == null)
                throw new Exception("wcc.exe not found.");

            var output = process.StandardOutput;
            var errput = process.StandardError;

            process.WaitForExit(TIMEOUT);

            if (!process.HasExited)
                throw new Exception($"wcc.exe process timeout. (timeout: {TIMEOUT} ms)");

            var outString = output.ReadToEnd();
            var errString = errput.ReadToEnd();

            if (!string.IsNullOrEmpty(errString))
            {
                throw new Exception(errString);
            }

            MyLog("WccCall (outString)", outString);

            return outString;
        }

        /// <summary>
        /// Načte seznam portů.
        /// Mají atribut adresářů.
        /// </summary>
        /// <returns></returns>
        private List<FindData> GetPorts()
        {
            return WccCall("-ports")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(it => it.StartsWith("COM"))
                .Select(it => new FindData(it, FileAttributes.Directory))
                .ToList();
        }

        /// <summary>
        /// Načte seznam souborů a adresářů.
        /// </summary>
        /// <returns></returns>
        private List<FindData> GetFileAndDirectories(RemotePath path)
        {
            return WccCall($"-p {path.GetPort()} -ls {path.GetPathWithoutPort()}/")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(CreateFindData)
                .ToList();
        }

        /// <summary>
        /// Vrací položku 
        /// </summary>
        /// <param name="line"></param>
        private static FindData CreateFindData(string line)
        {
            var lineArray = line.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            switch (lineArray)
            {
                case var la when la.Length != 3:
                    return new FindData("<unsupported line format>");

                case var la when la[0].Equals("d", StringComparison.OrdinalIgnoreCase):
                    return new FindData(la[2], FileAttributes.Directory);

                case var la when la[0].Equals("f", StringComparison.InvariantCultureIgnoreCase):
                    ulong.TryParse(la[1], out var fileSize);
                    return new FindData(la[2], fileSize, FileAttributes.Normal);

                default:
                    return new FindData($"<unsupported file type '{lineArray[0]}'>");
            }
        }


        private void MyLog(Exception ex)
        {
            MyLog(ex.ToString());
        }

        private void MyLog(string title, string contents)
        {
            MyLog($"{title}:");
            MyLog(contents);
        }

        private void MyLog(string title, RemotePath path)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Path ({title})");
            sb.AppendLine($"  Port:      '{path.GetPort()}'");
            sb.AppendLine($"  Level:     '{path.Level}'");
            sb.AppendLine($"  Directory: '{path.Directory}'");
            sb.AppendLine($"  Extension: '{path.Extension}'");
            sb.AppendLine($"  FileName:  '{path.FileName}'");
            sb.AppendLine($"  FileNameWithoutExtension: '{path.FileNameWithoutExtension}'");
            sb.AppendLine($"  HasValue:  {path.HasValue}");
            sb.AppendLine($"  Path:      '{path.Path}'");
            sb.AppendLine($"  GetPathWithoutPort:       '{path.GetPathWithoutPort()}'");
            sb.AppendLine($"  PathWithoutTrailingSlash: '{path.PathWithoutTrailingSlash}'");
            sb.AppendLine($"  TrailingSlash:   {path.TrailingSlash}");
            sb.AppendLine($"  Segments.Length: {path.Segments.Length}");

            for (var i = 0; i < path.Segments.Length; i++)
            {
                sb.AppendLine($"    [{i}]: '{path.Segments[i]}'");
            }

            MyLog(sb.ToString());
        }

        private void MyLog(string contents)
        {
#if DEBUG
            var fullPath = Path.Combine(PluginFolder, "WccFs.log");

            File.AppendAllLines($@"{fullPath}", new List<string> { contents });

            Log.Debug(contents);

            Debug.WriteLine(contents);
#endif
        }

        #endregion
    }
}
