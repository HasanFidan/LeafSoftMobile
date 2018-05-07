

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ExLeafSoftApplication.Common
{
    public interface IFileHelper
    {
        string GetLocalFilePath(string filename);
        string[] GetPhotoPathList(string fieldGuid);
        List<FileMetaInformation> GetImages(string[] files);
        string CameraFolderPath();
        void ShowMessage(string message);
        List<FileMetaInformation> GetTumbNailImages(string fieldGuid);
        void GetExifMetadata(string fileName, string OrderId);
    }

    public class FileMetaInformation
    {
        public string leanFileName { get; set; }
        public string encodedFile { get; set; }
        public byte[] orjinalImage { get; set; }

    }


    public class TickedMessage
    {
        public string Message { get; set; }
    }

    public class CancelledMessage
    {
    }

    public class StartLongRunningTaskMessage
    {
    }

    public class StopLongRunningTaskMessage
    {
    }
}
