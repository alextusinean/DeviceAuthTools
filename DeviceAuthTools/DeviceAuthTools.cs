using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace DeviceAuthGenerator
{
    public partial class DeviceAuthTools : Form
    {
        public static readonly string ACCOUNT_API_BASE_URL = "https://account-public-service-prod03.ol.epicgames.com";
        // Device code shit; Currently Diesel - Dauntless client
        // public static readonly string DEVICE_CODE_CLIENT_ID = "b070f20729f84693b5d621c904fc5bc2";
        // public static readonly string DEVICE_CODE_CLIENT_SECRET = "HG@XE&TGCxEJsgT#&_p2]=aRo#~>=>+c6PhR)zXP";
        public readonly HttpClient httpClient = new HttpClient();
        // IOS User-Agent by default
        public string userAgent = "FortniteGame/++Fortnite+Release-13.40-CL-14050091 IOS/13.6";
        // fortniteIOSClient by default
        public string clientId = "3446cd72694c4a4485d81b77adbb2141";
        public string clientSecret = "9209d4a5e25a457fb9b07489d313b41a";
        public string deviceCode;
        public string accessToken;
        public string accountId;
        // Device code shit
        // public Timer deviceCodeTimer;

        public DeviceAuthTools()
        {
            InitializeComponent();
            SetUserAgent();

            WriteOutput("Please login by pressing the Login button");
        }

        public void SetUserAgent()
        {
            httpClient.DefaultRequestHeaders.Remove("User-Agent");
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }

        public void WriteOutput(string output)
        {
            WriteOutput(output, true);
        }

        public void WriteOutput(string output, bool clear)
        {
            if (clear)
            {
                RichTextBoxLogger.Clear();
            }

            RichTextBoxLogger.AppendText(output);
        }

        // Device code shit
        /* public string GetDeviceCodeBasicToken()
        {
            return GetBasicToken(DEVICE_CODE_CLIENT_ID, DEVICE_CODE_CLIENT_SECRET);
        } */

        public string GetSwitchBasicToken()
        {
            return GetBasicToken("5229dcd3ac3845208b496649092f251b", "e3bd2d3e-bf8c-4857-9e7d-f3d947d220c7");
        }

        public string GetBasicToken()
        {
            return GetBasicToken(clientId, clientSecret);
        }

        public string GetBasicToken(string clientId, string clientSecret)
        {
            return "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ':' + clientSecret));
        }

        public async Task<bool> HttpCheckForErrors(string url, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                WriteOutput(url + " got " + (int)response.StatusCode + " status code\n\n" + JsonConvert.SerializeObject(await response.Content.ReadAsStringAsync(), Formatting.Indented));
                return true;
            }

            return false;
        }

        public void SetAuthorizationHeader(string authorization)
        {
            RemoveAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
        }

        public void RemoveAuthorizationHeader()
        {
            httpClient.DefaultRequestHeaders.Remove("Authorization");
        }

        public async Task<dynamic> HttpGet(string url, string auth)
        {
            SetAuthorizationHeader(auth);
            HttpResponseMessage response = await httpClient.GetAsync(ACCOUNT_API_BASE_URL + url);

            RemoveAuthorizationHeader();
            if (await HttpCheckForErrors(url, response))
            {
                return null;
            }

            return JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
        }

        public async Task<dynamic> HttpPost(string url, string auth, HttpContent body)
        {
            SetAuthorizationHeader(auth);
            HttpResponseMessage response = await httpClient.PostAsync(ACCOUNT_API_BASE_URL + url, body);

            RemoveAuthorizationHeader();
            if (await HttpCheckForErrors(url, response))
            {
                return null;
            }

            return JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
        }

        public async Task<HttpResponseMessage> HttpPostRaw(string url, string auth, Dictionary<string, string> body, bool allowErrors)
        {
            SetAuthorizationHeader(auth);
            HttpResponseMessage response = await httpClient.PostAsync(ACCOUNT_API_BASE_URL + url, new FormUrlEncodedContent(body));

            RemoveAuthorizationHeader();
            if (!allowErrors && await HttpCheckForErrors(url, response))
            {
                return null;
            }

            return response;
        }

        public async Task<dynamic> HttpDelete(string url, string auth)
        {
            SetAuthorizationHeader(auth);
            HttpResponseMessage response = await httpClient.DeleteAsync(ACCOUNT_API_BASE_URL + url);

            RemoveAuthorizationHeader();
            if (await HttpCheckForErrors(url, response))
            {
                return null;
            }


            return JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
        }

        public async Task<HttpResponseMessage> HttpDeleteRaw(string url, string auth, bool allowErrors)
        {
            SetAuthorizationHeader(auth);
            HttpResponseMessage response = await httpClient.DeleteAsync(ACCOUNT_API_BASE_URL + url);

            RemoveAuthorizationHeader();
            if (!allowErrors && await HttpCheckForErrors(url, response))
            {
                return null;
            }


            return response;
        }

        public bool IsLoggedIn()
        {
            return ButtonLogin.Text == "Logout";
        }

        public void SetLoggedIn(bool loggedIn)
        {
            if (!loggedIn)
            {
                deviceCode = null;
                accountId = null;
            }

            ButtonLogin.Enabled = true;
            ButtonLogin.Text = loggedIn ? "Logout" : "Login";
            ButtonCreate.Enabled = loggedIn;
            ButtonShow.Enabled = loggedIn;
            ButtonDelete.Enabled = loggedIn;
            ButtonGetExchange.Enabled = loggedIn;
            LabelSetIOSClient.Visible = !loggedIn;
            LabelSetClient.Visible = !loggedIn;
        }

        public string GetAuthorizationCode(string redirectUrl)
        {
            Uri redirectUri;
            try
            {
                redirectUri = new Uri(redirectUrl);
            }
            catch (Exception)
            {
                return null;
            }

            return HttpUtility.ParseQueryString(redirectUri.Query).Get("code");
        }

        public async void ButtonLogin_Click(object sender, EventArgs e)
        {
            if (IsLoggedIn())
            {
                SetLoggedIn(false);
                WriteOutput("Successfully logged out");
                return;
            }

            ButtonLogin.Enabled = false;

            string authorizationCodeRaw = Prompt.Authorization("Enter an authorization code", clientId);
            if (authorizationCodeRaw == "\x00")
            {
                ButtonLogin.Enabled = true;
                return;
            }

            if (string.IsNullOrEmpty(authorizationCodeRaw))
            {
                MessageBox.Show("The authorization code cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ButtonLogin.Enabled = true;
                return;
            }

            WriteOutput("Please wait...");

            string authorizationCode;
            try
            {
                dynamic authorizationCodeData = JsonConvert.DeserializeObject(authorizationCodeRaw);
                authorizationCode = GetAuthorizationCode((string)authorizationCodeData.redirectUrl);
            }
            catch (Exception)
            {
                authorizationCode = GetAuthorizationCode(authorizationCodeRaw);
            }

            if (string.IsNullOrEmpty(authorizationCode))
                authorizationCode = authorizationCodeRaw;

            HttpResponseMessage response = await HttpPostRaw("/account/api/oauth/token", GetBasicToken(),
                    new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", authorizationCode }
                    }, true);
            dynamic responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
            {
                WriteOutput(ACCOUNT_API_BASE_URL + "/account/api/oauth/token got " + (int)response.StatusCode + " status code\n\n" + JsonConvert.SerializeObject(responseData, Formatting.Indented));
                ButtonLogin.Enabled = true; 
                return;
            }

            accessToken = "bearer " + responseData.access_token;
            accountId = responseData.account_id;

            SetLoggedIn(true);
            WriteOutput("Welcome, " + responseData.displayName);

            // Device code shit
            /* dynamic response = await HttpPost("/account/api/oauth/token", GetDeviceCodeBasicToken(),
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                }));
            if (response == null) return;

            response = await HttpPost("/account/api/oauth/deviceAuthorization", "bearer " + response.access_token, new FormUrlEncodedContent(new Dictionary<string, string> { }));
            if (response == null) return;

            deviceCode = response.device_code;

            deviceCodeTimer = new Timer();
            deviceCodeTimer.Tick += new EventHandler(delegate (Object o, EventArgs a) { CheckDeviceCode(); });
            deviceCodeTimer.Interval = response.interval * 1000;
            deviceCodeTimer.Start();

            WriteOutput("Waiting for device code completion...");
            System.Diagnostics.Process.Start(Convert.ToString(response.verification_uri_complete)); */
        }

        // Device code shit
        /* public async void CheckDeviceCode()
        {
            HttpResponseMessage response = await HttpPostRaw("/account/api/oauth/token", GetSwitchBasicToken(),
                    new Dictionary<string, string>
                    {
                        { "grant_type", "device_code" },
                        { "device_code", deviceCode }
                    }, true);
            dynamic responseData = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
            {
                if (responseData.errorCode == "errors.com.epicgames.not_found")
                {
                    deviceCodeTimer.Stop();
                    SetLoggedIn(false);
                    WriteOutput("The device code expired, press the Login button again to login");
                }
                else if (responseData.errorCode != "errors.com.epicgames.account.oauth.authorization_pending")
                {
                    deviceCodeTimer.Stop();
                    SetLoggedIn(false);
                    WriteOutput(ACCOUNT_API_BASE_URL + "/account/api/oauth/token got " + (int)response.StatusCode + " status code\n\n" + JsonConvert.SerializeObject(responseData, Formatting.Indented));
                }

                return;
            }

            deviceCodeTimer.Stop();

            if (GetSwitchBasicToken() != GetBasicToken())
            {
                responseData = await HttpGet("/account/api/oauth/exchange", "bearer " + responseData.access_token);
                if (responseData == null) return;
                responseData = await HttpPost("/account/api/oauth/token", GetBasicToken(),
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "exchange_code" },
                        { "exchange_code", Convert.ToString(responseData.code) }
                    }));
                if (responseData == null) return;
            }

            accessToken = "bearer " + responseData.access_token;
            accountId = responseData.account_id;

            SetLoggedIn(true);
            WriteOutput("Welcome, " + responseData.displayName);
        } */

        public void LockButtons(bool locked)
        {
            ButtonLogin.Enabled = !locked;
            ButtonCreate.Enabled = !locked;
            ButtonShow.Enabled = !locked;
            ButtonDelete.Enabled = !locked;
            ButtonGetExchange.Enabled = !locked;
        }

        public async void ButtonCreate_Click(object sender, EventArgs e)
        {
            LockButtons(true);
            WriteOutput("Please wait...");

            dynamic response = await HttpPost("/account/api/public/account/" + accountId + "/deviceAuth/", accessToken, new StringContent("{}", Encoding.UTF8, "application/json"));
            if (response == null)
            {
                LockButtons(false);
                return;
            }

            WriteOutput(JsonConvert.SerializeObject(response, Formatting.Indented));
            LockButtons(false);
        }

        public async void ButtonShow_Click(object sender, EventArgs e)
        {
            LockButtons(true);
            WriteOutput("Please wait...");

            dynamic response = await HttpGet("/account/api/public/account/" + accountId + "/deviceAuth/", accessToken);
            if (response == null)
            {
                LockButtons(false);
                return;
            }

            WriteOutput(JsonConvert.SerializeObject(response, Formatting.Indented));
            LockButtons(false);
        }

        public async void ButtonDelete_Click(object sender, EventArgs e)
        {
            LockButtons(true);

            string deviceAuthId = Prompt.ShowDialog("Enter the Device Auth ID");
            if (deviceAuthId == "\x00")
            {
                LockButtons(false);
                return;
            }

            if (string.IsNullOrEmpty(deviceAuthId))
            {
                MessageBox.Show("The Device Auth ID cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LockButtons(false);
                return;
            }

            WriteOutput("Please wait...");

            HttpResponseMessage response = await HttpDeleteRaw("/account/api/public/account/" + accountId + "/deviceAuth/" + deviceAuthId, accessToken, true);
            if (!response.IsSuccessStatusCode)
            {
                WriteOutput(ACCOUNT_API_BASE_URL + "/account/api/public/account/" + accountId + "/deviceAuth/" + deviceAuthId + " got " + (int)response.StatusCode + " status code\n\n" + JsonConvert.SerializeObject(await response.Content.ReadAsStringAsync(), Formatting.Indented));
                LockButtons(false);
                return;
            }

            WriteOutput("Successfully deleted the device auth with id " + deviceAuthId);
            LockButtons(false);
        }

        public async void ButtonGetExchange_Click(object sender, EventArgs e)
        {
            LockButtons(true);
            WriteOutput("Please wait...");

            dynamic response = await HttpGet("/account/api/oauth/exchange", accessToken);
            if (response == null)
            {
                LockButtons(false);
                return;
            }

            WriteOutput(JsonConvert.SerializeObject(response, Formatting.Indented));
            LockButtons(false);
        }

        public async void LabelSetIOSClient_Click(object sender, EventArgs e)
        {
            clientId = "3446cd72694c4a4485d81b77adbb2141";
            clientSecret = "9209d4a5e25a457fb9b07489d313b41a";

            WriteOutput("Successfully set the client to IOS (" + clientId + ':' + clientSecret + ')');
        }

        public async void LabelSetClient_Click(object sender, EventArgs e)
        {
            string clientId = Prompt.ShowDialog("Enter the Client ID", this.clientId);
            if (clientId == "\x00")
                return;

            if (string.IsNullOrEmpty(clientId))
            {
                MessageBox.Show("The Client ID cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string clientSecret = Prompt.ShowDialog("Enter the Client secret", this.clientSecret);
            if (clientSecret == "\x00")
                return;

            if (string.IsNullOrEmpty(clientSecret))
            {
                MessageBox.Show("The Client secret cannot be empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.clientId = clientId;
            this.clientSecret = clientSecret;

            WriteOutput("Successfully set the client (" + clientId + ':' + clientSecret + ')');
        }

        public async void LabelSetUserAgent_Click(object sender, EventArgs e)
        {
            string userAgent = Prompt.ShowDialog("Enter the User Agent", this.userAgent);
            if (userAgent == "\x00")
                return;

            if (string.IsNullOrEmpty(userAgent))
            {
                this.userAgent = "";
                httpClient.DefaultRequestHeaders.Remove("User-Agent");

                WriteOutput("Successfully removed the User-Agent");
            } else
            {
                this.userAgent = userAgent;
                SetUserAgent();

                WriteOutput("Successfully set the User-Agent to " + userAgent);
            }
        }



        // GARBAGE
        public async void LabelSetIOSClient_MouseDown(object sender, EventArgs e)
        {
            LabelSetIOSClient.ForeColor = Color.FromArgb(255, 0, 0);
        }

        public async void LabelSetClient_MouseDown(object sender, EventArgs e)
        {
            LabelSetClient.ForeColor = Color.FromArgb(255, 0, 0);
        }

        public async void LabelSetUserAgent_MouseDown(object sender, EventArgs e)
        {
            LabelSetUserAgent.ForeColor = Color.FromArgb(255, 0, 0);
        }



        public async void LabelSetIOSClient_MouseLeave(object sender, EventArgs e)
        {
            LabelSetIOSClient.ForeColor = Color.FromArgb(0, 0, 238);
        }

        public async void LabelSetClient_MouseLeave(object sender, EventArgs e)
        {
            LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
        }

        public async void LabelSetUserAgent_MouseLeave(object sender, EventArgs e)
        {
            LabelSetUserAgent.ForeColor = Color.FromArgb(0, 0, 238);
        }



        public async void LabelSetIOSClient_MouseHover(object sender, EventArgs e)
        {
            LabelSetIOSClient.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        public async void LabelSetClient_MouseHover(object sender, EventArgs e)
        {
            LabelSetClient.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }

        public async void LabelSetUserAgent_MouseHover(object sender, EventArgs e)
        {
            LabelSetUserAgent.ForeColor = Color.FromArgb(0, 119, 204);
            Cursor.Current = Cursors.Hand;
        }



        public async void LabelSetIOSClient_MouseMove(object sender, EventArgs e)
        {
            LabelSetIOSClient.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        public async void LabelSetClient_MouseMove(object sender, EventArgs e)
        {
            LabelSetClient.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }

        public async void LabelSetUserAgent_MouseMove(object sender, EventArgs e)
        {
            LabelSetUserAgent.ForeColor = Color.FromArgb(0, 0, 238);
            Cursor.Current = Cursors.Default;
        }



        public async void LabelSetIOSClient_MouseUp(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }

        public async void LabelSetClient_MouseUp(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }

        public async void LabelSetUserAgent_MouseUp(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.Hand;
        }
    }
}
