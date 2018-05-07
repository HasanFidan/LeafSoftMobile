using ExLeafSoftApplication.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ExLeafSoftApplication.Data
{
    public class PhotoManagerService
    {
        private string CameraFolderPath = string.Empty;

        public PhotoManagerService()
        {
            CameraFolderPath = DependencyService.Get<IFileHelper>().CameraFolderPath();
           
        }

        public string[] GetPhotoList(string FieldGuid)
        {
            DependencyService.Get<IFileHelper>().ShowMessage("Photo sent started!!!");
            string[] files = DependencyService.Get<IFileHelper>().GetPhotoPathList(FieldGuid);
            return files;

        }

        public void SetImageExif(string fileName,string Orderid)
        {
            DependencyService.Get<IFileHelper>().GetExifMetadata(fileName, Orderid);
        }

        

        public List<FileMetaInformation> GetPhotos(string[] paths)
        {
            List<FileMetaInformation>  listofImages = DependencyService.Get<IFileHelper>().GetImages(paths);
            string encoded = string.Empty;
            //foreach (string a in listofImages)
            //{

            //    encoded = a;
               
            //    //byte[] b = Base64Decode(a);
                
                


            //    //long i = a.Length;

            //    ////ZipFile.CreateFromDirectory(startPath, zipPath);
            //    //byte[] bytes = a.ToArray();
            //    //string encodedString = Base64Encode(bytes);
            //    //int len = encodedString.Length;
            //    //byte[] copy =  Base64Decode(encodedString);
            //    //long lenofcopy = copy.Length;
            //    //int ak = 0;

            //    //if (i == lenofcopy)
            //    //{
            //    //    ak = 1;
            //    //}

            //    //string path = Path.Combine(CameraFolderPath, "NewFile.jpg");

            //    //FileStream newfile = new FileStream(path,FileMode.Create,FileAccess.Write);
            //    //a.WriteTo(newfile);
            //    //newfile.Close();
            //    //a.Close();
                



               

            //}

            return listofImages;

        }

        public List<FileMetaInformation> GetThumbNailPhotos(string fieldGuid)
        {
           return DependencyService.Get<IFileHelper>().GetTumbNailImages(fieldGuid);
        }

        public static string Base64Encode(byte[] bytes)
        {
            //var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(bytes);
        }

        public static byte[] Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            //return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            return base64EncodedBytes;
        }
    }
}
