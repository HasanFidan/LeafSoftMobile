using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using ExLeafSoftApplication.ServiceInterface;
using System.Collections.Generic;
using ExLeafSoftApplication.Common;

namespace ExLeafSoftApplication.Data
{ 

    public class PhotoService : IPhotoService
    {
        HttpClient client;
        public CapsuleItem Items { get; private set; }
        public LoginResult loginResult { get; private set; }

        private const string CustomerServiceURL = "http://leafsoft.test.soil-cares.com/Synchronization/";


        public PhotoService()
        {
            //var authData = string.Format("{0}:{1}", Constants.Username, Constants.Password);
            //var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            client = new HttpClient();
            client.MaxResponseContentBufferSize = 256000;
         
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }


        private string str = string.Empty;
        private string innerstr = string.Empty;

        public async Task<string> SendPhotoAsync()
        {
            string returnValue = string.Empty ;
            try
            {
                client.BaseAddress = new Uri(CustomerServiceURL);

                PhotoManagerService ms = new PhotoManagerService();
               
                List<FileMetaInformation> encoded = ms.GetPhotos(ms.GetPhotoList(""));

                if (encoded.Count == 0)
                    return string.Empty;

                string str = JsonConvert.SerializeObject(new RootModel { Encoded = encoded });
                StringContent theContent = new StringContent(str, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("ConvertBase64ToImage", theContent);
                if (response.IsSuccessStatusCode)
                {


                    var content = await response.Content.ReadAsStringAsync();
                    returnValue = JsonConvert.DeserializeObject<string>(content);
                }
            }
            catch (Exception ex)
            {
                str = ex.Message;
                innerstr = ex.InnerException.Message;
            }

            return returnValue;
        }

       
    }
}
