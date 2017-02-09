﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Tryout_Respond.Models
{
    public class AccountManager
    {
        public const int MINIMALPASSWORDLENGTH = 8;
        public const int TOKENLIFETIME = 20; //minutes

        DatabaseConnection connection = new DatabaseConnection();

        public string Authenticate(string username, string unencryptedPassword)
        {
            var token = String.Empty;

            if(username.Any(character => !Char.IsLetterOrDigit(character)) || unencryptedPassword.Any(character => !Char.IsLetterOrDigit(character)))
            {
                return token;
            }

            /*MySqlCommand cmd = MySqlConn.cmd;
            cmd = new MySqlCommand(
                "SELECT count(*) FROM admin " +
                "WHERE admin_username=@username " +
                "AND admin_password=PASSWORD(@passwd)",
                MySqlConn.conn);
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@passwd", password);
            int result = (int)cmd.ExecuteReader();

            // Returns true when username and password match:
            return (result > 0);*/

            /*SqlCommand sqlCommand = connection.*/

            var passwordHash = Encoding.ASCII.GetString(new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(unencryptedPassword)));

            IList<object[]> results = connection.RunQuery("SELECT Username FROM Users WHERE username = '" + username + "' AND passwordHash = '" + passwordHash + "'");

            if (!results.Any())
            {
                return token;
            }

            token = Guid.NewGuid().ToString();
            var expirationDate = DateTime.UtcNow.AddMinutes(TOKENLIFETIME).ToString("yyyyMMdd HH:mm");

            if (!connection.RunNonQuery("UPDATE Users SET token='" + token + "', token_expirationDate='" + expirationDate + "' WHERE username = '" + username + "'"))
            {
                return token = String.Empty;
            }

            return token;

        }

        public string Register(string username)
        {
            var passwordHash = String.Empty;

            if (username.Any(character => !Char.IsLetterOrDigit(character)))
            {
                return passwordHash;
            }

            if (AccountExists(username))
            {
                return passwordHash;
            }

            var unencryptedPassword = Guid.NewGuid().ToString().Replace("-", "").Substring(0, MINIMALPASSWORDLENGTH);
            passwordHash = Encoding.ASCII.GetString(new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(unencryptedPassword)));

            string userID = GenerateUserID();

            if (!connection.RunNonQuery("INSERT INTO Users(userID, username, passwordHash) VALUES('" + userID + "', '" + username + "', '" + passwordHash + "')"))
            {
                return passwordHash = String.Empty;
            }

            return unencryptedPassword;
        }

        public string GenerateUserID()
        {
            var userID = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 4);

            if(connection.RunQuery("SELECT userID FROM Users WHERE userID = '" + userID + "'").Any())
            {
                userID = GenerateUserID();
            }

            return userID;
        }

        private bool AccountExists(string username)
        {
            IList<object[]> result = connection.RunQuery("SELECT userID FROM Users WHERE username = '" + username + "'");

            return result.Any();
        }

        public bool isTokenValid(string token)
        {
            IList<object[]> results = connection.RunQuery("SELECT token FROM Users WHERE token = '" + token + "' AND token_expirationDate > '" + DateTime.UtcNow.ToString("yyyyMMdd HH:mm") + "'");

            return results.Any();
        }

        public String GetOwnAccountInfo(string token)
        {
            var accountInfo = String.Empty;

            IList<object[]> results = connection.RunQuery("SELECT username, passwordHash FROM Users WHERE token = '" + token + "'");

            foreach (object accountField in results.First())
            {
                accountInfo += accountField + ":";
            }

            return accountInfo;
        }

        public String RefreshToken(string oldToken)
        {
            var newToken = Guid.NewGuid().ToString();
            var expirationDate = DateTime.UtcNow.AddMinutes(TOKENLIFETIME).ToString("yyyyMMdd HH:mm");

            if (!connection.RunNonQuery("UPDATE Users SET token='" + newToken + "', token_expirationDate='" + expirationDate + "' WHERE token = '" + oldToken + "'"))
            {
                return newToken = String.Empty;
            }

            return newToken;
        }
    }
}