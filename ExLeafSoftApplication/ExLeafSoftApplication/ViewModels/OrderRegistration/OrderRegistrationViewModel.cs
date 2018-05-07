
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ExLeafSoftApplication.Views;
using ExLeafSoftApplication.Data;
using ExLeafSoftApplication.Common;
using System.IO;
using ExLeafSoftApplication.Models;

namespace ExLeafSoftApplication.ViewModels
{
    public class ThumbNail
    {
        public string Name { get; set; }
        public string OriginalImageName { get; set; }
        public ImageSource ImgSource { get; set; }
        public string CropId { get; set; }

    }

    public class OrderRegistrationViewModel :BaseViewModel
    {

        public object ParentPage { get; set; }
        public INavigation Navigation { get; set; }
        public string SelectedImagePath { get; set; }

        private int totalCropCount { get; set; }

        private ObservableCollection<ThumbNail> _listofImgs;

        public ObservableCollection<ThumbNail> listofImgs {
            get { return _listofImgs; }
            set { _listofImgs = value; OnPropertyChanged("listofImgs"); }
        }

        public ICommand CommandFarmerSelection { get; set;  }
        public ICommand CommandFieldSelection { get; set; }
        public ICommand CommandCompleteOrder { get; set; }
        public ICommand CommandCropSelection { get; set; }


        private FarmerModel _selectedFarmer { get; set; }
        public FarmerModel SelectedFarmer
        {
            get { return _selectedFarmer; }
            set { _selectedFarmer = value;
                IsFarmerAndFieldSelected();
                OnPropertyChanged("SelectedFarmer"); }

        }

        private FieldModel _selectedField { get; set; }
        public FieldModel SelectedField
        {
            get { return _selectedField; }
            set { _selectedField = value;
                IsFarmerAndFieldSelected();
                OnPropertyChanged("SelectedField"); }

        }

        private bool _cropSelectionIsEnabled { get; set; }
        public bool CropSelectionIsEnabled
        {
            get { return _cropSelectionIsEnabled; }
            set {
                _cropSelectionIsEnabled = value;
                OnPropertyChanged(nameof(CropSelectionIsEnabled));
            }
        }

        private bool _orderCompleteIsEnabled { get; set; }
        public bool OrderCompleteIsEnabled
        {
            get { return _orderCompleteIsEnabled; }
            set
            {
                _orderCompleteIsEnabled = value;
                OnPropertyChanged(nameof(OrderCompleteIsEnabled));
            }
        }

        private string _btnCropSelection = string.Empty;
        public string BtnCropSelection
        {
            get { return _btnCropSelection; }
            set
            {
                _btnCropSelection = value;
                OnPropertyChanged(nameof(BtnCropSelection));
            }
        }

        private string _btnFieldSelection { get; set; }
        public string BtnFieldSelection
        { get { return _btnFieldSelection; }
            set {
                _btnFieldSelection = value;
                OnPropertyChanged("BtnFieldSelection"); }
        }


        private string _btnFarmerSelection = string.Empty;
        public string BtnFarmerSelection
        {
            get { return _btnFarmerSelection; }
            set
            {
                _btnFarmerSelection = value;
                OnPropertyChanged(nameof(BtnFarmerSelection));
            }
        }

        public OrderRegistrationViewModel()
        {
            totalCropCount = 0;
            SelectedImagePath = string.Empty;
            CropSelectionIsEnabled = false;
            OrderCompleteIsEnabled = false;
            BtnCropSelection = "Select Crop";
            BtnFarmerSelection = "Select Farmer";
            BtnFieldSelection = "Go Field";
            CommandFarmerSelection = new Command(async () => await ExecuteFarmerSelectionCommand());
            CommandFieldSelection = new Command(async () =>  await ExecuteFieldSelectionCommand());
            CommandCompleteOrder = new Command(async () => await ExecuteCompleteOrderCommand());
            CommandCropSelection = new Command<string>(async (string param) => await ExecuteCropSelectionCommand(param));
            //LoadImages();
        }

        private void IsFarmerAndFieldSelected()
        {
            if (SelectedFarmer != null && SelectedField != null)
                CropSelectionIsEnabled = true;
            else
                CropSelectionIsEnabled = false;
        }
       

        public async Task<AddressFarmerModel> GetAddressFarmerModel(int farmerID, string FarmerGuid)
        {
            if (farmerID == 0)
            {
                farmerID = -1;
            }

            Guid tmp = Guid.Empty;

            if (!string.IsNullOrEmpty(FarmerGuid))
            {
                tmp = new Guid(FarmerGuid);
            }
          
            AddressFarmerModel selectedFarmerAddress = await App.AddressFarmerTable.GetItemAsync(farmerID,tmp);
            return selectedFarmerAddress;
        }

        async Task ExecuteCompleteOrderCommand()
        {
            OrderCompleteIsEnabled = false;

            PhotoManagerService ms = new PhotoManagerService();
            string[] photoPaths = ms.GetPhotoList(SelectedField.FieldGuid);

          
            Guid orderGuid = Guid.NewGuid();

            OrderModel orderModel = new OrderModel {
                Orde_LabelStickerName = "abc",
                Orde_FieldGuid = this.SelectedField.FieldGuid,
                Orde_UniqueId = orderGuid.ToString(),
                Orde_StatusId = 1,
                Orde_CreationDate = DateTime.Now.ToString(),
                Orde_RowHash = 0,
                Orde_UpdateDate = string.Empty

            };

           await App.OrderTable.SaveOrder(orderModel);

            foreach(ThumbNail item in listofImgs)
            {
                string orginalImageName = item.OriginalImageName;
                string cropID = item.CropId;
                Guid orderCropId = Guid.NewGuid();

                OrderDetailModel detail = new OrderDetailModel
                {
                    Ocad_CropID = Convert.ToInt32(cropID),
                    Ocad_CropPhotoName = item.OriginalImageName,
                    Ocad_OrderGuid = orderGuid.ToString(),
                    Ocad_CropUniqueId = orderCropId.ToString()

                };

               await App.OrderDetailTable.SaveOrderDetail(detail);

            }

            foreach(string photo in photoPaths)
            {
                ms.SetImageExif(photo,orderGuid.ToString());
            }


            await Navigation.PopAsync();
        }

        async Task ExecuteCropSelectionCommand(string param)
        {
            CropSelectionIsEnabled = false;

            //totalCropCount++;

            if (listofImgs != null && listofImgs.Count >= 3)
            {
                OrderCompleteIsEnabled = true;
                return;
            }

            if (param != null)
                SelectedImagePath = param as string;
            else
                SelectedImagePath = string.Empty;


         await Navigation.PushModalAsync(new CameraPage(this));
           CropSelectionIsEnabled = true;

        }

        async Task ExecuteFieldSelectionCommand()
        {
            await Navigation.PushAsync(new OrderSelectionsPage("SelectField",SelectedFarmer));
        }


        async Task ExecuteFarmerSelectionCommand()
        {
            await Navigation.PushAsync(new OrderSelectionsPage());
        }

        public void LoadImages()
        {
            PhotoManagerService ms = new PhotoManagerService();
            
            List<ThumbNail> images = new List<ThumbNail>();

            List<FileMetaInformation> thumbnails = ms.GetThumbNailPhotos(SelectedField.FieldGuid);
            foreach (FileMetaInformation item in thumbnails)
            {
                ImageSource img;
                img = ImageSource.FromStream(() => new MemoryStream(item.orjinalImage));
                string[] parts = item.leanFileName.Split('_');

                images.Add(new ThumbNail { Name=parts[1] , OriginalImageName = item.leanFileName, ImgSource = img, CropId = parts[3] });
            }

            listofImgs = new ObservableCollection<ThumbNail>(images);
         

        }

    }
}
