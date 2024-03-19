using System.Configuration;
using System.Data;
using System.Windows;
using SQLitePCL;

namespace LC_Portfolio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize SQLitePCL
            SQLitePCL.Batteries_V2.Init();

            // Other startup logic...
        }
    }

}
