using FirebirdSql.Data.FirebirdClient;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using WpfApp.Core;

namespace WpfApp {
    class AuthenticationProvider : IAuthenticationProvider {
        public string Login { get; private set; }

        public string Password { get; private set; }

        public string[] Roles { get; private set; }

        public bool CheckAutentication(string login, string password) {
            try {
                using (IDbConnection conn = GetDbConnection(login, password)) {
                    Roles = GetUserRoles(login, conn);
                    Login = login;
                    Password = password;
                    return true;
                }
            } catch (FbException ex) {
                Login = null;
                Password = null;
                Roles = null;
                if (ex.SQLSTATE == "28000") {
                    return false;
                } else throw;
            }
        }

        string[] GetUserRoles(string user, IDbConnection connection) {
            List<string> result = new List<string>();
            using (IDbCommand cmd = connection.CreateCommand()) {
                cmd.CommandText = @"SELECT 
                        trim(u.RDB$RELATION_NAME)
                        FROM RDB$USER_PRIVILEGES u
                        WHERE u.RDB$PRIVILEGE = 'M'
                          and upper(RDB$USER) = upper('" + user + @"')
                        ORDER BY 1";
                using (IDataReader rdr = cmd.ExecuteReader()) {
                    while (rdr.Read()) {
                        result.Add(rdr[0].ToString());
                    }
                }
            }
            return result.ToArray();
        }

        IDbConnection GetDbConnection(string login, string password) {
            string connStr = string.Format(ConfigurationManager.ConnectionStrings["DataContext"].ConnectionString, login, password, ""); 
            FbConnection connection = new FbConnection(connStr);
            connection.Open();
            return connection;
        }
    }
}
