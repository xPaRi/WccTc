using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcPluginBase;
using TcPluginBase.FileSystem;

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
            if (String.IsNullOrEmpty(Title))
                Title = "WccFs";
        }


        public override IEnumerable<FindData> GetFiles(RemotePath path)
        {
            if (!path.HasValue)
            {
                return new List<FindData>()
                {
                    new FindData("<Path not specified>")
                };
            }

            if (path.Level == 0)
            {
                var portList = GetPorts();

                if (portList.Count > 0)
                    return portList; 
                
                return new List<FindData>(){new FindData("<No COM port found>", FileAttributes.Offline)};
            }

            var port = path.Segments[0];
            var directory = "";

            if (path.Segments.Length > 1)
                directory = string.Join("/", path.Segments, 1, path.Segments.Length - 1);

            return GetFileAndDirectories(port, directory);
        }

        public override string RootName
        {
            get { return "Root Name"; }
            set {}
        }


        #region Override stack

        public override Task<FileSystemExitCode> GetFileAsync(RemotePath remoteName, string localName, CopyFlags copyFlags, RemoteInfo remoteInfo,
            Action<int> setProgress, CancellationToken token)
        {
            return base.GetFileAsync(remoteName, localName, copyFlags, remoteInfo, setProgress, token);
        }

        public override Task<FileSystemExitCode> PutFileAsync(string localName, RemotePath remoteName, CopyFlags copyFlags, Action<int> setProgress,
            CancellationToken token)
        {
            return base.PutFileAsync(localName, remoteName, copyFlags, setProgress, token);
        }

        public override FileSystemExitCode RenMovFile(RemotePath oldName, RemotePath newName, bool move, bool overwrite, RemoteInfo remoteInfo)
        {
            return base.RenMovFile(oldName, newName, move, overwrite, remoteInfo);
        }

        public override bool DeleteFile(RemotePath fileName)
        {
            return base.DeleteFile(fileName);
        }

        public override bool RemoveDir(RemotePath dirName)
        {
            return base.RemoveDir(dirName);
        }

        public override bool MkDir(RemotePath dir)
        {
            return base.MkDir(dir);
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
            return base.Disconnect(disconnectRoot);
        }

        public override void StatusInfo(string remoteDir, InfoStartEnd startEnd, InfoOperation infoOperation)
        {
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

        public override FileSystemExitCode GetFile(RemotePath remoteName, string localName, CopyFlags copyFlags, RemoteInfo remoteInfo)
        {
            return base.GetFile(remoteName, localName, copyFlags, remoteInfo);
        }

        public override FileSystemExitCode PutFile(string localName, RemotePath remoteName, CopyFlags copyFlags)
        {
            return base.PutFile(localName, remoteName, copyFlags);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Načte seznam portů.
        /// Mají atribut adresářů.
        /// </summary>
        /// <returns></returns>
        private static List<FindData> GetPorts()
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "wcc.exe",
                    Arguments = "-ports",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

            if (process == null)
                return new List<FindData> {new FindData("<wcc.exe not found>")};

            var output = process.StandardOutput;
            var errput = process.StandardError;

            process.WaitForExit(5000); //5 sec

            if (!process.HasExited)
                return new List<FindData> { new FindData("<wcc.exe process timeout>") };

            var outString = output.ReadToEnd();
            var errString = errput.ReadToEnd();

            if (!string.IsNullOrEmpty(errString))
                throw new Exception(errString);

            return outString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(it => it.StartsWith("COM"))
                .Select(it=>new FindData(it, FileAttributes.Directory))
                .ToList();
        }

        /// <summary>
        /// Načte seznam souborů a adresářů.
        /// </summary>
        /// <returns></returns>
        private static List<FindData> GetFileAndDirectories(string port, string path)
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "wcc.exe",
                    Arguments = $"-p {port} -ls {path}/*",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

            if (process == null)
                return new List<FindData> { new FindData("<wcc.exe not found>") };

            var output = process.StandardOutput;

            process.WaitForExit(5000); //5 sec

            if (!process.HasExited)
                return new List<FindData> { new FindData("<wcc.exe process timeout>")};

            return output.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(it => NewFindData(it))
                .ToList();
        }

        /// <summary>
        /// Vrací položku 
        /// </summary>
        /// <param name="line"></param>
        private static FindData NewFindData(string line)
        {
            var lineArray = line.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (lineArray.Length != 3)
                return new FindData("<unsupported line format>");

            if (lineArray[0].Equals("d", StringComparison.OrdinalIgnoreCase))
                return new FindData(lineArray[2], FileAttributes.Directory);

            if (lineArray[0].Equals("f", StringComparison.InvariantCultureIgnoreCase))
            {
                ulong.TryParse(lineArray[1], out ulong fileSize);

                return new FindData(lineArray[2], fileSize, FileAttributes.Normal);
            }

            return new FindData($"<unsupported file type '{lineArray[0]}'>");
        }

        #endregion
    }
}
