﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using YNHM.Database;
using YNHM.Entities.Models;
using YNHM.RepositoryServices;
using YNHM.WebApp.Models;

namespace YNHM.WebApp.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        RoomieRepository pr = new RoomieRepository();
        ApplicationDbContext db = new ApplicationDbContext();


        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            //ViewBag.UserRole =

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        //GET: /Manage/ViewProfile
        public ActionResult ViewProfile()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            var roomie = pr.GetById(user.RoomieId);

            PersonDetailsVM vm = new PersonDetailsVM(roomie);

            return RedirectToAction($"PersonalProfile/{user.RoomieId}", "HomePage");
            //return View(vm);
        }
       
             
        //GET: /Manage/EditUserDetails
        public async Task<ActionResult> EditUserDetails()
        {
            //var userId = User.Identity.GetUserId();
            //var user = UserManager.FindById(userId);

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            var roomieId = user.RoomieId;
            var roomie = pr.GetById(roomieId);

            roomie.PhotoUrl = user.UserPhoto;

            PersonDetailsVM vm = new PersonDetailsVM(roomie);
            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePhoto(UploadPhotoVM uploadPhotoVM)
        {
            if (!ModelState.IsValid)
            {
                RedirectToAction("EditUserDetails");
            }

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null) { return Redirect("~/Shared/Error"); }

            if (uploadPhotoVM.ImageFile != null)
            {
                uploadPhotoVM.ImageFile.SaveAs(Server.MapPath("~/Content/images/user/" + user.Id + ".jpg"));

                uploadPhotoVM.Image = "~/Content/images/user/" + user.Id + ".jpg";
            }
            else
            {
                return Redirect("~/Shared/Error");
            }
            user.UserPhoto = "/Content/images/user/" + user.Id + ".jpg";

            var result = await UserManager.UpdateAsync(user);
            if (result.Succeeded) { return RedirectToAction("EditUserDetails"); }
            else { return Redirect("~/Shared/Error"); }

        }

        //POST: /Manage/EditUserDetails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUserDetails([Bind(Include = "Id,FirstName,LastName,Age,Email,Phone,Facebook,HasHouse, PhotoUrl")] Roomie roomie)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var user = UserManager.FindById(User.Identity.GetUserId());

            if (ModelState.IsValid)
            {
                db.Entry(roomie).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                roomie.PhotoUrl = user.UserPhoto;              

                return RedirectToAction("Index","HomePage");
            }

            PersonDetailsVM vm = new PersonDetailsVM(roomie);

            return View(vm);
        }

        public async Task<ActionResult> CreateRoomie()
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            CreateRoomieVM vm = new CreateRoomieVM()
            {
                PhotoUrl = user.UserPhoto
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadPhoto(UploadPhotoVM uploadPhotoVM)
        {
            if (!ModelState.IsValid)
            {
                RedirectToAction("CreateRoomie");
            }

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null) { return Redirect("~/Shared/Error"); }

            if (uploadPhotoVM.ImageFile != null)
            {
                uploadPhotoVM.ImageFile.SaveAs(Server.MapPath("~/Content/images/user/" + user.Id + ".jpg"));

                uploadPhotoVM.Image = "~/Content/images/user/" + user.Id + ".jpg";
            }
            else
            {
                return Redirect("~/Shared/Error");
            }
            user.UserPhoto = "/Content/images/user/" + user.Id + ".jpg";

            var result = await UserManager.UpdateAsync(user);
            if (result.Succeeded) { return RedirectToAction("CreateRoomie"); }
            else { return Redirect("~/Shared/Error"); }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoomie(CreateRoomieVM cr)
        {
            var userId = User.Identity.GetUserId();
            var user = UserManager.FindById(userId);

            if (ModelState.IsValid)
            {
                Roomie r = new Roomie()
                {
                    FirstName = cr.FirstName,
                    LastName = cr.LastName,
                    Age = cr.Age,
                    HasHouse = cr.HasHouse,
                    Email = user.Email,
                    Phone = cr.Phone,
                    Facebook = cr.Facebook,
                    PhotoUrl = user.UserPhoto
                };

                if (r.HasHouse)
                {
                    var house = CreateHouse(r);
                    r.House = house;
                }

                db.Roomies.Add(r);
                db.SaveChanges();

                user.RoomieId = r.Id;
                UserManager.Update(user);

                return RedirectToAction("Subscriptions","HomePage");
            }
            return View(cr);
        }

        private House CreateHouse(Roomie r)
        {
            Random rand = new Random();
            string[] streets =
{
                    "Ipirou", "Patission","Panepistimiou", "Frynis","Kerkyras",
                    "Pl. Koliatsou","Skiathou","Skopelou","Kykladon","Tritis Septemvriou",
                    "Agiou Nikolaou","Agiou Georgiou","Agias Annas","Agias Zonis","Agias Kiriakis",
                    "Ari Velouchioti","Angelou Sikelianou","Kosma Aitolou","Athinas","Iras",
                    "Afroditis","Dia","Autokratoron Angelon","Themistokleous","Perikleous"
                };

            string[] districts =
            {
                    "Center","Zografou","Exarcheia","Kolonaki","Kato Patissia",
                    "Kypseli","Kaisariani","Perama","Peiraias","Pasalimani",
                    "Nea Smyrni","Kallithea"
                };

            string address = $"{streets[rand.Next(0, streets.Length)]} {rand.Next(1, 350)}";
            string district = $"{districts[rand.Next(0, districts.Length)]}";
            int rooms = rand.Next(2, 6);
            int floor = rand.Next(0, 11);
            int rent = 0;
            int area = 0;

            if (rooms > 2)
            {
                area = rand.Next(70, 200);
            }
            else
            {
                area = rand.Next(45, 200);
            }

            if (area > 100)
            {
                rent = rand.Next(350, 601);
            }
            else
            {
                rent = rand.Next(200, 601);
            }

            House h = new House()
            {
                Address = address,
                District = district,
                Area = area,
                Bedrooms = rooms,
                Floor = floor,
                Rent = rent
            };

            h.Roomies.Add(r);
            db.SaveChanges();
            return h;
        }

        public ActionResult MatchDetails()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            var userRoomie = db.Roomies.Find(user.RoomieId);

            var pairs = db.RoomiesPair
                .Where(p => p.RoomieOneId == userRoomie.Id || p.RoomieTwoId == userRoomie.Id)
                .ToList();

            Dictionary<Roomie,int> pairedRoomies = new Dictionary<Roomie, int>();
            foreach (var pair in pairs)
            {
                int id = pair.RoomieOneId != userRoomie.Id ? id = pair.RoomieOneId : id = pair.RoomieTwoId;
                var roomie = db.Roomies.Find(id);
                pairedRoomies.Add(roomie,pair.MatchPercentage);
            }
            var house = userRoomie.House;

            pairedRoomies = pairedRoomies.OrderBy(r => r.Key.LastName).ThenBy(r => r.Key.FirstName).ToDictionary(x => x.Key, x => x.Value);
            MatchDetailsVM vm = new MatchDetailsVM()
            {
                PairedRoomies = pairedRoomies,
                House = house
            };
            return View(vm);
        }





        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        #endregion
    }
}