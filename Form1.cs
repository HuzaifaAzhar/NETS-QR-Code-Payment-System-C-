using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace WinFormsApp4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void DisplayQRCode(string base64QrCode)
        {
            try
            {
                byte[] qrCodeBytes = Convert.FromBase64String(base64QrCode);

                using (MemoryStream ms = new MemoryStream(qrCodeBytes))
                {
                    Image qrCodeImage = Image.FromStream(ms);

                    PictureBox qrCodePictureBox = new PictureBox
                    {
                        Image = qrCodeImage,
                        SizeMode = PictureBoxSizeMode.AutoSize 
                    };

                    this.Controls.Add(qrCodePictureBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying QR code: {ex.Message}");
            }
        }

        public static string GenerateSignature(string payload, string secret)
        {

            string signature = payload + secret;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signature));
                string base64EncodedSignature = Convert.ToBase64String(hashBytes);
                return base64EncodedSignature;
            }

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            TimeZoneInfo singaporeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            DateTime singaporeDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, singaporeTimeZone);

            string transactionTime = singaporeDateTime.ToString("HHmmss"); 
            string transactionDate = singaporeDateTime.ToString("MMdd"); 

            string json = $@"{{
                    ""mti"":""0200"",
                    ""process_code"":""990000"",
                    ""amount"":""1000"",
                    ""stan"":""100001"",
                    ""transaction_time"":""{transactionTime}"",
                    ""transaction_date"":""{transactionDate}"",
                    ""entry_mode"":""000"",
                    ""condition_code"":""85"",
                    ""institution_code"":""20000000001"",
                    ""host_tid"":""37066801"",
                    ""host_mid"":""11137066800"",
                    ""npx_data"":{{
                        ""E103"":""37066801"",
                        ""E201"":""00000123"",
                        ""E202"":""SGD""
                    }},
                    ""communication_data"":[{{
                        ""type"":""http"",
                        ""category"":""URL"",
                        ""destination"":""https://your-domain-name:8801/demo/order/notification"",
                        ""addon"":{{
                            ""external_API_keyID"":""8bc63cde-2647-4a78-ac75-d5f534b56047""
                        }}
                    }}],
                    ""getQRCode"":""Y""
                }}";
            string secretKey = "16c573bf-0721-478a-8635-38e53e3badf1";
            string signature = GenerateSignature(json, secretKey);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("KeyId", "231e4c11-135a-4457-bc84-3cc6d3565506");
                    client.DefaultRequestHeaders.Add("Sign", signature);

                    string jsonData = json;

                    StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://uat-api.nets.com.sg:9065/uat/merchantservices/qr/dynamic/v1/order/request", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(responseBody);
                    var responseJson = JsonDocument.Parse(responseBody);
                    string qrCodeData = responseJson.RootElement.GetProperty("qr_code").GetString();
                    DisplayQRCode(qrCodeData);
                     }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}");
            }
        }

    }

}