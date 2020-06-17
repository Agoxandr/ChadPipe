using ChadPipe.Droid.Services;
using ChadPipe.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(GetPathServiceService))]
namespace ChadPipe.Droid.Services
{
    public class GetPathServiceService : IGetPathService
    {
        public string GetPath()
        {
            if (Android.OS.Environment.MediaMounted.Equals(Android.OS.Environment.ExternalStorageState))
            {
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
            }
            else
            {
                return null;
            }
        }
    }
}