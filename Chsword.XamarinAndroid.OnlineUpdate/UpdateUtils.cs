using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Java.IO;
using Environment = Android.OS.Environment;
using Exception = Java.Lang.Exception;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

namespace Chsword.XamarinAndroid.OnlineUpdate
{
    public static class UpdateConfig
    {
        public static Action<Activity, string> AlertFunc = null;

        internal static void Alert(Activity activity, string msg)
        {
            if (AlertFunc != null)
            {
                AlertFunc(activity, msg);
            }
        }
    }

    public class UpdateUtils
    {
        public static string GetVersion(Activity activity)
        {
            try
            {
                PackageManager manager = activity.PackageManager;
                PackageInfo info = manager.GetPackageInfo(activity.PackageName, 0);
                string version = info.VersionName;
                return version;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void Update(Activity activity, IUpdateInfo updateInfo)
        {
            try
            {
                string version = GetVersion(activity);
                if (updateInfo.Version == version)
                {
                    
                    UpdateConfig.Alert(activity, activity.GetString(Resource.String.chsword_latest_version));
                }
                else
                {
                    NewVersionUpdate(activity, updateInfo);
                }
            }
            catch (Exception e)
            {
                UpdateConfig.Alert(activity, e.ToString());
            }
        }

        private static void NewVersionUpdate(Activity activity, IUpdateInfo updateInfo)
        {
            string version = GetVersion(activity);
            var sb = new StringBuilder();
            sb.AppendFormat(activity.GetString(Resource.String.chsword_current_version), version
                , updateInfo.Version);
            Dialog dialog = new AlertDialog.Builder(activity)
                .SetTitle(activity.GetString(Resource.String.chsword_update_dialog_title))
                .SetMessage(sb.ToString())
                .SetPositiveButton(activity.GetString(Resource.String.chsword_update_dialog_ok),
                    (s, e) =>
                    {
                        ProgressDialog pBar = ProgressDialog.Show(activity, null, "loading", true);
                        pBar.SetTitle(activity.GetString(Resource.String.chsword_update_progress_title));
                        pBar.SetMessage(activity.GetString(Resource.String.chsword_update_progress_content));
                        pBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                        DownloadFile(activity, pBar, updateInfo.Url, pBar);
                    })
                .SetNegativeButton(activity.GetString(Resource.String.chsword_update_dialog_cancel), (s, e) => { }
                ).Create();
            dialog.Show();
        }

        private static void DownloadFile(Activity activity, ProgressDialog pBar, string url, ProgressDialog dialog)
        {
            pBar.Show();
            string filename = Guid.NewGuid() + ".apk";

            var thread = new Thread(() =>
            {
                var webRequest = (HttpWebRequest) WebRequest.Create(url);
                using (WebResponse response = webRequest.GetResponse())
                {
                    long totalBytes = 0;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (var dest = new FileOutputStream(new File(Environment.ExternalStorageDirectory,
                            filename)))
                        {
                            int currentSize;
                            var buffer = new byte[1024];

                            while ((currentSize = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                totalBytes += currentSize;
                                //var percentage = (int) (totalBytes*100.0/totalSize);                                dest.Write(buffer, 0, currentBlockSize);
                                dest.Write(buffer, 0, currentSize);

                                //if (dialog != null)
                                //    dialog.
                                //    progress(percentage, totalBytes, totalSize);
                            }
                            Down(activity, filename, dialog);
                        }
                    }
                }
            });

            thread.Start();
        }

        private static void Down(Activity activity, string filename, Dialog dialog)
        {
            var intent = new Intent(Intent.ActionView);

            intent.SetDataAndType(Uri.FromFile(new File(Environment
                .ExternalStorageDirectory, filename)),
                "application/vnd.android.package-archive");
            activity.StartActivity(intent);
            dialog.Hide();
            dialog.Dismiss();
            dialog.Cancel();
        }
    }
}