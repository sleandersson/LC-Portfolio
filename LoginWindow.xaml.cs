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
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Temporary check against a hardcoded username and password
            if (username == "user" && password == "password")
            {
                // MessageBox.Show("Login successful.");

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
        }
        */
    }
}
