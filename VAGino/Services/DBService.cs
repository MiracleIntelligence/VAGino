using SQLite;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VAGino.Models;

namespace VAGino.Services
{
    public class DBService
    {
        public SQLiteConnection Connection { get; private set; }

        public void Init()
        {
            // Get an absolute path to the database file
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VAGinoData.db");

            var db = new SQLiteConnection(databasePath);
            Connection = db;

            Connection.CreateTable<VAGBlock>();
            Connection.CreateTable<FilteredBlock>();
        }
    }
}
