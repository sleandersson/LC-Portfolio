using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace LC_Portfolio
{
    public partial class LoginWindow : Window
    {
        private int loginAttempts = 0; // Counter for login attempts
        public LoginWindow()
        {
            InitializeComponent();
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (rbtnSSO.IsChecked == true)
            {
                AuthenticateUsingSSO();
            }
            else if (rbtnOAuth.IsChecked == true)
            {
                AuthenticateUsingOAuth();
            }
            else if (rbtnIWA.IsChecked == true)
            {
                AuthenticateUsingIWA();
            }
            else
            {
                UserPrompt.Text = "Please choose an appropriate login method";
                //MessageBox.Show("Please choose an appropriate login method", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AuthenticateUsingSSO() // Install-Package Microsoft.Identity.Client if this is used, remember to install package
        {
            //// Initialize the MSAL client with details from your Azure app registration
            //var pcaOptions = new PublicClientApplicationBuilder()
            //    .WithClientId("Your_Application_Id")
            //    .WithAuthority(AzureCloudInstance.AzurePublic, "Your_Tenant_Id")
            //    .WithDefaultRedirectUri()
            //    .Build();

            //try
            //{
            //    // Attempt to acquire an existing token from the cache, or login the user
            //    var result = await pcaOptions.AcquireTokenInteractive(new[] { "User.Read" }).ExecuteAsync();

            //    // If successful, result contains an authentication token which can be used to access Microsoft Graph, etc.
            //    MessageBox.Show($"Authentication successful. Welcome, {result.Account.Username}!");
            //}
            //catch (MsalUiRequiredException)
            //{
            //    // The user needs to perform interactive authentication
            //    MessageBox.Show("Interactive authentication is required.");
            //}
            //catch (Exception ex)
            //{
            //    // Handle other exceptions accordingly
            //    MessageBox.Show($"Authentication failed: {ex.Message}");
            //}

            // Placeholder for SSO authentication logic
            MessageBox.Show("Code for authenticating using SSO is currently disabled");
        }

        private async void AuthenticateUsingOAuth()
        {
            //// Your OAuth provider's authorization endpoint
            //string authorizationEndpoint = "https://example.com/auth";

            //// Your application's client ID and redirect URI
            //string clientId = "your-client-id";
            //string redirectUri = "your-redirect-uri";

            //// The OAuth scope(s) you're requesting
            //string scope = "openid profile";

            //// Construct the authorization request URL
            //string authorizationRequest = $"{authorizationEndpoint}?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}";

            //// Open the authorization request URL in the user's browser
            //System.Diagnostics.Process.Start(authorizationRequest);

            //// TODO: Implement the logic to handle the authorization response.
            //// This often involves starting a local HTTP server before opening the browser
            //// to listen for the callback from the OAuth provider, extracting the authorization code,
            //// and exchanging the code for an access token at the token endpoint.

            //MessageBox.Show("Please complete the authentication in your web browser.");

            MessageBox.Show("Code for authenticating using OAuth/WIF is currently disabled");
        }

        private async void AuthenticateUsingIWA()
        {   
            //var handler = new HttpClientHandler()
            //{
            //    UseDefaultCredentials = true
            //};

            //using (var client = new HttpClient(handler))
            //{
            //    // Replace 'apiUrl' with the URL of your web API
            //    string apiUrl = "https://yourwebapi.com/api/values";

            //    try
            //    {
            //        var response = await client.GetAsync(apiUrl);
            //        if (response.IsSuccessStatusCode)
            //        {
            //            MessageBox.Show("Authenticated successfully using IWA.");
            //        }
            //        else
            //        {
            //            MessageBox.Show("Authentication failed.");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Error: {ex.Message}");
            //    }
            //}
            
            MessageBox.Show("Code for authenticating using IWA is currently disabled");
        }

        private void ByPassLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Temporary check against a hardcoded username and password
            if (username == "user" && password == "")
            {

                // Open the main window and close the login window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                loginAttempts++;
                if (loginAttempts >= 5)
                {
                    MessageBox.Show("Maximum login attempts reached. Application will close.");
                    this.Close(); // Close the application or disable login
                }
                else
                {
                    // Authentication failed
                    int attemptsLeft = 5 - loginAttempts;
                    MessageBox.Show($"Login failed. You have {attemptsLeft} attempts left. Please check your username and password.");
                }
            }
      
            /* Commented out: Active Directory authentication structure
            if (AuthenticateUser(username, password))
            {
                // Authentication successful
                MessageBox.Show("Login successful.");
                // Proceed to open the main application window or perform other actions
            }
            else
            {
                // Authentication failed
                MessageBox.Show("Login failed. Please check your username and password.");
            }
            */
        }

        /*
        private bool AuthenticateUser(string username, string password)
        {
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during authentication: {ex.Message}");
                return false;
            }
        }*/

    }
}
