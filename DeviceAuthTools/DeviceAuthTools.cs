using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeviceAuthGenerator
{
    public partial class DeviceAuthTools : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string basicToken = "basic NTIyOWRjZDNhYzM4NDUyMDhiNDk2NjQ5MDkyZjI1MWI6ZTNiZDJkM2UtYmY4Yy00ODU3LTllN2QtZjNkOTQ3ZDIyMGM3=";
        private Timer checkDeviceCodeTimer;
        private string deviceCode;
        private string accountId;

        public DeviceAuthTools()
        {
            InitializeComponent();

            this.RichTextBoxLogger.AppendText("Please login by pressing the Login button");

            client.BaseAddress = new Uri("https://account-public-service-prod03.ol.epicgames.com");
            client.DefaultRequestHeaders.Add("Authorization", basicToken);
        }

        async private void ButtonLogin_Click(object sender, EventArgs e)
        {
            this.RichTextBoxLogger.Clear();
            this.ButtonLogin.Enabled = false;
            this.RichTextBoxLogger.AppendText("Please wait...");

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await client.PostAsync("/account/api/oauth/token", body);
            dynamic responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.Clear();
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to get an access token\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
                return;
            }

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", "bearer " + responseData.access_token);

            body = new FormUrlEncodedContent(new Dictionary<string, string> { });

            response = await client.PostAsync("/account/api/oauth/deviceAuthorization", body);
            responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.Clear();
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to get an device code\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
                return;
            }
            deviceCode = responseData.device_code;

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", basicToken);

            System.Diagnostics.Process.Start("" + responseData.verification_uri_complete);

            checkDeviceCodeTimer = new Timer();
            checkDeviceCodeTimer.Tick += new EventHandler(checkDeviceCodeTick);
            checkDeviceCodeTimer.Interval = responseData.interval * 1000;
            checkDeviceCodeTimer.Start();

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Waiting for device code completion...");
        }

        async private void ButtonCreate_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Please wait...");

            var response = await client.PostAsync("/account/api/public/account/" + accountId + "/deviceAuth/", new StringContent("{}", Encoding.UTF8, "application/json"));
            var responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            this.RichTextBoxLogger.Clear();

            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to create a device auth\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
            }
            else
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));

            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
        }

        async private void ButtonShow_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Please wait...");

            var response = await client.GetAsync("/account/api/public/account/" + accountId + "/deviceAuth/");
            var responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            this.RichTextBoxLogger.Clear();

            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to get the device auths\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
            }
            else
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));

            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
        }

        async private void ButtonDelete_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;

            string deviceAuthId = Prompt.ShowDialog("Enter the Device Id");
            if(string.IsNullOrEmpty(deviceAuthId)) {
                MessageBox.Show("The device id cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.ButtonCreate.Enabled = true;
                this.ButtonShow.Enabled = true;
                this.ButtonDelete.Enabled = true;
                return;
            }

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Please wait...");

            var response = await client.DeleteAsync("/account/api/public/account/" + accountId + "/deviceAuth/" + deviceAuthId);
            var responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            this.RichTextBoxLogger.Clear();

            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to delete the device " + deviceAuthId + " auth\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
            } else
                this.RichTextBoxLogger.AppendText("Successfully deleted device " + deviceAuthId + " auth");

            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
        }

        private void checkDeviceCodeTick(object sender, EventArgs e)
        {
            checkDeviceCode();
        }

        async private void checkDeviceCode()
        {
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "device_code" },
                { "device_code", deviceCode }
            });

            var response = await client.PostAsync("/account/api/oauth/token", body);
            dynamic responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
            {
                if (responseData.errorCode == "errors.com.epicgames.not_found")
                {
                    this.ButtonLogin.Enabled = true;
                    this.RichTextBoxLogger.Clear();
                    this.RichTextBoxLogger.AppendText("The device code expired, please click again the Login button to login");
                } else if(responseData.errorCode != "errors.com.epicgames.account.oauth.authorization_pending")
                {
                    this.ButtonLogin.Enabled = true;
                    this.RichTextBoxLogger.Clear();
                    this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to get the access token by the device code\n\n");
                    this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
                }
                return;
            }

            checkDeviceCodeTimer.Stop();

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", "bearer " + responseData.access_token);

            accountId = responseData.account_id;

            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Welcome, " + responseData.displayName);
        }

        private void LabelSetUserAgent_Click(object sender, EventArgs e)
        {
            string userAgent = Prompt.ShowDialog("Enter the User Agent");
            if (string.IsNullOrEmpty(userAgent))
            {
                MessageBox.Show("The User-Agent cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "" + userAgent);

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Successfully set the User-Agent to " + userAgent);
        }

        private void LabelSetUserAgent_MouseDown(object sender, MouseEventArgs e)
        {
            this.LabelSetUserAgent.ForeColor = Color.FromArgb(255, 0, 0);
        }

        private void LabelSetUserAgent_MouseUp(object sender, MouseEventArgs e)
        {
            this.LabelSetUserAgent.ForeColor = Color.FromArgb(0, 0, 238);
        }

        private void LabelSetUserAgent_MouseHover(object sender, EventArgs e)
        {
            this.LabelSetUserAgent.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetUserAgent_MouseLeave(object sender, EventArgs e)
        {
            this.LabelSetUserAgent.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        private void LabelSetUserAgent_MouseMove(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }
    }
}
