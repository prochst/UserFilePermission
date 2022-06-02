using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.DirectoryServices.AccountManagement;
using System.ComponentModel;
using Microsoft.Win32;
using System.DirectoryServices.ActiveDirectory;
using Ookii.Dialogs.Wpf;
using System.Runtime.InteropServices;

namespace UserFilePermission
{
    /// <summary>
    /// Aplikace projde zadaný adresář včetně podadresářů do definované úrovně
    /// a vypíše souborová oprávnění pro zvoleného uživatele.
    /// Podle volby vypíče individuální oprávnšní nebo i oprávnění skupin,
    /// ve kterých je členem.
    /// Je určeno pro uživatele z Active Directory domeny a sdílené adresáře souborového serveru
    /// Aplikaci je nutné pouštět s admin oprávněnímpro proghledávané složky
    /// </summary>
    /// <settings>
    /// INI soubor config.ini je uložen v adresáři aplikace
    /// Formát:
    /// [Default setting]
    /// domain= mydomain.local
    /// levels = 3
    /// directoryexcluded= _osoba, _archiv
    /// </settings>
    public partial class MainWindow : Window
    {
        // Třída pro prohledávání
        CheckPermissions checkPermissions;
        public MainWindow()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            checkPermissions = new CheckPermissions();
            InitializeComponent();

            // Registrace událostí vlákna na pozadí
            checkPermissions.BackgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            checkPermissions.BackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            
            // Napojení formuláře na data
            DataContext = checkPermissions;
            dgVýsledek.DataContext = checkPermissions;
            dgVýsledek.ItemsSource = checkPermissions.Vystup;

            // Nastavení výchozích hodnot
            IniCfg iniCfg = new IniCfg(".\\config.ini"); 
            tbDomain.Text = iniCfg.IniReadValue("Default setting", "domain");
            tbUserName.Text = "";
            tbSearchDir.Text = "";
            tbNumLevels.Text = iniCfg.IniReadValue("Default setting", "levels"); ;
            tbDirectoryExcluded.Text = iniCfg.IniReadValue("Default setting", "directoryexcluded");
        }
       
        /// <summary>
        /// Spuštění prohledávání
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            // Kontrola a nastavení vstupních parametrů
            if (!SetInputParams())
                return;

            // Změna tlačítek
            btStart.Visibility = Visibility.Hidden;
            btStop.Visibility = Visibility.Visible;

            //Spustí vlastní prohledávání
            checkPermissions.FindPermission();
        }
        /// <summary>
        /// Předčasné ukončení prohledávání
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            checkPermissions.StopFind();

            // Změna tlačítek
            btStart.Visibility = Visibility.Visible;
            btStop.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Uložení nalezených oprávnění do CSV souboru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSaveLog_Click(object sender, RoutedEventArgs e)
        {
            System.IO.TextWriter txtWriter;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV file (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog() == true)
            {
                if (System.IO.File.Exists(saveFileDialog.FileName))
                    System.IO.File.Delete(saveFileDialog.FileName);

                
                txtWriter = new System.IO.StreamWriter(saveFileDialog.FileName, true, Encoding.GetEncoding(1250));

                txtWriter.WriteLine(String.Format("OPrávnění pro uživstele: {0} ve složce {1}", checkPermissions.UserName, checkPermissions.SearchDir));
                txtWriter.WriteLine(checkPermissions.getVysledek(";"));

                txtWriter.Flush();
                txtWriter.Close();
            }
        }

        /// <summary>
        /// Kontrola vstupních parametrů a jejich uložení
        /// </summary>
        /// <returns>
        /// bool
        ///     true - pramatry jsou správné
        ///     false - došlo k chybě
        /// </returns>
        private bool SetInputParams()
        {
            // Kontrola a uložení vstupích parametrů
            if (DomainExist(tbDomain.Text))
                checkPermissions.Domain = tbDomain.Text;
            else
            {
                MessageBox.Show("Domena neexistuje!", "Chyba");
                return false;
            }

            if (UserExists(tbUserName.Text))
                checkPermissions.UserName = tbUserName.Text;
            else
            {
                MessageBox.Show("Uživatel neexistuje!", "Chyba");
                return false;
            }

            if (Directory.Exists(tbSearchDir.Text))
                checkPermissions.SearchDir = tbSearchDir.Text;
            else
            {
                MessageBox.Show("Vstupní adrešář neexistuje!", "Chyba");
                return false;
            }

            int levels;
            if ((int.TryParse(tbNumLevels.Text, out levels) && levels >= -1))
                checkPermissions.NumLevels = levels;
            else
            {
                MessageBox.Show("Pčet úrovní musí být číslo větší než -1", "Chyba");
                return false;
            }

            checkPermissions.UserOnly = cbUserOnly.IsChecked.Value;
            checkPermissions.DirectoryExcluded = tbDirectoryExcluded.Text.Replace(", ", ",").Split(",");
            return true;
        }
        /// <summary>
        /// Otestuje za zadaný uživetel existuje v AD
        /// </summary>
        /// <param name="usrName"></param>
        /// <returns>bool</returns>
        private bool UserExists(string usrName)
        {
            string strDomain = checkPermissions.Domain;

            using (var domainContext = new PrincipalContext(ContextType.Domain, strDomain))
            {
                using (var user = new UserPrincipal(domainContext))
                {
                    user.SamAccountName = usrName;

                    using (var pS = new PrincipalSearcher())
                    {
                        pS.QueryFilter = user;

                        using (PrincipalSearchResult<Principal> results = pS.FindAll())
                        {
                            if (results != null && results.Count() > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Otestuje zda AD domána existuje a je dostupná
        /// </summary>
        /// <param name="domain"></param>
        /// <returns>bool</returns>
        private bool DomainExist(string domain)
        {
            HashSet<string> domains = new HashSet<string>();
            foreach (Domain d in Forest.GetCurrentForest().Domains)
            {
                domains.Add(d.Name.ToLower());
            }
            return domains.Contains(domain.ToLower());
        }
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            svVysledek.ScrollToBottom();
        }
        /// <summary>
        /// Ukončení prohledávání
        /// vlákno s vlastním prohledávání skončilo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Prohlédávání skončilo", "Info");
            btStart.Visibility = Visibility.Visible;
            btStop.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Dialog pro výběr strtovacího adresáře
        /// Vybraný adresář se nastavý do vstupního parametru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btselDir_Click(object sender, RoutedEventArgs e)
        {
            var Dialog = new VistaFolderBrowserDialog();
            if (Dialog.ShowDialog() == true)
            {
                tbSearchDir.Text = Dialog.SelectedPath;
            }
        }
    }

    /// <summary>
    /// Obecná ttřída pro čtení a zapis do INI souboru
    /// </summary>
    /// <param>
    /// string path - cesta a název INI souboru
    /// </param>
    /// <example>
    /// Obecný formát INI souboru:
    ///     [section1]
    ///     key1= value
    ///     key2= value
    ///     [section2]
    ///     key3= value
    /// </example>
    public class IniCfg
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniCfg(string INIPath)
        {
            path = INIPath;
        }

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
            return temp.ToString();
        }
    }
}
