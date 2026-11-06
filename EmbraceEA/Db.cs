using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace EmbraceEA
{
    /// <summary>
    /// All database work lives here (SQL Server). The database and tables are created and
    /// filled automatically the first time the program starts.
    /// </summary>
    static class Db
    {
        // LocalDB comes with Visual Studio / SQL Server Express. For a full SQL Server Express use  @".\SQLEXPRESS"
        const string Server = @"(localdb)\MSSQLLocalDB";
        const string DbName = "EmbraceEA";

        public static bool Ready = false;
        public static string Error = "";

        public static string NotReadyMessage
        {
            get
            {
                return "The database is not available, so this feature is switched off.\r\n\r\n" +
                       "Install SQL Server Express LocalDB (free) or change 'Server' at the top of Db.cs.\r\n\r\n" + Error;
            }
        }

        static string Conn(string db)
        {
            return "Server=" + Server + ";Database=" + db + ";Integrated Security=true;Connect Timeout=15;";
        }

        // ---------------- start-up: create database, tables and starter data ----------------
        public static void Init()
        {
            try
            {
                Run(Conn("master"), "IF DB_ID('" + DbName + "') IS NULL CREATE DATABASE [" + DbName + "]");

                string[] tables =
                {
                    "IF OBJECT_ID('Countries') IS NULL CREATE TABLE Countries (Id INT IDENTITY PRIMARY KEY, SortOrder INT, Name NVARCHAR(60), FlagFile NVARCHAR(60), Capital NVARCHAR(120), Joined NVARCHAR(120), Languages NVARCHAR(200), About NVARCHAR(MAX))",
                    "IF OBJECT_ID('Faqs') IS NULL CREATE TABLE Faqs (Id INT IDENTITY PRIMARY KEY, Question NVARCHAR(200), Answer NVARCHAR(MAX))",
                    "IF OBJECT_ID('Pages') IS NULL CREATE TABLE Pages (PageKey NVARCHAR(80) PRIMARY KEY, Title NVARCHAR(120), Body NVARCHAR(MAX))",
                    "IF OBJECT_ID('Workshops') IS NULL CREATE TABLE Workshops (Id INT IDENTITY PRIMARY KEY, Title NVARCHAR(200), EventDate DATE NULL, Venue NVARCHAR(200), Description NVARCHAR(MAX))",
                    "IF OBJECT_ID('News') IS NULL CREATE TABLE News (Id INT IDENTITY PRIMARY KEY, Title NVARCHAR(200), Body NVARCHAR(MAX), PostedOn DATETIME2 DEFAULT SYSDATETIME())",
                    "IF OBJECT_ID('Users') IS NULL CREATE TABLE Users (Id INT IDENTITY PRIMARY KEY, FullName NVARCHAR(120), Email NVARCHAR(200) NOT NULL UNIQUE, Salt VARBINARY(16), Hash VARBINARY(32), CreatedAt DATETIME2 DEFAULT SYSDATETIME())",
                    "IF OBJECT_ID('Messages') IS NULL CREATE TABLE Messages (Id INT IDENTITY PRIMARY KEY, UserId INT NULL, Body NVARCHAR(MAX), SentAt DATETIME2 DEFAULT SYSDATETIME())",
                    "IF OBJECT_ID('Questions') IS NULL CREATE TABLE Questions (Id INT IDENTITY PRIMARY KEY, UserId INT NULL, Body NVARCHAR(MAX), AskedAt DATETIME2 DEFAULT SYSDATETIME())"
                };
                foreach (string sql in tables) Run(Conn(DbName), sql);

                if (Count("Countries") == 0)
                    foreach (string[] r in CountryRows)
                        Run(Conn(DbName), "INSERT INTO Countries (SortOrder,Name,FlagFile,Capital,Joined,Languages,About) VALUES (@a,@b,@c,@d,@e,@f,@g)",
                            "@a", int.Parse(r[0]), "@b", r[1], "@c", r[2], "@d", r[3], "@e", r[4], "@f", r[5], "@g", r[6]);

                if (Count("Faqs") == 0)
                    foreach (string[] r in FaqRows)
                        Run(Conn(DbName), "INSERT INTO Faqs (Question,Answer) VALUES (@a,@b)", "@a", r[0], "@b", r[1]);

                if (Count("Pages") == 0)
                    foreach (string[] r in PageRows)
                        Run(Conn(DbName), "INSERT INTO Pages (PageKey,Title,Body) VALUES (@a,@b,@c)", "@a", r[0], "@b", r[1], "@c", r[2]);

                Ready = true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Ready = false;
            }
        }

        // ---------------- basic helpers ----------------
        // Parameters are passed as name, value, name, value ...
        static int Run(string connString, string sql, params object[] p)
        {
            using (SqlConnection c = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(sql, c))
            {
                for (int i = 0; i + 1 < p.Length; i += 2)
                    cmd.Parameters.AddWithValue((string)p[i], p[i + 1] ?? DBNull.Value);
                c.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        static int Count(string table)
        {
            using (SqlConnection c = new SqlConnection(Conn(DbName)))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM " + table, c))
            {
                c.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public static DataTable Query(string sql, params object[] p)
        {
            DataTable t = new DataTable();
            try
            {
                using (SqlConnection c = new SqlConnection(Conn(DbName)))
                using (SqlCommand cmd = new SqlCommand(sql, c))
                {
                    for (int i = 0; i + 1 < p.Length; i += 2)
                        cmd.Parameters.AddWithValue((string)p[i], p[i + 1] ?? DBNull.Value);
                    new SqlDataAdapter(cmd).Fill(t);
                }
            }
            catch (Exception ex) { Error = ex.Message; }
            return t;
        }

        /// <summary>Runs an INSERT/UPDATE. Returns null if it worked, otherwise an error message.</summary>
        public static string Save(string sql, params object[] p)
        {
            if (!Ready) return NotReadyMessage;
            try { Run(Conn(DbName), sql, p); return null; }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627) return "That email address is already registered.";
                return ex.Message;
            }
            catch (Exception ex) { return ex.Message; }
        }

        // ---------------- data used by the page (falls back to built-in data if there is no database) ----------------
        public static DataTable Countries()
        {
            if (Ready) return Query("SELECT * FROM Countries ORDER BY SortOrder");
            return FromRows(new string[] { "SortOrder", "Name", "FlagFile", "Capital", "Joined", "Languages", "About" }, CountryRows);
        }

        public static DataTable Faqs()
        {
            if (Ready) return Query("SELECT Question, Answer FROM Faqs ORDER BY Id");
            return FromRows(new string[] { "Question", "Answer" }, FaqRows);
        }

        public static string PageBody(string key)
        {
            if (Ready)
            {
                DataTable t = Query("SELECT Body FROM Pages WHERE PageKey=@k", "@k", key);
                if (t.Rows.Count > 0) return t.Rows[0][0].ToString();
            }
            else
            {
                foreach (string[] r in PageRows) if (r[0] == key) return r[2];
            }
            return "Content coming soon.";
        }

        static DataTable FromRows(string[] cols, string[][] rows)
        {
            DataTable t = new DataTable();
            foreach (string c in cols) t.Columns.Add(c);
            foreach (string[] r in rows) t.Rows.Add(r);
            return t;
        }

        // ---------------- accounts (passwords are salted + hashed, never stored as text) ----------------
        static byte[] Hash(string password, byte[] salt)
        {
            return new Rfc2898DeriveBytes(password, salt, 10000).GetBytes(32);
        }

        public static string Register(string name, string email, string password)
        {
            byte[] salt = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return Save("INSERT INTO Users (FullName,Email,Salt,Hash) VALUES (@n,@e,@s,@h)",
                        "@n", name, "@e", email.ToLower(), "@s", salt, "@h", Hash(password, salt));
        }

        public static bool Login(string email, string password, out int id, out string name)
        {
            id = 0; name = "";
            DataTable t = Query("SELECT Id, FullName, Salt, Hash FROM Users WHERE Email=@e", "@e", email.ToLower());
            if (t.Rows.Count == 0) return false;
            byte[] stored = (byte[])t.Rows[0]["Hash"];
            byte[] check = Hash(password, (byte[])t.Rows[0]["Salt"]);
            for (int i = 0; i < stored.Length; i++) if (stored[i] != check[i]) return false;
            id = (int)t.Rows[0]["Id"];
            name = t.Rows[0]["FullName"].ToString();
            return true;
        }

        // ---------------- starter data ----------------
        static readonly string[][] CountryRows =
        {
            new string[] { "1", "Uganda", "Uganda.png", "Kampala", "Founding member of the revived EAC (2000)", "English, Swahili",
                "A landlocked country on the shores of Lake Victoria, home to the source of the Nile at Jinja." },
            new string[] { "2", "Rwanda", "Rwanda.png", "Kigali", "Member since 2007", "Kinyarwanda, English, French, Swahili",
                "Known as the land of a thousand hills, Rwanda is a regional leader in technology, tourism and conservation." },
            new string[] { "3", "Burundi", "Burundi.png", "Gitega (political), Bujumbura (economic)", "Member since 2007", "Kirundi, French, English",
                "Burundi lies on the north-eastern shore of Lake Tanganyika and links East and Central Africa." },
            new string[] { "4", "Kenya", "Kenya.png", "Nairobi", "Founding member of the revived EAC (2000)", "English, Swahili",
                "Kenya is a major commercial and transport hub, with the port of Mombasa on the Indian Ocean." },
            new string[] { "5", "South Sudan", "SouthSudan.png", "Juba", "Member since 2016", "English",
                "Independent since 2011, South Sudan sits on the upper Nile and is the region's newest nation." },
            new string[] { "6", "DRC", "DRC.png", "Kinshasa", "Member since 2022", "French, Swahili, Lingala, Kikongo, Tshiluba",
                "The Democratic Republic of the Congo is the largest EAC member by area and brings Central Africa into the Community." },
            new string[] { "7", "Tanzania", "Tanzania.png", "Dodoma", "Founding member of the revived EAC (2000)", "Swahili, English",
                "Home to Mount Kilimanjaro, the Serengeti and Zanzibar, Tanzania is the cradle of the Swahili language." }
        };

        static readonly string[][] FaqRows =
        {
            new string[] { "What is Embrace East Africa?", "Embrace EA works to engender movement, bilateral relations and access to commerce across East Africa." },
            new string[] { "Which countries are covered?", "Uganda, Rwanda, Burundi, Kenya, South Sudan, the DRC and Tanzania." },
            new string[] { "How can I join a workshop?", "Open Workshops in the menu, or send us a message with the Get in Touch form." },
            new string[] { "How do I create an account?", "Click Sign In / Sign Up in the menu and choose \"I'm new - create an account\"." }
        };

        static readonly string[][] PageRows =
        {
            new string[] { "About Us", "About Us",
                "Embrace East Africa (Embrace EA) is dedicated to engendering movement, bilateral relations and access to commerce in East Africa.\r\n\r\nOur work brings the people of Uganda, Rwanda, Burundi, Kenya, South Sudan, the DRC and Tanzania closer together." },
            new string[] { "Services", "Services",
                "Embrace EA supports movement, bilateral relations and access to commerce through training, advocacy, networking and information services.\r\n\r\nEdit this text in the Pages table of the database." },
            new string[] { "East African Anthem", "East African Anthem",
                "The East African Community anthem celebrates the unity and shared future of the Partner States.\r\n\r\nEdit this text in the Pages table of the database." },
            new string[] { "East African Heads of States", "East African Heads of States",
                "The Heads of State of the East African Community Partner States will be listed here.\r\n\r\nEdit this text in the Pages table of the database." },
            new string[] { "Learn Swahili", "Learn Swahili",
                "Habari  -  Hello / How are you?\r\nKaribu  -  Welcome\r\nAsante  -  Thank you\r\nTafadhali  -  Please\r\nPole  -  Sorry\r\nNdiyo / Hapana  -  Yes / No\r\nKwa heri  -  Goodbye\r\nUmoja  -  Unity" },
            new string[] { "About East African Passport", "About East African Passport",
                "The East African Passport is a common travel document designed to make it easier for citizens of the Partner States to move across the region.\r\n\r\nEdit this text in the Pages table of the database." }
        };
    }
}
