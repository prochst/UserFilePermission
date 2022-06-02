using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.AccessControl;
using System.Threading;

namespace UserFilePermission
{
    /// <summary>
    /// Prohledá adresáře do určené hloubky a vypíše oprávnění zadaného uživatele a případně u skupin, ve kterých je členem
    /// Je určeno pro uživatele z Active Directory domeny a sdílené adresáře souborového serveru
    /// </summary>
    class CheckPermissions : INotifyPropertyChanged
    {
        // Nastavení vlákna pro běh na pozadí
        public BackgroundWorker BackgroundWorker { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string vlastnost) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(vlastnost));

        // Přihlašovací jméno uživatele, pro kterého vypisujeme oprávnění - AD účet
        public string UserName { get; set; }

        // Active directory doména s kterou pracujeme
        public string Domain { get; set; }

        // Startovací adresář pro prohledávání
        public string SearchDir { get; set; }

        // Počet úrovní které se budou prohledávat
        public int NumLevels { get; set; }

        // Vypíší se jen oprávnění uživatele nebo i skupin, kterých je členem
        public bool UserOnly { get; set; }

        // Seznam adresářů oddělených čárkou, kterí se při procházení vynechají
        // zadávají se jen názvy bez cesty
        public string[] DirectoryExcluded { get; set; }

        // Kolekce výstupních řádků s nalezeným oprávněním
        public ObservableCollection<Radek> Vystup { get; private set; }

        // Seznam AD skupin, ve kterých je uživatel členem
        private List<string> UserGroups { get; set; }

        public CheckPermissions()
        {
            // Init vlákna pro běh na pozadí
            BackgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            BackgroundWorker.DoWork += BackgroundWorker_DoWork;

            Vystup = new ObservableCollection<Radek>();
            UserGroups = new List<string>();
        }

        /// <summary>
        /// Vstupní metoda pro zahájení prohledávání
        /// spustí vlastní hledání v novém vlákně na pozadí
        /// </summary>
        public void FindPermission()
        {
            //Nastaví context, ve ktrém se budou aktualizat UI prvky z Backgroud procesu)
            var uiContext = SynchronizationContext.Current;
            // Spustí vlákno na pozadí, které zjišťuje oprávnění BackgroundWorker_DoWork
            BackgroundWorker.RunWorkerAsync(uiContext);
        }

        // Přerušení procesu prohledávání na pozadí
        public void StopFind()
        {
            BackgroundWorker.CancelAsync();
        }

        /// <summary>
        /// Vrátí výsledky prohledávání jako text s hodnotani oddělenými definovaným oddělovačem
        /// </summary>
        /// <param name="delimetr">Oddělovač hodnot</param>
        /// <returns>String</returns>
        public string getVysledek(string delimetr)
        {
            string retVal = Radek.Header(delimetr);

            foreach (Radek radek in Vystup)
            {
                retVal += radek.ToDelString(delimetr);
            }

            return retVal;
        }
        /// <summary>
        /// Spustí na pozadí nové vlákno pro prohledání 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SynchronizationContext uiContext = (SynchronizationContext)e.Argument; // Synchronizační content pro aktualizaci prků bindovaných do UI)
            uiContext.Send(x => Vystup.Clear(), null);

            // Pokud se mají vypisovat i skupinová oprávnění, načte seznam skupin, ve kterých je uživatel členem
            if (!UserOnly)
            {
                PrincipalContext context = new PrincipalContext(ContextType.Domain);
                UserPrincipal userId = UserPrincipal.FindByIdentity(context, UserName);

                PrincipalSearchResult<Principal> groups = userId.GetGroups();
                foreach (GroupPrincipal g in groups)
                {
                    UserGroups.Add(g.Name);
                }
            }

            // rekurzivně projde adresáře a otestuje oprávnění
            CheckDirSecurity(SearchDir, 0, NumLevels, uiContext);
            BackgroundWorker.ReportProgress(100);
        }

        /// <summary>
        /// Rekurzivně projde celý strom podadresářů a zjisí zada má uživatel nějaké oprávnění k danému adresáři
        /// Parametrem levels lze omezit hloubku procházení
        /// </summary>
        /// <param name="dir">Počáteční adresář</param>
        /// <param name="curLevel"></param>
        /// <param name="levels">Počet úrovní procházení, -1 znamená všechny</param>
        private void CheckDirSecurity(string dir, int curLevel, int levels, SynchronizationContext sc)
        {
            bool isExcluded;
            bool isLevel;

            //string[] aDirs;
            try
            {
                // zpracuj počáteční adresář
                if (dir == SearchDir)
                {
                    if (HasPermission(dir, curLevel) != null)
                    {
                        sc.Send(x => Vystup.Add(HasPermission(dir, curLevel)), null);
                        //Vystup.Add(HasPermission(dir, curLevel));
                        OnPropertyChanged(nameof(Vystup));
                    }
                }

                // zjisti podadresáře
                string[] dirs = Directory.GetDirectories(@dir);
                foreach (string directory in dirs)
                {
                    // kontrola zda není hledání přerušeno
                    if (BackgroundWorker.CancellationPending)
                        break;
                    // refresh formuláře
                    BackgroundWorker.ReportProgress(50);

                    // konrola na vynachané adresáře
                    isExcluded = false;
                    foreach (string exDir in DirectoryExcluded)
                    {
                        if (directory.ToLower().Contains(exDir))
                            isExcluded = true;
                    }
                    // kontrola počtu úrovní vnoření
                    isLevel = true;
                    if (levels > -1 && curLevel >= levels)
                        isLevel = false;

                    if (isLevel && !isExcluded)
                    {
                        if (HasPermission(directory, curLevel) != null)
                        {
                            sc.Send(x => Vystup.Add(HasPermission(directory, curLevel)), null);
                            //Vystup.Add(HasPermission(directory, curLevel));
                            OnPropertyChanged(nameof(Vystup));
                        }
                        CheckDirSecurity(@directory, curLevel + 1, levels, sc);
                    }

                }

            }
            catch (Exception ex)
            {
                Radek radek = new Radek();
                radek.Directory = dir;
                radek.Permission = "Chyba při procházení adreářů";
                radek.PermType = ex.Message;
                sc.Send(x => Vystup.Add(radek), null);
                //Vystup.Add(radek);
                return;
            }
        }

        /// <summary>
        /// Zkontloluje zda uživatel UserName má nějaká nezděděná oprávnédí v daném adresáři
        /// Pokud ano vrátí typ oprávnění, pokud ne vráati ""
        /// </summary>
        /// <param name="dir">Adresář</param>
        /// <returns>Radek nebo ""</returns>

        private Radek HasPermission(string directory, int curLevel)
        {
            Radek retVal = new Radek();

            try
            {
                DirectoryInfo dInfo = new DirectoryInfo(@directory);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

                foreach (FileSystemAccessRule ace in acl)
                {
                    // prohledá se za je v seznamu oprávnění uživatel
                    if (!ace.IsInherited && ace.IdentityReference.Value.ToUpper().Contains(UserName.ToUpper()))
                    {
                        retVal.Directory = directory;
                        retVal.Permission = ace.FileSystemRights.ToString();
                        retVal.PermObject = ace.IdentityReference.Value;
                        retVal.PermType = ace.AccessControlType.ToString();
                        retVal.AplliesTo = ace.InheritanceFlags.ToString();
                        return retVal;
                    }
                    //prohledávají se i skupny ve kterých je uživatel členem, pokud je to požadováno - UserOnly = false
                    if (!ace.IsInherited && !UserOnly)
                    {
                        foreach (string grp in UserGroups)
                        {
                            if (ace.IdentityReference.Value.ToUpper().Contains(grp.ToUpper()))
                            {
                                retVal.Directory = directory;
                                retVal.Permission = ace.FileSystemRights.ToString();
                                retVal.PermObject = ace.IdentityReference.Value;
                                retVal.PermType = ace.AccessControlType.ToString();
                                retVal.AplliesTo = ace.InheritanceFlags.ToString();
                                return retVal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retVal.Directory = directory;
                retVal.Permission = "CHyba při načítání oprávnění";
                retVal.PermType = ex.Message;
                return retVal;
            }

            return null;
        }

        /// <summary>
        /// Přeloží typ oprávnění do češtiny a zjedndoší na Číst, Zapisovat, Vše
        /// -- není použito
        /// </summary>
        /// <param name="permition"></param>
        /// <returns>zjednodušený český ekvivalent druhu oprávnění</returns>
        private string translatePermition(string permition)
        {
            string retval;
            switch (permition)
            {
                case "ReadAndExecute, Synchronize":
                    retval = "Číst";
                    break;
                case "ReadData, ExecuteFile, Synchronize":
                    retval = "Číst";
                    break;
                case "Modify, Synchronize":
                    retval = "Vše";
                    break;
                case "Write, ReadAndExecute, Synchronize":
                    retval = "Zapisovat";
                    break;
                case "FullControl":
                    retval = "Vše";
                    break;
                default:
                    retval = permition;
                    break;
            }
            return retval;
        }
    }

    /// <summary>
    /// Pomocná třída pro výpis jednoho řádku výsledků hledání
    /// </summary>
    public class Radek
    {
        // Adresář na kterém je oprávnění nalezeno
        public string Directory { get; set; }
        // Druh oprávnění (Read, Modify ,...)
        public string Permission { get; set; }
        // Kdo má oprávnění nastaveno (User, Group)
        public string PermObject { get; set; }
        // Typ oprávnění (Alĺow, Permit)
        public string PermType { get; set; }
        // Oblast platnosti oprávnění (dědičnost)
        public string AplliesTo { get; set; }

        public Radek() { }
        public static string Header(string delimiter)
        {
            return String.Format("Složka{0}Oprávnění{0}Typ{0}Platí pro\n", delimiter);
        }
        /// <summary>
        /// Vypíše nalezané oprávnění do jednoho textového řádku, 
        /// hodnoty jsou oddělený zadaným oddělovačem
        /// </summary>
        /// <param name="delimiter">Oddělovač hodnot v textovém řetezci</param>
        /// <returns>String</returns>
        public string ToDelString(string delimiter)
        {
            return Directory + delimiter + Permission + delimiter + PermType + delimiter + AplliesTo + "\n";
        }
    }
}
