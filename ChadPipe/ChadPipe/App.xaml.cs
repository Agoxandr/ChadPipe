using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ChadPipe
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        public App(string id)
        {
            InitializeComponent();
            MainPage = new MainPage(id);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
