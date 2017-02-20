﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Tryout_Respond.Models;

namespace Tryout_Respond.Controllers
{
    [RoutePrefix("api/users")]
    public class UserController : ApiController
    {
        private DatabaseConnection databaseConnection;
        private Misc misc;
        private UserManager userManager;
        private LoginManager loginManager;

        public UserController()
        {
            databaseConnection = new DatabaseConnection();
            misc = new Misc(databaseConnection);
            userManager = new UserManager(databaseConnection, misc);
            loginManager = new LoginManager(databaseConnection, userManager, misc);
        }

        [HttpGet]
        [Route("")]
        public HttpResponseMessage Get/*Accounts*/()
        {
            try
            {
                if (!Request.Headers.GetValues("token").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("not logged in"));
                }

                string token = Request.Headers.GetValues("token").SingleOrDefault();

                if (String.IsNullOrWhiteSpace(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }

                if (!misc.IsTokenValid(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("session expired"));
                }

                var users = userManager.GetAccounts();

                string JsonAllUsers = JsonConvert.SerializeObject(new { users = users});

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonAllUsers);

                return response;
            }
            catch (InvalidOperationException invalidOperationException)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
            }
        }

        [HttpGet]
        [Route("{userID}")]
        public HttpResponseMessage GetAccount(string userID)
        {
            try
            {
                if (!Request.Headers.GetValues("token").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("not logged in"));
                }

                string token = Request.Headers.GetValues("token").SingleOrDefault();

                if (String.IsNullOrWhiteSpace(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }

                if (!misc.IsTokenValid(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("session expired"));
                }

                string JsonUser = JsonConvert.SerializeObject(new { user = userManager.GetUserByID(userID) });

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonUser);

                return response;
                
            }
            catch (InvalidOperationException invalidOperationException)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
            }
        }

        [HttpPost]
        [Route("{userID}/makeadmin")]
        public HttpResponseMessage MakeAdmin(string userID)
        {
            try
            {
                if (!Request.Headers.GetValues("token").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("not logged in"));
                }

                string token = Request.Headers.GetValues("token").FirstOrDefault();

                if (String.IsNullOrWhiteSpace(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }

                if (!misc.IsTokenValid(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("session expired"));
                }

                User user = userManager.GetUser(userID);

                if(user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("no access rights"));
                }

                if(!user.isAdmin)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("no access rights"));
                }

                var isAdmin = true;

                if (!userManager.MakeAdmin(userID, isAdmin))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }
                string JsonUserID = JsonConvert.SerializeObject(new { userID = userID });
                string JsonText = JsonConvert.SerializeObject(new { text = " set to admin" });

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonUserID + JsonText);

                return response;

            }
            catch (InvalidOperationException invalidOperationException)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
            }
        }

        [HttpPost]
        [Route("{userID}/changeUsername")]
        public HttpResponseMessage ChangeUsername(string userID)
        {
            try
            {
                if (!Request.Headers.GetValues("token").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("not logged in"));
                }

                if (!Request.Headers.GetValues("newUsername").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }

                string token = Request.Headers.GetValues("token").SingleOrDefault();
                string newUsername = Request.Headers.GetValues("newUsername").SingleOrDefault();

                if (String.IsNullOrWhiteSpace(token) || String.IsNullOrWhiteSpace(newUsername))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
                }

                if (!misc.IsTokenValid(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("session expired"));
                }

                User user = userManager.GetUserByIDWithToken(userID);

                if(user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("invalid link"));
                }

                //if (!misc.IsAccountOwner(token, userID))
                if(user.token == token)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("no access rights"));
                }

                //if(misc.AccountExistsUsername(newUsername))

                User user2 = userManager.GetUser(newUsername);
                if(user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("username in use"));
                }

                if (!userManager.SetUsername(token, newUsername))
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, JsonConvert.SerializeObject("username change failed"));
                }

                string JsonText = JsonConvert.SerializeObject(new { text = "Username veranderd" });

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonText);

                return response;
            }
            catch (InvalidOperationException invalidOperationsException)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
            }
        }

        [HttpPost]
        [Route("{userID}/changePassword")]
        public HttpResponseMessage ChangePassword(string userID)
        {
            try
            {
                if (!Request.Headers.GetValues("token").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("not logged in"));
                }

                if (!Request.Headers.GetValues("newPassword").Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("invalid credentials"));
                }

                string token = Request.Headers.GetValues("token").SingleOrDefault();
                string unencryptedNewPassword = Request.Headers.GetValues("newPassword").SingleOrDefault();

                if (String.IsNullOrWhiteSpace(token) || String.IsNullOrWhiteSpace(unencryptedNewPassword))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("invalid credentials"));
                }

                if (!misc.IsTokenValid(token))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("session expired"));
                }

                //if(!misc.IsAccountOwner(token, userID))
                User user = userManager.GetUserByIDWithToken(userID);
                if(user.token != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("no access rights"));
                }

                if (!userManager.ChangePassword(token, userID, unencryptedNewPassword))
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, JsonConvert.SerializeObject("change password failed"));
                }

                string JsonPassword = JsonConvert.SerializeObject(new { password = unencryptedNewPassword });

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonConvert.SerializeObject(JsonPassword);

                return response;

            }
            catch (InvalidOperationException invalidOperationException)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, JsonConvert.SerializeObject("credentials invalid"));
            }
        }
    }
}
