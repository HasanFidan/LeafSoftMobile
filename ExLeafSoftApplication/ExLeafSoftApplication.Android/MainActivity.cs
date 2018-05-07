using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Acr.UserDialogs;
using Xamarin.Forms;
using ExLeafSoftApplication.Droid.Services;
using ExLeafSoftApplication.Common;
using Android.Content;
using Plugin.CurrentActivity;
using Android.Locations;
using Android.Telephony;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using Android.Support.Design.Widget;

namespace ExLeafSoftApplication.Droid
{
    [Activity(Label = "ExLeafSoftApplication",AlwaysRetainTaskState =true, Icon = "@drawable/icon", 
        Theme = "@style/MainTheme", MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {

        public LocationManager locationManager { get; set; }
        public TelephonyManager phoneManager { get; set; }
        public string exifUserData { get; set; }
        Android.Views.View layout;

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            //RegisterActivityLifecycleCallbacks(this);
            locationManager = (LocationManager)GetSystemService(LocationService);
            phoneManager = (TelephonyManager)GetSystemService(TelephonyService);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            global::Xamarin.FormsMaps.Init(this, bundle);
            CrossCurrentActivity.Current.Activity = this;
            UserDialogs.Init(this);
            LoadApplication(new App());
            WireUpLongRunningTask();
        }
        protected override void OnStop()
        {
            base.OnStop();
        }



        public void CheckPermission()
        {
            Permission permissionCheck = ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState);

            if (permissionCheck != Permission.Granted)
            {
                RequestCameraPermission();
                //ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadPhoneState },0);
            }
            else
            {
                GetInformation();
            }
        }


        void RequestCameraPermission()
        {
            

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadPhoneState))
            {

                Snackbar.Make(layout, "Allow to read phone state",
                   Snackbar.LengthIndefinite).SetAction("ok", new Action<Android.Views.View>(delegate (Android.Views.View obj) {
                       ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadPhoneState }, 12);
                   })).Show();
            }
            else
            {
                // Camera permission has not been granted yet. Request it directly.
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadPhoneState }, 12);
            }
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case 12:
                    if ((grantResults.Length > 0) && (grantResults[0] == Permission.Granted))
                    {
                        GetInformation();
                    }
                    break;

                default:
                    break;
            }
        }

        private void GetInformation()
        {
            TelephonyManager phone = this.phoneManager;

            if (phone.GetImei(0) != null)
            {
                exifUserData += "phoneId=" + phone.GetImei(0) + ",";
                if (phone.Line1Number != null)
                    exifUserData += "phoneNr1=" + phone.Line1Number + ",";
                if (phone.SimSerialNumber != null)
                    exifUserData += "simSerial=" + phone.SimSerialNumber + ",";
                if (phone.DeviceSoftwareVersion != null)
                    exifUserData += "phoneSoftwareVersion=" + phone.DeviceSoftwareVersion + ",";

                exifUserData += "androidRelease=" + Android.OS.Build.VERSION.Release + ",";
                exifUserData += "androidManufacturer=" + Android.OS.Build.Manufacturer + ",";
                exifUserData += "androidModel=" + Android.OS.Build.Model + ",";
                exifUserData += "androidProduct=" + Android.OS.Build.Product + ",";
                exifUserData += "androidBrand=" + Android.OS.Build.Brand + ",";
            }//end of if (phone.DeviceId != null)

            try
            {
                PackageInfo info = this.PackageManager.GetPackageInfo(this.PackageName, 0);
                exifUserData += "leafspotAppVersion=" + info.VersionName + ",";
            }
            catch (Exception e)
            { }

            
        }


        //private void StartBackgroundDataRefreshService()
        //{
        //    var pt = new PeriodicTask.Builder()
        //        .SetPeriod(1800) // in seconds; minimum is 30 seconds
        //        .SetService(Java.Lang.Class.FromType(typeof(LongRunningTaskService)))
        //        .SetRequiredNetwork(0)
        //        .SetTag(your package name) // package name
        //        .Build();

        //    GcmNetworkManager.GetInstance(this).Schedule(pt);
        //}




        void WireUpLongRunningTask()
        {
            MessagingCenter.Subscribe<StartLongRunningTaskMessage>(this, "StartLongRunningTaskMessage", message => {
                var intent = new Intent(this, typeof(LongRunningTaskService));
                StartService(intent);
            });

            MessagingCenter.Subscribe<StopLongRunningTaskMessage>(this, "StopLongRunningTaskMessage", message => {
                var intent = new Intent(this, typeof(LongRunningTaskService));
                StopService(intent);
            });
        }

    }
}

