using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Locations;
using Android.Media;
using Android.Telephony;
using Android.Widget;
using AndroidHUD;
using ExLeafSoftApplication.Common;
using ExLeafSoftApplication.Droid;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(FileHelper))]
namespace ExLeafSoftApplication.Droid
{
    public class FileHelper : IFileHelper
    {
        public string GetLocalFilePath(string filename)
        {

           Java.IO.File filepath =  Android.OS.Environment.ExternalStorageDirectory;
           DirectoryInfo dirinfo =  Directory.CreateDirectory(filepath.AbsolutePath + "/LeafSoft");
            
            return System.IO.Path.Combine(dirinfo.FullName, filename);
        }

        public void ShowMessage(string message)
        {
            var activity = CrossCurrentActivity.Current.Activity;
           
            //Toast.MakeText(activity, "This is toast message", ToastLength.Short);
            AndHUD.Shared.ShowToast(activity,message , MaskType.Clear, TimeSpan.FromMilliseconds(1500));
        }

        public string CameraFolderPath()
        {
            var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
            var folderPath = absolutePath + "/Camera";

            return folderPath;
        }

        public string[] GetPhotoPathList(string fieldGuid)
        {
            string[] files = null;

            var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
            var folderPath = absolutePath + "/Camera";
            if (Directory.Exists(folderPath))
                files = Directory.GetFiles(folderPath, string.Format("NEW_{0}_{1}_{2}.jpg","*", fieldGuid,"*")) ;

            return files;
        }

        public void SendFilesNameChanged(List<FileMetaInformation> sendfileList)
        {
            Java.IO.File filepath = Android.OS.Environment.ExternalStorageDirectory;
            DirectoryInfo dirinfo = Directory.CreateDirectory(filepath.AbsolutePath + "/LeafSoftPhotos");

            string cameraFolderPath = CameraFolderPath();
            string LeafSoftSendFolder = dirinfo.FullName;
            foreach (var item in sendfileList)
            {

                //string leanFileName = System.IO.Path.GetFileNameWithoutExtension(item.filePath);
                //string directory = System.IO.Path.GetDirectoryName(item.filePath);

                byte[] decodedStr = System.Convert.FromBase64String(item.encodedFile);
                string newfilepath = System.IO.Path.Combine(LeafSoftSendFolder, "Send_" + item.leanFileName + ".jpg");
               
                using (var imageFile = new FileStream(newfilepath, FileMode.Create, FileAccess.Write))
                {
                    imageFile.Write(decodedStr, 0, decodedStr.Length);
                    imageFile.Flush();
                }

                string oldfilepath = System.IO.Path.Combine(cameraFolderPath, item.leanFileName + ".jpg");
                File.Delete(oldfilepath);


            }
           
        }

        //https://stackoverflow.com/questions/43899497/create-image-file-from-byte-array-in-documents-xamarin-android?rq=1
        public List<FileMetaInformation> GetImages(string[] files)
        {
            List<FileMetaInformation> sendfileList = new List<FileMetaInformation>();
            
            int count = 0;
            foreach (string path in files)
            {
                var memoryStream = new MemoryStream();


                count++;

                if (count > 5)
                    break;

                FileStream fs = File.OpenRead(path);
               

                Bitmap image = BitmapFactory.DecodeStream(fs);

                //https://theconfuzedsourcecode.wordpress.com/2016/02/24/convert-android-bitmap-image-and-ios-uiimage-to-byte-array-in-xamarin/
                //http://www.e-nature.ch/tech/saving-loading-bitmaps-to-the-android-device-storage-internal-external/
                image.Compress(Bitmap.CompressFormat.Jpeg,100, memoryStream);
                byte[] a = memoryStream.ToArray();
                string encoded = System.Convert.ToBase64String(a);

                string directory = System.IO.Path.GetDirectoryName(path);
                sendfileList.Add(new FileMetaInformation { leanFileName = System.IO.Path.GetFileNameWithoutExtension(path), encodedFile = encoded  });
                fs.Close();
                image.Recycle();
                memoryStream.Close();



            }

            if (sendfileList.Count > 0)
            {
                SendFilesNameChanged(sendfileList);
            }

            return sendfileList;

        }


        


        public List<FileMetaInformation> GetTumbNailImages(string fieldGuid)
        {
            string[] fileList = GetPhotoPathList(fieldGuid);
            List<FileMetaInformation> sendfileList = new List<FileMetaInformation>(); 
            foreach (string path in fileList)
            {
                FileStream fs = File.OpenRead(path);
                byte[] images = ResizeImageAndroid(fs, 100, 100, 100);
                fs.Close();
                sendfileList.Add(new FileMetaInformation { orjinalImage = images, leanFileName = System.IO.Path.GetFileNameWithoutExtension(path) });
            }

            return sendfileList;
        }

        private byte[] ResizeImageAndroid(FileStream fs, float width, float height, int quality)
        {
                // Load the bitmap
                //Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
            Bitmap originalImage = BitmapFactory.DecodeStream(fs);

            float oldWidth = (float)originalImage.Width;
            float oldHeight = (float)originalImage.Height;
            float scaleFactor = 0f;

            if (oldWidth > oldHeight)
            {
                scaleFactor = width / oldWidth;
            }
            else
            {
                scaleFactor = height / oldHeight;
            }

            float newHeight = oldHeight * scaleFactor;
            float newWidth = oldWidth * scaleFactor;

            Bitmap resizedImage = Bitmap.CreateScaledBitmap(originalImage, (int)newWidth, (int)newHeight, false);

            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, ms);
                return ms.ToArray();
            }
        }

        public void GetExifMetadata(string fileName, string OrderId)
        {
          
            ExifInterface exif = new ExifInterface(fileName);
           
            string exifMake = exif.GetAttribute(ExifInterface.TagMake);
            string exifModel = exif.GetAttribute(ExifInterface.TagModel);

            if (string.IsNullOrEmpty(exifMake))
                exifMake = string.Empty;
            if (string.IsNullOrEmpty(exifModel))
                exifModel = string.Empty;


            exifMake = exifMake + " " + exifModel;
            string exifExtraData = GetLocationForExif(exif);
            string tmp =  GetPhoneInformation();
            string exifUserData = tmp + "BatchID=" + OrderId + ",";
            exifModel = exifUserData + exifExtraData;

            exif.SetAttribute(ExifInterface.TagMake, exifModel);
            exif.SetAttribute(ExifInterface.TagModel,exifModel);
            exif.SetAttribute(ExifInterface.TagImageDescription, exifModel);

            exif.SaveAttributes();
          
            //ReadExifMetadata(fileName);

        }


        private void ReadExifMetadata(string fileName)
        {
            ExifInterface exif = new ExifInterface(fileName);
            string model = exif.GetAttribute(ExifInterface.TagModel);
            string desc = exif.GetAttribute(ExifInterface.TagImageDescription);
            string make = exif.GetAttribute(ExifInterface.TagMake);




        }


        private string GetPhoneInformation()
        {
            string returnValue = string.Empty;

            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            //TelephonyManager phone = activity.phoneManager;
            activity.CheckPermission();

            if (!string.IsNullOrEmpty(activity.exifUserData))
                returnValue = activity.exifUserData;

            return returnValue;

        }

        long lastfix = 0;

        private string GetLocationForExif(ExifInterface exif)
        {
            string exifExtraData = string.Empty;
            var activity = (MainActivity) CrossCurrentActivity.Current.Activity;
            LocationManager locationManager = activity.locationManager;
            var criteria = new Criteria { PowerRequirement = Power.Medium };

            var bestProvider = locationManager.GetBestProvider(criteria, true);
            var location = locationManager.GetLastKnownLocation(bestProvider);
            if (location != null)
            {
                string locProvider = location.Provider;
                long newlastfix = location.Time;
                bool uniqueFix = newlastfix != lastfix;
                lastfix = newlastfix;
                exifExtraData = "loc=" + locProvider + "," + "uniqueLocFix=" + uniqueFix + ","; ;
                double locLat = location.Latitude;
                double locLon = location.Longitude;

                try
                {
                    int num1Lat = (int)Math.Floor(locLat);
                    int num2Lat = (int)Math.Floor((locLat - num1Lat) * 60);
                    double num3Lat = (locLat - ((double)num1Lat + ((double)num2Lat / 60))) * 3600000;

                    int num1Lon = (int)Math.Floor(locLon);
                    int num2Lon = (int)Math.Floor((locLon - num1Lon) * 60);
                    double num3Lon = (locLon - ((double)num1Lon + ((double)num2Lon / 60))) * 3600000;

                    exif.SetAttribute(ExifInterface.TagGpsLatitude, num1Lat + "/1," + num2Lat + "/1," + num3Lat + "/1000");
                    exif.SetAttribute(ExifInterface.TagGpsLongitude, num1Lon + "/1," + num2Lon + "/1," + num3Lon + "/1000");

                    if (locLat > 0)
                    {
                        exif.SetAttribute(ExifInterface.TagGpsLatitudeRef, "N");
                    }
                    else
                    {
                        exif.SetAttribute(ExifInterface.TagGpsLatitudeRef, "S");
                    }

                    if (locLon > 0)
                    {
                        exif.SetAttribute(ExifInterface.TagGpsLongitudeRef, "E");
                    }
                    else
                    {
                        exif.SetAttribute(ExifInterface.TagGpsLongitudeRef, "W");
                    }

                }
                catch (Exception e)
                {
                }

            }
          


            return exifExtraData;
        }

        //public static byte[] ResizeImageWinPhone (byte[] imageData, float width, float height)
        //{
        //byte[] resizedData;

        //using (MemoryStream streamIn = new MemoryStream (imageData))
        //{
        //WriteableBitmap bitmap = PictureDecoder.DecodeJpeg (streamIn, (int)width, (int)height);

        //using (MemoryStream streamOut = new MemoryStream ())
        //{
        //bitmap.SaveJpeg(streamOut, (int)width, (int)height, 0, 100);
        //resizedData = streamOut.ToArray();
        //}
        //}
        //return resizedData;
        //}

    }
}