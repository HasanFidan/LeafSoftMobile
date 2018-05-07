using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.App;
using Android.Hardware;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using ExLeafSoftApplication.Views;
using ExLeafSoftApplication.Droid;
using Android.Content;
using AndroidHUD;
using ExifLib;
using Android.Media;
using Android.Locations;

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraPageRenderer))]
namespace ExLeafSoftApplication.Droid
{

    //https://developer.xamarin.com/guides/xamarin-forms/application-fundamentals/custom-renderer/contentpage/
    public class CameraPageRenderer : PageRenderer, TextureView.ISurfaceTextureListener
    {
        global::Android.Hardware.Camera camera;
        global::Android.Widget.Button takePhotoButton;
        global::Android.Widget.Button toggleFlashButton;
        global::Android.Widget.Button switchCameraButton;
        global::Android.Views.View view;

        Activity activity;
        CameraFacing cameraType;
        TextureView textureView;
        SurfaceTexture surfaceTexture;
        Page MainPage;

        bool flashOn;
        byte[] imageBytes;


        public CameraPageRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
            MainPage = e.NewElement;
          

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                SetupUserInterface();
                SetupEventHandlers();
                AddView(view);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"			ERROR: ", ex.Message);
            }
        }

        void SetupUserInterface()
        {
            activity = this.Context as Activity;
            view = activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);
            cameraType = CameraFacing.Back;

            textureView = view.FindViewById<TextureView>(Resource.Id.textureView);
            textureView.SurfaceTextureListener = this;
        }

        void SetupEventHandlers()
        {
            takePhotoButton = view.FindViewById<global::Android.Widget.Button>(Resource.Id.takePhotoButton);
            takePhotoButton.Click += TakePhotoButtonTapped;

            switchCameraButton = view.FindViewById<global::Android.Widget.Button>(Resource.Id.switchCameraButton);
            switchCameraButton.Click += SwitchCameraButtonTapped;

            toggleFlashButton = view.FindViewById<global::Android.Widget.Button>(Resource.Id.toggleFlashButton);
            toggleFlashButton.Click += ToggleFlashButtonTapped;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);
            view.Layout(0, 0, r - l, b - t);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            camera = global::Android.Hardware.Camera.Open((int)cameraType);
            textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            surfaceTexture = surface;

            camera.SetPreviewTexture(surface);
            PrepareAndStartCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            camera.StopPreview();
            camera.Release();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            PrepareAndStartCamera();
        }

        void PrepareAndStartCamera()
        {
            camera.StopPreview();

            var display = activity.WindowManager.DefaultDisplay;
            if (display.Rotation == SurfaceOrientation.Rotation0)
            {
                camera.SetDisplayOrientation(90);
            }

            if (display.Rotation == SurfaceOrientation.Rotation270)
            {
                camera.SetDisplayOrientation(180);
            }

            camera.StartPreview();
        }

        void ToggleFlashButtonTapped(object sender, EventArgs e)
        {
            flashOn = !flashOn;
            if (flashOn)
            {
                if (cameraType == CameraFacing.Back)
                {
                    toggleFlashButton.SetBackgroundResource(Resource.Drawable.FlashButton);
                    cameraType = CameraFacing.Back;

                    camera.StopPreview();
                    camera.Release();
                    camera = global::Android.Hardware.Camera.Open((int)cameraType);
                    var parameters = camera.GetParameters();
                    parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeTorch;
                    camera.SetParameters(parameters);
                    camera.SetPreviewTexture(surfaceTexture);
                    PrepareAndStartCamera();
                }
            }
            else
            {
                toggleFlashButton.SetBackgroundResource(Resource.Drawable.NoFlashButton);
                camera.StopPreview();
                camera.Release();

                camera = global::Android.Hardware.Camera.Open((int)cameraType);
                var parameters = camera.GetParameters();
                parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeOff;
                camera.SetParameters(parameters);
                camera.SetPreviewTexture(surfaceTexture);
                PrepareAndStartCamera();
            }
        }

        void SwitchCameraButtonTapped(object sender, EventArgs e)
        {
            if (cameraType == CameraFacing.Front)
            {
                cameraType = CameraFacing.Back;

                camera.StopPreview();
                camera.Release();
                camera = global::Android.Hardware.Camera.Open((int)cameraType);
                camera.SetPreviewTexture(surfaceTexture);
                PrepareAndStartCamera();
            }
            else
            {
                cameraType = CameraFacing.Front;

                camera.StopPreview();
                camera.Release();
                camera = global::Android.Hardware.Camera.Open((int)cameraType);
                camera.SetPreviewTexture(surfaceTexture);
                PrepareAndStartCamera();
            }
        }


      

       
      

        async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            camera.StopPreview();

            var image = textureView.Bitmap;

            try
            {
                var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
                var folderPath = absolutePath + "/Camera";

                //The Folllowing is an answer to quesition that how to Get the selected Crop and Image Name
                Models.CropModel SelectedCrop = null;
                string selectedImageName = (MainPage as CameraPage).GetViewModel.Parent.SelectedImagePath;
                FarmerModel selectedFarmer = (MainPage as CameraPage).GetViewModel.Parent.SelectedFarmer;
                FieldModel selectedField = (MainPage as CameraPage).GetViewModel.Parent.SelectedField;


                var filePath = string.Empty;

                if (!string.IsNullOrEmpty(selectedImageName))
                {
                    filePath = System.IO.Path.Combine(folderPath, string.Format("{0}.jpg", selectedImageName));
                    //ReadExifMetadata(filePath);
                    File.Delete(filePath);

                }

                if ((MainPage as CameraPage).GetViewModel.SelectedCrop != null)
                    SelectedCrop = (MainPage as CameraPage).GetViewModel.SelectedCrop;
                string fileName = string.Format("NEW_{0}_{1}_{2}.jpg", SelectedCrop.Crop_Name, selectedField.FieldGuid, SelectedCrop.Crop_ID.ToString());
                filePath = System.IO.Path.Combine(folderPath, fileName);


                //End of



                var fileStream = new FileStream(filePath, FileMode.Create);

                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 80, fileStream);

                fileStream.Close();
                image.Recycle();
                AndHUD.Shared.ShowToast(activity, "Photo saved!!!", MaskType.Clear, TimeSpan.FromMilliseconds(1500));

                var intent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
                var file = new Java.IO.File(filePath);
                var uri = Android.Net.Uri.FromFile(file);
                intent.SetData(uri);
                //Forms.Context.SendBroadcast(intent);
                Context.SendBroadcast(intent);
                //GetExifMetadata(filePath, SelectedCrop.Crop_Name);






            }
            catch (Exception ex)
            {
                AndHUD.Shared.ShowToast(activity, "Photo can not taken!!!", MaskType.Clear, TimeSpan.FromMilliseconds(1500));
                System.Diagnostics.Debug.WriteLine(@"				", ex.Message);

            }
            finally
            {
                await MainPage.Navigation.PopModalAsync();
            }
            //camera.StartPreview();
        }
    }
}

