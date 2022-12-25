﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GenLauncherNet.Windows;

namespace GenLauncherNet
{
    /// <summary>
    /// Логика взаимодействия для InitWindow.xaml
    /// </summary>
    public partial class InitWindow : Window
    {
        private HttpClient client = new HttpClient();

        public InitWindow()
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GenLauncher", "1"));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var firstRun = true;
            if (Directory.Exists(EntryPoint.GenLauncherModsFolder) || Directory.Exists(EntryPoint.GenLauncherModsFolderOld))
                firstRun = false;

            await Task.Run(() => CreateLauncherFolders());

            EntryPoint.SessionInfo.Connected = await PrepareLauncher();
            DataHandler.FirstRun = firstRun;

            this.Hide();
            MainWindow mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            this.Close();
        }

        public async Task<bool> PrepareLauncher()
        {
            var connected = await CheckConnection();

            await DataHandler.InitData(connected);

            if (DataHandler.ReposModsNames == null)
                connected = false;

            if (connected)
            {
                var lastActivatedMod = DataHandler.GetSelectedMod();

                if (lastActivatedMod != null)
                    await DataHandler.ReadPatchesAndAddonsForMod(lastActivatedMod);
            }
            else
                MessageBox.Show("Cannot connect to https://github.com/, installing modifications from internet is not available!");

            client.Dispose();

            return connected;
        }

        private async Task<bool> CheckConnection()
        {
            try
            {
                var response = await client.GetAsync(EntryPoint.ModsRepos, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();
            }
            catch 
            {
                return false;
            }
            return true;
        }

        private static void CreateLauncherFolders()
        {
            CreateFolder(EntryPoint.GenLauncherModsFolder);
        }

        private static void CreateFolder(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);

            if (Directory.Exists(EntryPoint.GenLauncherModsFolderOld))
            {
                MoveContentsOfDirectory(EntryPoint.GenLauncherModsFolderOld, folderName);
            }
        }

        private static void MoveContentsOfDirectory(string source, string target)
        {
            foreach (var file in Directory.EnumerateFiles(source))
            {
                var dest = Path.Combine(target, Path.GetFileName(file));
                File.Move(file, dest);
            }

            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                var dest = Path.Combine(target, Path.GetFileName(dir));
                Directory.Move(dir, dest);
            }

            Directory.Delete(source);
        }
    }
}
