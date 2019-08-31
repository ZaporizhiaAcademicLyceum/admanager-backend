using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;
using admanager_backend.Models;

namespace admanager_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ADUserController : ControllerBase
    {
        private static readonly string AD_USER = Environment.GetEnvironmentVariable("AD_USER");
        private static readonly string AD_PASS = Environment.GetEnvironmentVariable("AD_PASS");
        private static readonly string AD_DC_HOST = Environment.GetEnvironmentVariable("AD_DC_HOST");
        private static readonly string SECRET_KEY = Environment.GetEnvironmentVariable("SECRET_KEY");

        private PrincipalContext _principalSearch;
        PrincipalContext RootSearchContext
        {
            get
            {
                if (_principalSearch == null)
                {
                    _principalSearch = new PrincipalContext(ContextType.Domain, AD_DC_HOST, AD_USER, AD_PASS);
                }
                return _principalSearch;
            }
        }

        [Route("[action]")]
        public object Debug()
        {
            return new { health = "ok" };
        }

        public class PostData
        {
            public string secretkey;
            public ADUser user;
        }

        [HttpPost]
        public object Post(PostData data)
        {
            string res = "ERROR: Unknown";
            if (data.secretkey != SECRET_KEY)
            {
                res = "ERROR: secretkey";
            }
            else
            {
                UserPrincipal userPrincipial = UserPrincipal.FindByIdentity(RootSearchContext, IdentityType.SamAccountName, data.user.Username);
                if (userPrincipial != null)
                {
                    res = "ERROR: Account already exists";
                }
                else
                {
                    try
                    {
                        CreateUser(data.user);
                        res = "OK";
                    }
                    catch (Exception e)
                    {
                        res = "ERROR: " + e.Message;
                    }
                }
            }
            return new { data = res };
        }

        private void CreateUser(ADUser postUser)
        {
            var containerOU = new PrincipalContext(ContextType.Domain, AD_DC_HOST, postUser.OU, AD_USER, AD_PASS);
            var newUser = new UserPrincipal(containerOU, postUser.Username, postUser.Password, true);

            newUser.UserPrincipalName = postUser.Username + "@academlyceum.zp.ua";
            newUser.Name = postUser.Firstname + " " + postUser.Lastname;
            newUser.GivenName = postUser.Firstname;
            newUser.Surname = postUser.Lastname;
            newUser.DisplayName = postUser.Firstname + " " + postUser.Lastname;
            newUser.HomeDirectory = postUser.HomeDir;
            newUser.HomeDrive = "H:";
            newUser.Save();

            // GetUnderlyingObject() method can be called only after Save().
            DirectoryEntry rawEntry = newUser.GetUnderlyingObject() as DirectoryEntry;
            rawEntry.Properties["department"].Value = postUser.Department;
            rawEntry.CommitChanges();

            AddUserToGroup(postUser, newUser);
            CreateHomeDir(postUser.HomeDir, postUser.Username);
        }

        private void AddUserToGroup(ADUser postUser, UserPrincipal newUser)
        {
            GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(RootSearchContext, postUser.Unit);
            if (groupPrincipal == null)
            {
                throw new Exception("Group doesn't exists");
            }
            else
            {
                groupPrincipal.Members.Add(newUser);
                groupPrincipal.Save();
                groupPrincipal.Dispose();
            }
        }

        private void CreateHomeDir(string uncPath, string username)
        {
            var dir = Directory.CreateDirectory(uncPath);

            var security = dir.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(username,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow)
            );

            dir.SetAccessControl(security);
        }
    }
}