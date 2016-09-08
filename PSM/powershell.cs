using PSMonitor.Stores;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSMonitor.Powershell
{

    public class Powershell : IDisposable
    {


        private List<Script> _scripts = new List<Script>();

        public Powershell() { }

        public void Dispose()
        {

            _scripts.ForEach(script => script.Dispose());
            _scripts.Clear();
            

        }


        public void Reload(string path = null)
        {

            Logger.Info("Powershell : Reloading...");

            DirectoryInfo dir = new DirectoryInfo(Setup.Powershell.Directory);
            IEnumerable<FileInfo> files = dir.EnumerateFiles("*.ps1");
            Random random = new Random();

            int result = 0;

            foreach (FileInfo file in files)
            {
                if (path != null && path != file.FullName)
                    continue;

                int i = _scripts.FindIndex(s => { return s.File.FullName == file.FullName; });

                if (i == -1)
                {
                    _scripts.Add(new Script(file));
                    Logger.Info(String.Format("Powershell : {0} was added", file.Name));
                }
                
                else
                {
                    _scripts[i].Dispose();
                    _scripts[i] = new Script(_scripts[i]);
                }
            }

            if (result > 0)
                Logger.Info("Powershell : " + result + " scripts was added");

            result = 0;

            foreach (Script s in _scripts.ToArray())
            {
                if (!File.Exists(s.File.FullName))
                {
                    result++;
                    _scripts.Remove(s);
                    s.Dispose();
                    Logger.Info(String.Format("Powershell : {0} was removed", s.File.Name));
                }
            }

            if (result > 0)
                Logger.Info("Powershell : " + result + " scripts was removed");

            foreach (Script script in _scripts)
            {

                if (path != null && path != script.File.FullName)
                    continue;


                Logger.Info("Powershell : Loaded script: " + script.File.FullName);

                script.Context.Start(OnData);
                Thread.Sleep((int)(random.NextDouble() * 1000));
            }



        }

        private object OnData(Envelope data)
        {

            IStore store = PSM.Store();

            if (data == null)
                return null;
            
            Debug.WriteLine(data);

            if (store != null)
            {

                try
                {
                    store.Write(data);
                    Logger.Info(String.Format("Powershell : Sent {0} entries to the store ({1})", data.Entries != null ? data.Entries.Length : 0, store.GetType()));
                }
                catch (Exception error)
                {
                    Logger.Error(error.Message);
                }

            }

            return null;
        }


    }

}
