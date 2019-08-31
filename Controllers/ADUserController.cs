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
        private PrincipalContext _principalSearch;
        PrincipalContext SearchContext
        {
            get
            {
                if (_principalSearch == null)
                {
                    _principalSearch = new PrincipalContext(ContextType.Domain, null, @"AD\<REDACTED>", "<REDACTED>");
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
            if (data.secretkey != "<REDACTED>")
            {
                res = "ERROR: secretkey";
            }
            else
            {
                UserPrincipal userPrincipial = UserPrincipal.FindByIdentity(SearchContext, data.user.Username);
                if (userPrincipial != null)
                {
                    res = "ERROR: A user account already exist.";
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
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, null, postUser.OU, @"AD\<REDACTED>", "<REDACTED>");
            UserPrincipal newUser = new UserPrincipal(ctx, postUser.Username, postUser.Password, true);

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
            CreateHomeDir(postUser);
        }

        private void AddUserToGroup(ADUser postUser, UserPrincipal newUser)
        {
            GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(SearchContext, postUser.Unit);
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

        private void CreateHomeDir(ADUser postUser)
        {
            DirectoryInfo dirInfo = Directory.CreateDirectory(postUser.HomeDir);

            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.AddAccessRule(new FileSystemAccessRule(postUser.Username,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow)
            );

            dirInfo.SetAccessControl(dirSecurity);
        }
    }
}