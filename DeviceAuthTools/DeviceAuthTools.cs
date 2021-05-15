using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace DeviceAuthGenerator
{
    public partial class DeviceAuthTools : Form
    {
        private static readonly HttpClient client = new HttpClient();
        // Diesel - Dauntless client
        private static readonly string deviceCodeClientId = "b070f20729f84693b5d621c904fc5bc2";
        private static readonly string deviceCodeClientSecret = "HG@XE&TGCxEJsgT#&_p2]=aRo#~>=>+c6PhR)zXP";
        // fortniteIOSClient
        private static string clientId = "3446cd72694c4a4485d81b77adbb2141";
        private static string clientSecret = "9209d4a5e25a457fb9b07489d313b41a";
        private Timer checkDeviceCodeTimer;
        private string deviceCode;
        private string accountId;

        public DeviceAuthTools()
        {
            InitializeComponent();

            this.RichTextBoxLogger.AppendText("Please login by pressing the Login button");

            client.BaseAddress = new Uri("https://account-public-service-prod03.ol.epicgames.com");
            client.DefaultRequestHeaders.Add("Authorization", GetDeviceCodeBasicToken());
        }
        private string GetDeviceCodeBasicToken()
        {
            return _GetBasicToken(deviceCodeClientId, deviceCodeClientSecret);
        }

        private string GetSwitchBasicToken()
        {
            return _GetBasicToken("5229dcd3ac3845208b496649092f251b", "e3bd2d3e-bf8c-4857-9e7d-f3d947d220c7");
        }

        private string GetBasicToken()
        {
            return _GetBasicToken(clientId, clientSecret);
        }

        private string _GetBasicToken(string clientId, string clientSecret)
        {
            return "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ':' + clientSecret));
        }

        async private void ButtonLogin_Click(object sender, EventArgs e)
        {
            if (this.ButtonLogin.Text == "Logout")
            {
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", GetDeviceCodeBasicToken());
                accountId = null;

                this.LabelSetClient.Visible = true;
                this.LabelSetSwitchClient.Visible = true;
                this.LabelSetIOSClient.Visible = true;
                this.ButtonCreate.Enabled = false;
                this.ButtonShow.Enabled = false;
                this.ButtonDelete.Enabled = false;
                this.ButtonGetExchange.Enabled = false;
                this.ButtonLogin.Text = "Login";
                this.RichTextBoxLogger.Clear();
                this.RichTextBoxLogger.AppendText("Successfully logged out; login by pressing the Login button");
                return;
            }

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
            client.DefaultRequestHeaders.Add("Authorization", GetDeviceCodeBasicToken());

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
            this.ButtonGetExchange.Enabled = false;

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
            this.ButtonGetExchange.Enabled = true;
        }

        async private void ButtonShow_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;
            this.ButtonGetExchange.Enabled = false;

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
            this.ButtonGetExchange.Enabled = true;
        }

        async private void ButtonDelete_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;
            this.ButtonGetExchange.Enabled = false;

            string deviceAuthId = Prompt.ShowDialog("Enter the Device Id");
            if(string.IsNullOrEmpty(deviceAuthId)) {
                MessageBox.Show("The device id cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.ButtonCreate.Enabled = true;
                this.ButtonShow.Enabled = true;
                this.ButtonDelete.Enabled = true;
                this.ButtonGetExchange.Enabled = true;
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
            this.ButtonGetExchange.Enabled = true;
        }

        async private void ButtonGetExchange_Click(object sender, EventArgs e)
        {
            this.ButtonCreate.Enabled = false;
            this.ButtonShow.Enabled = false;
            this.ButtonDelete.Enabled = false;
            this.ButtonGetExchange.Enabled = false;

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Please wait...");

            var response = await client.GetAsync("/account/api/oauth/exchange");
            var responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            this.RichTextBoxLogger.Clear();

            if (!response.IsSuccessStatusCode)
            {
                this.RichTextBoxLogger.AppendText("Got " + (int)response.StatusCode + " status code while trying to get an device auth\n\n");
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));
            }
            else
                this.RichTextBoxLogger.AppendText(JsonConvert.SerializeObject(responseData, Formatting.Indented));

            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
            this.ButtonGetExchange.Enabled = true;
        }

        private void checkDeviceCodeTick(object sender, EventArgs e)
        {
            checkDeviceCode();
        }

        async private void checkDeviceCode()
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", GetSwitchBasicToken());

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

            if (GetSwitchBasicToken() != GetBasicToken())
            {
                response = await client.GetAsync("/account/api/oauth/exchange");
                responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                string exchangeCode = responseData.code;

                body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "exchange_code" },
                    { "exchange_code", exchangeCode }
                });

                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", GetBasicToken());

                response = await client.PostAsync("/account/api/oauth/token", body);
                responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", "bearer " + responseData.access_token);
            }

            accountId = responseData.account_id;

            this.LabelSetClient.Visible = false;
            this.LabelSetSwitchClient.Visible = false;
            this.LabelSetIOSClient.Visible = false;
            this.ButtonCreate.Enabled = true;
            this.ButtonShow.Enabled = true;
            this.ButtonDelete.Enabled = true;
            this.ButtonGetExchange.Enabled = true;
            this.ButtonLogin.Text = "Logout";
            this.ButtonLogin.Enabled = true;
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
        private void LabelSetClient_Click(object sender, EventArgs e)
        {
            string clientId = Prompt.ShowDialog("Enter the Client ID");
            if (string.IsNullOrEmpty(clientId))
            {
                MessageBox.Show("The Client ID cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string clientSecret = Prompt.ShowDialog("Enter the Client secret");
            if (string.IsNullOrEmpty(clientSecret))
            {
                MessageBox.Show("The Client secret cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DeviceAuthTools.clientId = clientId;
            DeviceAuthTools.clientSecret = clientSecret;

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Successfully set the client to " + GetBasicToken() + " (" + clientId + ':' + clientSecret + " base64 encoded)");
        }

        private void LabelSetClient_MouseDown(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(255, 0, 0);
        }

        private void LabelSetClient_MouseUp(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
        }

        private void LabelSetClient_MouseHover(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetClient_MouseLeave(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        private void LabelSetClient_MouseMove(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetIOSClient_Click(object sender, EventArgs e)
        {
            clientId = "3446cd72694c4a4485d81b77adbb2141";
            clientSecret = "9209d4a5e25a457fb9b07489d313b41a";

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Successfully set the client to IOS (" + GetBasicToken() + "; " + clientId + ':' + clientSecret + " base64 encoded)");
        }

        private void LabelSetIOSClient_MouseDown(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(255, 0, 0);
        }

        private void LabelSetIOSClient_MouseUp(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
        }

        private void LabelSetIOSClient_MouseHover(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetIOSClient_MouseLeave(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        private void LabelSetIOSClient_MouseMove(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetSwitchClient_Click(object sender, EventArgs e)
        {
            clientId = "5229dcd3ac3845208b496649092f251b";
            clientSecret = "e3bd2d3e-bf8c-4857-9e7d-f3d947d220c7";

            this.RichTextBoxLogger.Clear();
            this.RichTextBoxLogger.AppendText("Successfully set the client to Switch (" + GetBasicToken() + "; " + clientId + ':' + clientSecret + " base64 encoded)");
        }

        private void LabelSetSwitchClient_MouseDown(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(255, 0, 0);
        }

        private void LabelSetSwitchClient_MouseUp(object sender, MouseEventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
        }

        private void LabelSetSwitchClient_MouseHover(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        private void LabelSetSwitchClient_MouseLeave(object sender, EventArgs e)
        {
            this.LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        private void LabelSetSwitchClient_MouseMove(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }
    }
}
