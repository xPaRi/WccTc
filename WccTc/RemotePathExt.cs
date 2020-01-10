using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcPluginBase.FileSystem;

namespace WccTc
{
    public static class RemotePathExt
    {
        public static string GetPort(this RemotePath path)
        {
            return path.GetSegment(1);
        }

        public static string GetPathWithoutPort(this RemotePath path)
        {
            if (path.Segments.Length > 1)
                return "/" + string.Join("/", path.Segments, 1, path.Segments.Length - 1);

            return "/";
        }
    }
}
