using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace ChadPipe.Droid
{
    [Activity(Label = "ChadPipe", Icon = "@mipmap/icon", Theme = "@style/SplashTheme", NoHistory = true, MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SplashScreen : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        public override void OnBackPressed() { }
    }
}