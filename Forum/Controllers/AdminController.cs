using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using PagedList;
using Forum.Models;

namespace Forum.Controllers
{
    public class AdminController : Controller
    {
        #region Vars
        public UserManager<ApplicationUser> UserManager { get; set; }
        public ApplicationDbContext         context { get; set; }

        public static List<AdminUserViewModel> usrList = new List<AdminUserViewModel>();
        public static List<SelectListItem>     roleList = new List<SelectListItem>();

        public static string UsrName { get; set; }
        public static string UsrEmail { get; set; }
        public static string UsrRole { get; set; }
        public static string NameSrch { get; set; }
        public static string RankSrch { get; set; }
        #endregion  Vars

        public AdminController()
        {
            context     = new ApplicationDbContext();
            UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Moderator")]
        public async Task<ActionResult> Index(AdminUserViewModel model, string sortOrder, string searchString, string searchRank, int? page, ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
             message == ManageMessageId.UserDeleted ? "User account has successfully been deleted."
             : message == ManageMessageId.UserUpdated ? "User account has been updated."
             : "";

            ViewBag.ErrorMessage =
                message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.HighRankedUser ? "This user account cannot be deleted due to its rank."
                : "";

            await ShowUserDetails(model, sortOrder, searchString, searchRank, page);
            return View();
        }

        [Authorize(Roles = "Admin, Moderator")]
        [ChildActionOnly]
        public async Task<ActionResult> ShowUserDetails(AdminUserViewModel model, string sortOrder, string searchString, string searchRank, int? page)
        {
            usrList.Clear();
            roleList.Clear();
            ViewBag.CurrentSort      = sortOrder;
            ViewBag.RankSortParm     = string.IsNullOrEmpty(sortOrder) ? "rank_desc" : "";
            ViewBag.UsernameSortParm = sortOrder == "Username" ? "username_desc" : "Username";
            IList<ApplicationUser> users = context.Users.ToList();

            foreach (var user in users)
            {
                var roles      = await UserManager.GetRolesAsync(user.Id);
                model.UserName = user.UserName;
                foreach (var role in roles)
                {
                    model.RankName = role;
                    switch (role)
                    {
                        case "Admin":
                            model.RankId = "1";
                            break;
                        case "Moderator":
                            model.RankId = "2";
                            break;
                        case "Journalist":
                            model.RankId = "3";
                            break;
                        case "Member":
                            model.RankId = "4";
                            break;
                        case "Banned":
                            model.RankId = "5";
                            break;                  
                    }
                }
                model.UserId       = user.Id;
                model.UserFullName = user.FirstName + " " + user.LastName;
                usrList.Add(new AdminUserViewModel() { UserName = model.UserName, RankName = model.RankName, UserId = model.UserId, RankId = model.RankId, UserFullName = model.UserFullName });
                //model.RankName = null;
            }

            List<AdminRoleViewModel> rolesList = new List<AdminRoleViewModel>();
            rolesList.Add(new AdminRoleViewModel() { Role = "All", RoleId = "0", RoleValue = "" });
            rolesList.Add(new AdminRoleViewModel() { Role = "Admin", RoleId = "1", RoleValue = "Admin" });
            rolesList.Add(new AdminRoleViewModel() { Role = "Moderator", RoleId = "2", RoleValue = "Moderator" });
            rolesList.Add(new AdminRoleViewModel() { Role = "Journalist", RoleId = "3", RoleValue = "Journalist" });
            rolesList.Add(new AdminRoleViewModel() { Role = "Member", RoleId = "4", RoleValue = "Member" });
            rolesList.Add(new AdminRoleViewModel() { Role = "Banned", RoleId = "5", RoleValue = "Banned" });            
            rolesList = rolesList.OrderBy(x => x.RoleId).ToList();
            foreach (var role in rolesList)
            {
                roleList.Add(new SelectListItem { Text = role.Role, Value = role.RoleValue });
            }

            //search(filter) users in admin panel by role\name
            if (searchString != null)
            {
                usrList = usrList.Where(x => x.UserName.Contains(searchString)).ToList();
                NameSrch = searchString;
            }

            if (NameSrch != null)
            {

                usrList = usrList.Where(x => x.UserName.Contains(NameSrch)).ToList();
            }

            if (searchRank != null)
            {
                usrList = usrList.Where(x => x.RankName.Contains(searchRank)).ToList();
                RankSrch = searchRank;
            }

            if (RankSrch != null)
            {
                usrList = usrList.Where(x => x.RankName.Contains(RankSrch)).ToList();
            }

            switch (sortOrder)
            {
                case "rank_desc":
                    usrList = usrList.OrderByDescending(x => x.RankId).ToList();
                    break;
                case "Username":
                    usrList = usrList.OrderBy(x => x.UserName).ToList();
                    break;
                case "username_desc":
                    usrList = usrList.OrderByDescending(x => x.UserName).ToList();
                    break;
                default:
                    usrList = usrList.OrderBy(x => x.RankId).ToList();
                    break;
            }

            int pageSize = 15; //pagedlist
            int pageNumber = (page ?? 1);
            return PartialView("ShowUserDetails", usrList.ToPagedList(pageNumber, pageSize));
        }


        [HttpGet]
        [Authorize(Roles = "Admin, Moderator")]
        public ActionResult EditUser()
        {
            return View();
        }

        [Authorize(Roles = "Admin, Moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditUser(string id, AdminEditViewModel model)
        {
            try
            {                
                var user       = UserManager.FindById(id);
                model.Email    = user.Email;
                var roles      = await UserManager.GetRolesAsync(user.Id);
                model.UserName = user.UserName;

                foreach (var role in roles)
                {
                    model.RankName = role;
                }
                UsrName  = model.UserName;
                UsrEmail = model.Email;
                UsrRole  = model.RankName;
                return RedirectToAction("EditUser");
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Roles = "Admin, Moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveUser(string id, AdminEditViewModel model)
        {
            try
            {
                UsrRole = model.RankName;
                UsrName = model.UserName;

                var userid     = context.Users.Where(x => x.UserName == UsrName).Select(x => x.Id).FirstOrDefault();
                var user       = await UserManager.FindByIdAsync(userid);
                var userRoles  = await UserManager.GetRolesAsync(user.Id);
                string[] roles = new string[userRoles.Count];
                userRoles.CopyTo(roles, 0);
                await UserManager.RemoveFromRolesAsync(user.Id, roles);
                await UserManager.AddToRoleAsync(user.Id, UsrRole);

                return RedirectToAction("Index", "Admin", new { Message = ManageMessageId.UserUpdated });
            }
            catch
            {
                return RedirectToAction("Index", "Admin", new { Message = ManageMessageId.Error });
            }
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteUser()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteUser(string userid)
        {
            if (UsrRole == "Admin")
            {
                return RedirectToAction("Index", "Admin", new { Message = ManageMessageId.HighRankedUser });
            }
            userid = context.Users.Where(x => x.UserName == UsrName).Select(x => x.Id).FirstOrDefault();

            var user       = await UserManager.FindByIdAsync(userid);
            var userClaims = await UserManager.GetClaimsAsync(user.Id);
            var userRoles  = await UserManager.GetRolesAsync(user.Id);
            var userLogins = await UserManager.GetLoginsAsync(user.Id);
            foreach (var claim in userClaims)
            {
                await UserManager.RemoveClaimAsync(user.Id, claim);
            }
            string[] roles = new string[userRoles.Count];
            userRoles.CopyTo(roles, 0);
            await UserManager.RemoveFromRolesAsync(user.Id, roles);
            foreach (var log in userLogins)
            {
                await UserManager.RemoveLoginAsync(user.Id, new UserLoginInfo(log.LoginProvider, log.ProviderKey));
            }
            await UserManager.DeleteAsync(user);

            return RedirectToAction("Index", "Admin", new { Message = ManageMessageId.UserDeleted });
        }

        #region Helpers
        //for user editing
        public IEnumerable<SelectListItem> GetUserRoles(string usrrole) //edit roles for admin
        {
            var roles = context.Roles.OrderBy(x => x.Name).ToList();
            List<AdminRoleViewModel> rlList = new List<AdminRoleViewModel>();
            rlList.Add(new AdminRoleViewModel() { Role = "Admin", RoleId = "1" });
            rlList.Add(new AdminRoleViewModel() { Role = "Moderator", RoleId = "2" });
            rlList.Add(new AdminRoleViewModel() { Role = "Journalist", RoleId = "3" });
            rlList.Add(new AdminRoleViewModel() { Role = "Member", RoleId = "4" });
            rlList.Add(new AdminRoleViewModel() { Role = "Banned", RoleId = "5" });            
            rlList = rlList.OrderBy(x => x.RoleId).ToList();

            List<SelectListItem> roleNames = new List<SelectListItem>();
            foreach (var role in rlList)
            {
                roleNames.Add(new SelectListItem()
                {
                    Text = role.Role,
                    Value = role.Role
                });
            }
            var selectedRoleName = roleNames.FirstOrDefault(d => d.Value == usrrole);
            if (selectedRoleName != null)
                selectedRoleName.Selected = true;

            return roleNames;
        }

        public IEnumerable<SelectListItem> GetUserRolesForModer(string usrrole) //edit roles for moder
        {
            var roles = context.Roles.OrderBy(x => x.Name).ToList();
            List<AdminRoleViewModel> rlList = new List<AdminRoleViewModel>();            
            rlList.Add(new AdminRoleViewModel() { Role = "Member", RoleId = "4" });
            rlList.Add(new AdminRoleViewModel() { Role = "Banned", RoleId = "5" });            
            rlList = rlList.OrderBy(x => x.RoleId).ToList();

            List<SelectListItem> roleNames = new List<SelectListItem>();
            foreach (var role in rlList)
            {
                roleNames.Add(new SelectListItem()
                {
                    Text = role.Role,
                    Value = role.Role
                });
            }
            var selectedRoleName = roleNames.FirstOrDefault(d => d.Value == usrrole);
            if (selectedRoleName != null)
                selectedRoleName.Selected = true;

            return roleNames;
        }

        public enum ManageMessageId
        {
            HighRankedUser,
            Error,
            UserDeleted,
            UserUpdated
        }
        #endregion
    }
}