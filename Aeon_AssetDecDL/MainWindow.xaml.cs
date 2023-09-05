using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aeon_AssetDecDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btn_download_list_Click(object sender, RoutedEventArgs e)
        {
            // 強制覆蓋原有的assetlist
            string al = Path.Combine(App.Root, "assetlist");
            List<Task> tasks = new List<Task>
            {
                DownLoadFile("https://rp-cn-prod-server.rpfans.net/api/game-data/hash/latest", Path.Combine(al, "assets.json"), true),
                DownLoadFile("https://rp-cn-prod-server.rpfans.net/api/game-data/hash/request/latest", Path.Combine(al, "assets_secret.json"), true)
            };
            await Task.WhenAll(tasks);
            tasks.Clear();
            if (App.glocount > 0)
            {
                System.Windows.MessageBox.Show($"下載完成，共{App.glocount}個檔案", "Finish");
                App.glocount = 0;
            }
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            // [1] hashlatest.json
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = Path.Combine(App.Root, "assetlist");
            openFileDialog.Filter = "assets.json|*.json";
            if (!openFileDialog.ShowDialog() == true)
                return;

            JArray FileList = JArray.Parse(File.ReadAllText(openFileDialog.FileName));

            List<Tuple<string, string>> AssetList = new List<Tuple<string, string>>();
            App.Respath = Path.Combine(App.Root, "Asset");
            foreach (JObject jo in FileList)
            {
                string name = jo["fileName"].ToString();
                string url = jo["url"].ToString();

                AssetList.Add(new Tuple<string, string>(name, url));
            }

            // [2] latest.json
            openFileDialog.InitialDirectory = Path.GetDirectoryName(openFileDialog.FileName);
            openFileDialog.Filter = "assets_secret.json|*.json";
            if (!openFileDialog.ShowDialog() == true)
                return;

            FileList = JArray.Parse(File.ReadAllText(openFileDialog.FileName));

            foreach (JObject jo in FileList)
            {
                string name = jo["fileName"].ToString();
                string url = jo["url"].ToString();

                AssetList.Add(new Tuple<string, string>(name, url));
            }

            App.TotalCount = AssetList.Count;

            if (App.TotalCount > 0)
            {
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                int count = 0;
                List<Task> tasks = new List<Task>();
                foreach (Tuple<string, string> asset in AssetList)
                {
                    string path = Path.Combine(App.Respath, asset.Item1);
                    string url = App.ServerURL + asset.Item2;

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }

                lb_counter.Content = $"進度 : {count} / {App.TotalCount}，正在解壓";
                await Task.Delay(1);

                string[] fileList = Directory.GetFiles(App.Respath, "*.zip", SearchOption.TopDirectoryOnly);
                foreach (string file in fileList)
                {
                    //ZipFile.ExtractToDirectory(file, App.Unzippath);
                    if (IsEncryptZip(file, "test"))
                        UnZipFiles(file, App.Respath, "test");
                    else
                        UnZipFiles(file, App.Respath);
                    File.Delete(file);
                }

                if (cb_Debug.IsChecked == true && App.log.Count > 0)
                {
                    using (StreamWriter outputFile = new StreamWriter("404.log", false))
                    {
                        foreach (string s in App.log)
                            outputFile.WriteLine(s);
                    }
                }

                string msg = $"下載完成，共{App.glocount}個檔案";
                if (App.log.Count > 0)
                    msg += $"，{App.log.Count}個檔案失敗";
                if (App.TotalCount - App.glocount > 0)
                    msg += $"，略過{App.TotalCount - App.glocount - App.log.Count}個檔案";

                System.Windows.MessageBox.Show(msg, "Finish");
                lb_counter.Content = String.Empty;
            }
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Don't use DownloadFileTaskAsync, if 404 it will create an empty file, use DownloadDataTaskAsync instead.
                    byte[] data = await wc.DownloadDataTaskAsync(downPath);
                    File.WriteAllBytes(savePath, data);
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// 檢查是否為加密壓縮檔
        /// </summary>
        public static bool IsEncryptZip(string path, string password = "")
        {
            bool isenc = false;
            using (FileStream fileStreamIn = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (ZipInputStream zipInStream = new ZipInputStream(fileStreamIn))
            {
                ZipEntry entry;
                if (password != null && password != string.Empty) zipInStream.Password = password;
                while ((entry = zipInStream.GetNextEntry()) != null)
                {
                    if (entry.IsCrypted) isenc = true;
                }
                return isenc;
            }
        }

        /// <summary>
        /// 解壓縮
        /// </summary>
        private void UnZipFiles(string filepath, string destfolder, string password = "")
        {
            ZipInputStream zipInStream = null;

            try
            {
                if (!Directory.Exists(destfolder))
                    Directory.CreateDirectory(destfolder);
                
                zipInStream = new ZipInputStream(File.OpenRead(filepath));
                if (password != null && password != string.Empty) zipInStream.Password = password;
                ZipEntry entry;

                while ((entry = zipInStream.GetNextEntry()) != null)
                {
                    string filePath = Path.Combine(destfolder, entry.Name);
                    
                    if (entry.Name != "")
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        // Skip directory entry
                        if (Path.GetFileName(filePath).Length == 0)
                        {
                            continue;
                        }

                        byte[] buffer = new byte[4096];
                        using (FileStream streamWriter = File.Create(filePath))
                        {
                            StreamUtils.Copy(zipInStream, streamWriter, buffer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
            }
            finally
            {
                zipInStream.Close();
                zipInStream.Dispose();
            }
        }

        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            string selectPath = String.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = App.Root;

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectPath = openFolderDialog.Folder;
                if (!Directory.Exists(selectPath))
                {
                    selectPath = String.Empty;
                    lb_counter.Content = "Error: 選擇的路徑不存在";
                }
            }

            var result = System.Windows.MessageBox.Show("轉換將會覆蓋掉原始檔案，繼續?", "注意", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            string signed = "eeab";
            int count = 0;
            List<string> fileList = Directory.GetFiles(selectPath, "*.unity3d", SearchOption.AllDirectories).ToList();

            foreach (string file in fileList)
            {
                byte[] data = File.ReadAllBytes(file);
                long data_size = data.Length;

                // File Sign check
                if (data_size > signed.Length)
                {
                    byte[] tmp = new byte[signed.Length];
                    Array.Copy(data, tmp, signed.Length);
                    if (Encoding.UTF8.GetString(tmp) == signed)
                    {
                        //App.eeablist.Add(file.Replace(App.Root, String.Empty));
                        byte[] newdata = DecryptUnityAsset.DecryptMemory(file, data);
                        File.WriteAllBytes(file, newdata);
                        count++;
                    }
                }
            }
            
            /*
            using (StreamWriter outputFile = new StreamWriter("eeablist.log", false))
            {
                foreach (string s in App.eeablist)
                    outputFile.WriteLine(s);
            }
            */

            lb_counter.Content = $"已轉換 {count} 個檔案";
        }
    }
}
