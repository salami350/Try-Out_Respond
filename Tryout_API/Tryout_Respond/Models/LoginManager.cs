﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Tryout_Respond.Models
{
    public class LoginManager
    {
        private DatabaseConnection databaseConnection = new DatabaseConnection();
        private Misc misc = new Misc();

        public string Authenticate(string username, string unencryptedPassword)
        {
            var token = String.Empty;

            if (username.Any(character => !Char.IsLetterOrDigit(character)) || unencryptedPassword.Any(character => !Char.IsLetterOrDigit(character)))
            {
                return token;
            }

            if (unencryptedPassword.Length < Constants.MINIMALPASSWORDLENGTH || unencryptedPassword.Length > Constants.MAXIMUMPASSWORDLENGTH)
            {
                return token;
            }

            var passwordHash = misc.HashPassword(unencryptedPassword);

            if (!databaseConnection.IsAccountOwnerCredentials(username, passwordHash))
            {
                return token;
            }

            token = Guid.NewGuid().ToString();
            var expirationDate = DateTime.UtcNow.AddMinutes(Constants.TOKENLIFETIME)/*.ToString("yyyyMMdd HH:mm")*/;

            if (!databaseConnection.SetToken(token, expirationDate, username))
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

            if (misc.AccountExistsUsername(username))
            {
                return passwordHash;
            }

            var unencryptedPassword = Guid.NewGuid().ToString().Replace("-", "").Substring(0, Constants.MAXIMUMPASSWORDLENGTH);
            passwordHash = misc.HashPassword(unencryptedPassword);

            string userID = misc.GenerateUserID();

            var isAdmin = false;
            if (!databaseConnection.InsertUser(userID, username, passwordHash, isAdmin = false))
            {
                return passwordHash = String.Empty;
            }

            return unencryptedPassword;
        }

        public String RefreshToken(string oldToken)
        {
            var newToken = Guid.NewGuid().ToString();
            var expirationDate = DateTime.UtcNow.AddMinutes(Constants.TOKENLIFETIME)/*.ToString("yyyyMMdd HH:mm")*/;

            if (!databaseConnection.RefreshToken(oldToken, newToken, expirationDate))
            {
                return newToken = String.Empty;
            }

            return newToken;
        }

        public bool DeleteToken(string token)
        {
            return databaseConnection.DeleteToken(token);
        }
    }
}