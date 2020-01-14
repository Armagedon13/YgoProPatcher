using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YgoProEsPatcher
{
    public partial class YgoProEsPatcher : Form
    {
        public YgoProEsPatcher()
        {
            InitializeComponent();
            ServicePointManager.DefaultConnectionLimit = throttleValue + 2;
            List<string> paths = LocalData.LoadFileToList("paths.txt");
            YgoProEsPath.Text = paths?[0];
            UpdateCheckerCooldownCheck();
            _pool = new Semaphore(0, throttleValue);
            _pool.Release(throttleValue);
            toolTip1.SetToolTip(ReinstallCheckbox, "This will download the newest version of the YGOProEs Client and install it.\nTHIS OPTION WILL OVERWRITE YOUR SETTINGS AND CUSTOM TEXTURES!");
            toolTip1.SetToolTip(OverwriteCheckbox, "This will redownload all of the pictures in your picture folder.");
            toolTip1.SetToolTip(gitHubDownloadCheckbox, "RECOMMENDED OPTION!\nThis will update your YGOProEs with the newest cards, pictures.");
            toolTip1.SetToolTip(YgoProEsPath, "Please select your YGOProEs directory which contains all the YGOPro2 files.");
            toolTip1.SetToolTip(YgoProEsPathButton, "Please select your YGOProEs directory which contains all the YGOProEs files.");
            //toolTip1.SetToolTip(YgoProLinksPath, "Please select your YGOPro Percy directory which contains all the YGOPro Percy files.");
            //toolTip1.SetToolTip(YGOPRO1PathButton, "Please select your YGOPro Percy directory which contains all the YGOPro Percy files.");
            toolTip1.SetToolTip(UpdateButton, "Start updating with the selected options.");
            toolTip1.SetToolTip(UpdateCheckerButton, "This allows you to get notified via sound and message popup\nabout new updates while this app is running!");
            toolTip1.SetToolTip(UpdateCheckerTimeNumeric, "Select the interval between update checks!");
            toolTip1.SetToolTip(UpdateWhenLabel, "This label tells you if/when the next check will occur or if it's on cooldown!");
            toolTip1.SetToolTip(MimimizeButton, "This button makes the application minimize to taskbar!\nUseful if you want to check for updates without this window taking space!");
            toolTip1.SetToolTip(StartMinimizedCheckbox, "This lets you make YgoProPatcher start in background,\nchecking for new updates in background!");
            string version = Data.version;
            footerLabel.Text += version;
            CheckForNewVersionOfPatcher(version);
            gitHubDownloadCheckbox.Enabled = false;

        }

        //timer
        private void UpdateCheckerCooldownCheck()
        {
            List<string> timerList = LocalData.LoadFileToList("donotdeletethis");
            if (timerList != null)
            {
                DateTime dateNow = DateTime.Now;
                DateTime result = Convert.ToDateTime(timerList[0]);
                result = result.AddMilliseconds(Double.Parse(timerList[1]));


                if (result.CompareTo(dateNow) > 0)
                {

                    UpdateCheckerButton.Enabled = false;
                    TimeSpan timer = result.Subtract(dateNow);
                    UpdateWhenLabel.Text = String.Format("Update checking is on cooldown until {0}!", result.ToShortTimeString());


                    ButtonNotAvailableTimer.Interval = (int)timer.TotalMilliseconds;
                    ButtonNotAvailableTimer.Elapsed += ButtonNotAvailableTimer_Tick;
                    ButtonNotAvailableTimer.Start();
                }
            }
        }

        //boton no disponible mientras esta actualizando
        private void ButtonNotAvailableTimer_Tick(object sender, EventArgs e)
        {
            UpdateCheckerButton.Invoke(new Action(() =>
            {
                UpdateCheckerButton.Enabled = true; UpdateWhenLabel.Text = "Update checking is available again!";
            }));
        }

        int throttleValue = 6;
        int downloads = 0;
        bool threadRunning = false;
        private static Semaphore _pool;
        System.Timers.Timer updateCheckerTimer = new System.Timers.Timer();
        System.Timers.Timer nextUpdateTimer = new System.Timers.Timer();
        System.Timers.Timer ButtonNotAvailableTimer = new System.Timers.Timer();
        ContextMenu contextMenu1 = new ContextMenu();
        MenuItem menuItemExit = new MenuItem();
        MenuItem menuItemStatus = new MenuItem();

        private void YgoProEsButton_Click(object sender, EventArgs e)
        {
            FolderSelection("YGOProEs");
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            updateCheckerTimer.Stop();
            UpdateCheckerButton.Enabled = false;
            UpdateCheckerTimeNumeric.Enabled = false;
            internetCheckbox.Enabled = false;
            UpdateButton.Enabled = false;
            ReinstallCheckbox.Enabled = false;
            gitHubDownloadCheckbox.Enabled = false;
            OverwriteCheckbox.Enabled = false;
            progressBar.Visible = true;
            exitButton.Visible = false;
            cancelButton.Visible = true;
            YgoProEsPath.Enabled = false;
            YgoProEsPathButton.Enabled = false;
            threadRunning = true;
            backgroundWorker1.RunWorkerAsync();
        }

        //seleccion la carpeta
        private string FolderSelection(string versionOfYGO)
        {
            string folderString = "";
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Select main folder of " + versionOfYGO
            };
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (versionOfYGO == "YGOProES")
                {
                     YgoProEsPath.Text = fbd.SelectedPath;
                   
                }
               /* else
                {
                    //YgoProLinksPath.Text = fbd.SelectedPath;
                }*/
            }

            return folderString;
        }

        //Lo que falla y no se que carajo hace esto
        private async Task<bool> FileDownload(string fileName, string destinationFolder, string website, bool overwrite)
        {

            string webFile = website + fileName;
            string destFile;
            if (Path.GetExtension(fileName) == ".jpg")
            {
                //destFile = Path.Combine(destinationFolder, Path.ChangeExtension(fileName, ".png"));
                destFile = Path.Combine(destinationFolder, fileName);
            }
            else
            {
                destFile = Path.Combine(destinationFolder, fileName);
            }


            try
            {

                if (!File.Exists(destFile) || overwrite)
                {
                    using (var client = new WebClient())
                    {
                        _pool.WaitOne();
                        await Task.Run(() => {client.DownloadFile(new Uri(webFile), destFile); });
                        downloads = -_pool.Release();
                    }


                }

                return true;
            }
            catch
            {
                downloads = -_pool.Release();
                return false;
            }
            finally
            {
                //debug.Invoke(new Action(() => { debug.Text = downloads.ToString(); }));
            }

        }

        //boton de cancelar
        private void Cancel_Click(object sender, EventArgs e)
        {
            while (downloads > 1 - throttleValue && (gitHubDownloadCheckbox.Checked || internetCheckbox.Checked) && cancelButton.Visible)
            {
                Status.Invoke(new Action(() => { Status.Text = "Canceling the download, please wait!"; Status.Update(); }));
            }
            if (cancelButton.Visible)
            {
                threadRunning = false;
                backgroundWorker1.CancelAsync();
                cancelButton.Visible = false;
                exitButton.Visible = true;
                internetCheckbox.Enabled = true;
                ReinstallCheckbox.Enabled = true;
                OverwriteCheckbox.Enabled = true;
                gitHubDownloadCheckbox.Enabled = false;
                YgoProEsPath.Enabled = true;
                YgoProEsPathButton.Enabled = true;
                UpdateButton.Enabled = true;
                UpdateCheckerButton.Enabled = true;
                UpdateCheckerTimeNumeric.Enabled = true;
                Status.Text = "Operation canceled!";
                Status.Update();
            }
        }

        //Si reinstalacion esta checkeado
        private async void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (ReinstallCheckbox.Checked || !File.Exists(YgoProEsPath.Text + "/YGOProES.exe"))
            {
                Status.Invoke(new Action(() => {
                    Status.Text = "Reinstalling YGOProEs, please be patient, this may take a while!";
                    cancelButton.Visible = false;
                    progressBar.Visible = false;
                }));

                YgoProEsCliente.Download(YgoProEsPath.Text);
            }
            cancelButton.Invoke(new Action(() =>
            {
                cancelButton.Visible = true;
                progressBar.Visible = true;
            }));

            DeleteOldCdbs();
            if (!gitHubDownloadCheckbox.Checked)
            {
                /*if (threadRunning) { Copy("cdb"); ; }
                if (threadRunning) { Copy("script"); }
                if (threadRunning) { Copy("script2"); }
                if (threadRunning) { Copy("pic"); ; }*/
            }
            else
            {
                if (threadRunning)
                {
                    await GitHubDownload(YgoProEsPath.Text);
                }
            }
            if (threadRunning)
            {

                Status.Invoke(new Action(() => {
                    notifyIcon1.ShowBalloonTip(6000, "Update complete!", "Click this to launch YGOProEs.", ToolTipIcon.Info);
                    notifyIcon1.BalloonTipClicked += FinishButton_Click;
                    Status.Text = "Update complete!"; ReinstallCheckbox.Enabled = true; cancelButton.Visible = false; exitButton.Visible = true; internetCheckbox.Enabled = true; gitHubDownloadCheckbox.Enabled = true; OverwriteCheckbox.Enabled = true; UpdateCheckerButton.Enabled = false;
                    UpdateCheckerTimeNumeric.Enabled = false; UpdateButton.Visible = false; FinishButton.Visible = true; FinishButton.Enabled = true;
                }));
                threadRunning = false;
            }
        }

        //Descarga CDB usando github
        private async Task<List<string>> DownloadCDBSFromGithub(string destinationFolder)
        {

            List<string> listOfCDBs = GitAccess.GetAllFilesWithExtensionFromYGOPRO("/", ".cdb");
            string cdbFolder = Path.Combine(destinationFolder, "locales/es-ES");
            //string cdbFolder2 = Path.Combine(destinationFolder);
            if (!await FileDownload("cards.cdb", cdbFolder, "https://github.com/Armagedon13/YgoproEs-CDB/raw/master/", true))
            {
                await FileDownload("cards.cdb", cdbFolder, "https://github.com/Armagedon13/YgoproEs-CDB/raw/master/", true);             
            }
            /*if (!await FileDownload("cards.cdb", cdbFolder2, Data.GetStringsWebsite(), true))
            {
                await FileDownload("cards.cdb", cdbFolder2, Data.GetStringsWebsite(), true);
            }*/
            progressBar.Invoke(new Action(() => progressBar.Maximum = listOfCDBs.Count));
            List<string> listOfDownloadedCDBS = new List<string>() { Path.Combine(cdbFolder, "cards.cdb") };
            if (await FileDownload("prerelease.cdb", cdbFolder, "https://github.com/Armagedon13/YgoproEs-CDB/raw/master/", true))
            {
                listOfDownloadedCDBS.Add(Path.Combine(cdbFolder, "prerelease.cdb"));
            }
            if (await FileDownload("preupdate.cdb", cdbFolder, "https://github.com/Armagedon13/YgoproEs-CDB/raw/master/", true))
            {
                listOfDownloadedCDBS.Add(Path.Combine(cdbFolder, "preupdate.cdb"));
            }
            List<Task> downloadList = new List<Task>();
            foreach (string cdb in listOfCDBs)
            {
                await FileDownload(cdb, cdbFolder, "https://github.com/Armagedon13/YgoproEs-CDB/raw/master/", true);
                listOfDownloadedCDBS.Add(Path.Combine(cdbFolder, cdb));
                progressBar.Invoke(new Action(() => progressBar.Increment(1)));

            }
            while (downloads > 1 - throttleValue)
            {
                Thread.Sleep(1);
            }
            return listOfDownloadedCDBS;
        }

        //Descarga lista y strings
        private async Task GitHubDownload(string destinationFolder)
        {
            Status.Invoke(new Action(() => { Status.Text = "Updating card databases from YGOProES CDB."; }));
            List<string> CDBS = new List<string>();

            CDBS = await DownloadCDBSFromGithub(destinationFolder);
            await FileDownload("lflist.conf", Path.Combine(YgoProEsPath.Text), "https://raw.githubusercontent.com/Armagedon13/YgoproEs-CDB/master/", true);
            await FileDownload("strings.conf", Path.Combine(YgoProEsPath.Text, "locales/es-ES"), Data.GetStringsWebsite(), true);
            await FileDownload("cards.cdb", Path.Combine(YgoProEsPath.Text), Data.GetStringsWebsite(), true);
            progressBar.Invoke(new Action(() => { progressBar.Value = progressBar.Maximum; }));

            DownloadUsingCDB(CDBS, destinationFolder);
        }

        //
        private void DownloadUsingCDB(List<string> listOfDownloadedCDBS, string destinationFolder)
        {
            if (threadRunning)
            {

                foreach (string cdb in listOfDownloadedCDBS)
                {
                    if (threadRunning)
                    {
                        DataClass db = new DataClass(cdb);
                        DataTable dt = db.SelectQuery("SELECT id FROM datas");
                        Status.Invoke(new Action(() => Status.Text = "Updating pictures using " + Path.GetFileName(cdb)));
                        progressBar.Invoke(new Action(() => progressBar.Maximum = (dt.Rows.Count)));
                        progressBar.Invoke(new Action(() => progressBar.Value = 0));
                        string dlWebsitePics = Data.GetPicWebsite();
                        string dlWebsiteLua = Data.GetLuaWebsite();
                        string dlWebsiteField = Data.GetFieldWebsite();
                        string dFPics = Path.Combine(destinationFolder, "pics");
                        string dFPicsField = Path.Combine(destinationFolder, "pics/field");
                        string dFLua = Path.Combine(destinationFolder, "script");
                        List<string> downloadList = new List<string>();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (threadRunning)
                            {
                                downloadList.Add(dt.Rows[i][0].ToString());
                            }
                            else
                            {
                                break;
                            }

                        }
                        foreach (string Value in downloadList)
                        {

                            if (threadRunning)
                            {
                                FileDownload(Value.ToString() + ".jpg", dFPics, dlWebsitePics, OverwriteCheckbox.Checked);
                                FileDownload("c" + Value.ToString() + ".lua", dFLua, dlWebsiteLua, true);
                                //FileDownload(Value.ToString() + ".jpg", dFPicsField, dlWebsiteField, OverwriteCheckbox.Checked);
                                progressBar.Invoke(new Action(() => progressBar.Increment(1)));

                            }
                        }
                        while (downloads > 1 - throttleValue)
                        {
                            Thread.Sleep(1);
                        }

                    }

                }
                while (downloads > 1 - throttleValue)
                {
                    Thread.Sleep(1);
                }
                if (threadRunning)
                {
                    GitHubClient gitClient = GitAccess.githubAuthorized;
                    string path = "pics/field";
                    var fields = gitClient.Repository.Content.GetAllContents("Armagedon13", "YgoproEs-Pics-Field").Result;
                    Status.Invoke(new Action(() => { Status.Text = "Downloading field spell pictures."; }));
                    progressBar.Invoke(new Action(() => { progressBar.Maximum = fields.Count; }));
                    foreach (var field in fields)
                    {
                        if (threadRunning)
                        {
                            FileDownload(field.Name, Path.Combine(YgoProEsPath.Text, path), field.DownloadUrl, OverwriteCheckbox.Checked);
                            progressBar.Invoke(new Action(() => { progressBar.Increment(1); }));
                        }
                    }
                    while (downloads > 1 - throttleValue)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }


        //
        private void GitHubDownloadCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            //YgoProLinksPath.Enabled = !YgoProLinksPath.Enabled;
            //YGOPRO1PathButton.Enabled = !YGOPRO1PathButton.Enabled;
            //YgoProLinksPath.Visible = !YgoProLinksPath.Visible;
            //YGOPRO1PathButton.Visible = !YGOPRO1PathButton.Visible;
            //YGOPRO1Label.Visible = !YGOPRO1Label.Visible;
            internetCheckbox.Enabled = !internetCheckbox.Enabled;
            if (gitHubDownloadCheckbox.Checked && !internetCheckbox.Checked)
            {
                internetCheckbox.Checked = !internetCheckbox.Checked;
            }


        }

        //boton de salida nedeah
        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //nose que hace pero algo hace
        private void YgoProPatcher_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (threadRunning)
            {
                threadRunning = false;
                while (downloads > 1 - throttleValue && (gitHubDownloadCheckbox.Checked || internetCheckbox.Checked))
                {
                    Status.Invoke(new Action(() => { Status.Text = "Canceling the download, please wait!"; Status.Update(); }));

                }
            }
            try
            {
                LocalData.SaveFile(new List<string> { StartMinimizedCheckbox.Checked.ToString(), UpdateCheckerTimeNumeric.Value.ToString() }, "AutoStartSettings");

                LocalData.SaveFile(new List<string> { /*YgoProLinksPath.Text, */YgoProEsPath.Text }, "paths.txt");
            }
            catch
            {

            }
        }

        //borra los viejos CDB
        private void DeleteOldCdbs()
        {
            try
            {
                string cdbFolder = Path.Combine(YgoProEsPath.Text, "locales/es-ES");
                FileInfo[] cdbFiles = new DirectoryInfo(cdbFolder).GetFiles();
                foreach (FileInfo cdb in cdbFiles)
                {
                    if (cdb.Name.Contains("prerelease"))
                    {
                        cdb.Delete();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Access to YGOProEs denied. Check if the path is correct or\ntry launching the patcher with admin privileges.\n\nError Code:\n" + e.ToString());
                threadRunning = false;
                cancelButton.Visible = false;

            }
        }

        //Checkea nueva version de YgoPatcher
        private void CheckForNewVersionOfPatcher(string version)
        {
            try
            {
                Release release = GitAccess.GetNewestYgoProPatcherRelease();
                if (release.TagName != null && release.TagName != version && MessageBox.Show("New version of YgoProPatcher detected!\nDo You want to download it?", "New Version detected!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string dir;
                    FolderBrowserDialog fbd = new FolderBrowserDialog
                    {
                        ShowNewFolderButton = true,
                        SelectedPath = System.Windows.Forms.Application.StartupPath,
                        Description = "Select where you want to download YgoProPatcher ZIP File:"
                    };
                    DialogResult result = fbd.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        dir = fbd.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                    string fileName = Path.Combine(dir, "YgoProPatcher" + release.TagName + ".zip");
                    using (WebClient client = new WebClient())
                    {

                        client.DownloadFile(new System.Uri(release.Assets[0].BrowserDownloadUrl), fileName);
                    }
                    if (new FileInfo(fileName).Exists)
                    {
                        MessageBox.Show("New YgoProPatcher" + release.TagName + ".zip was succesfully\ndownloaded to the target location.\nPlease extract the newest release and use it!\n\nThis app will now close.", "Download completed!");
                        try
                        {
                            System.Diagnostics.Process.Start(fileName);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            Environment.Exit(0);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                if (!(e is AggregateException))
                {
                    MessageBox.Show("Couldn't check for new version of YgoProPatcher.\nMake sure You are connected to the internet or no program blocks the patcher!\n\n");
                }
            }
        }

        //Boton de finalizado
        private void FinishButton_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo YGOProES = new System.Diagnostics.ProcessStartInfo(Path.Combine(YgoProEsPath.Text, "YGOProES.exe"))
                {
                    WorkingDirectory = YgoProEsPath.Text
                };
                System.Diagnostics.Process.Start(YGOProES);
            }
            catch
            {

            }
            finally
            {
                this.Close();
            }

        }

        //checkeo de timer maximo
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            //GITHUB API LIMITS ARE 60/Hour, so minimum is 10mins.
            updateCheckerTimer.Interval = (double)UpdateCheckerTimeNumeric.Value * 60000 + 100;
            updateCheckerTimer.Elapsed += UpdateCheckerTimer_Tick;
            updateCheckerTimer.AutoReset = true;
            updateCheckerTimer.Enabled = true;
            nextUpdateTimer.Elapsed += NextUpdaterTimer_Tick;
            nextUpdateTimer.AutoReset = true;
            nextUpdateTimer.Start();
            UpdateCheckerTimer_Tick(sender, e);
            UpdateCheckerButton.Invoke(new Action(() => {
                UpdateCheckerButton.Enabled = true; UpdateCheckerButton.Text = "Click to turn off checking for updates.";
            }));

        }

        //tiempo checkeo tick
        private void UpdateCheckerTimer_Tick(object sender, EventArgs e)
        {

            if (UpdateChecker.CheckForUpdate())
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    SystemSounds.Hand.Play();
                    MessageBox.Show("New updates available!");

                }
                else if (this.WindowState == FormWindowState.Minimized && notifyIcon1.Visible)
                {
                    SystemSounds.Hand.Play();
                    notifyIcon1.ShowBalloonTip(6000, "New update for YGOProEs available!", "Click this to update!", ToolTipIcon.Info);
                    notifyIcon1.BalloonTipClicked += UpdateButton_Click;
                }
            }
            LocalData.SaveFile(new List<string>() { DateTime.Now.ToLocalTime().ToString(), updateCheckerTimer.Interval.ToString() }, "donotdeletethis");
            nextUpdateTimer.Stop();
            nextUpdateTimer.Interval = updateCheckerTimer.Interval / 10;
            nextUpdateTimer.Start();
        }

        //Siguien actualizacion tiempo
        private void NextUpdaterTimer_Tick(object sender, EventArgs e)
        {
            UpdateWhenLabel.Invoke(new Action(() => { UpdateWhenLabel.Text = "Next update in: " + (int)updateCheckerTimer.Interval / 60000 + " minutes"; }));

        }

        //Tiempo checkeo boton
        private void UpdateCheckerButton_Click(object sender, EventArgs e)
        {
            if (!updateCheckerTimer.Enabled && !backgroundWorker2.IsBusy && UpdateCheckerButton.Enabled)
            {
                UpdateCheckerButton.Enabled = false;
                backgroundWorker2.RunWorkerAsync();
            }
            else
            {
                updateCheckerTimer.Stop();
                nextUpdateTimer.Stop();
                UpdateWhenLabel.Text = "";
                UpdateCheckerButton.Text = "Check For New Updates";
                UpdateCheckerCooldownCheck();

            }


        }

        //Update de checkeo tiempo numerico
        private void UpdateCheckerTimeNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (updateCheckerTimer.Enabled)
            {
                updateCheckerTimer.Stop();
                updateCheckerTimer.Interval = ((double)UpdateCheckerTimeNumeric.Value * 60000) + 100;
                updateCheckerTimer.Start();

            }
            else
            {
                updateCheckerTimer.Interval = ((double)UpdateCheckerTimeNumeric.Value * 60000) + 100;

            }

        }

        //boton de maximizar
        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            contextMenu1.MenuItems.AddRange(new MenuItem[] { menuItemExit, menuItemStatus });
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Icon = this.Icon;
            menuItemExit.Index = 0;
            menuItemStatus.Index = 1;
            menuItemStatus.Text = Status.Text;
            menuItemExit.Text = "Exit";
            menuItemExit.Click += Cancel_Click;
            menuItemExit.Click += (object sender1, EventArgs e1) => { this.Close(); };
            notifyIcon1.ContextMenu = contextMenu1;
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }

        //notificacion de clickeo
        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            MaximizeForm(sender, e);
        }
        private void MaximizeForm(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        //Carga del Patcher
        private void YgoProPatcher_Load(object sender, EventArgs e)
        {
            List<string> settings = LocalData.LoadFileToList("AutoStartSettings");
            bool minimized = false;
            if (settings != null)
            {
                try
                {
                    minimized = Convert.ToBoolean(settings[0]);
                    StartMinimizedCheckbox.Checked = minimized;
                    UpdateCheckerTimeNumeric.Value = Convert.ToDecimal(settings[1]);
                }
                catch
                {

                }
            }
            if (minimized)
            {
                MinimizeButton_Click(sender, e);
                if (ButtonNotAvailableTimer.Enabled)
                {
                    ButtonNotAvailableTimer.Elapsed += (sender1, e1) => {
                        UpdateCheckerButton.Invoke(new Action(() =>
                        {
                            UpdateCheckerButton_Click(sender1, e1);
                        }));
                    };
                }
                else
                {
                    UpdateCheckerButton_Click(sender, e);
                }
            }

        }

    }

}
