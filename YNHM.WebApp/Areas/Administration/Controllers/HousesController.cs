using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using YNHM.Database;
using YNHM.Entities.Models;
using YNHM.Entities.TestResources;
using YNHM.WebApp.Controllers;

namespace YNHM.WebApp.Areas.Administration.Controllers
{
    public class HousesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        HomePageController homePageController = new HomePageController();

        // GET: Administration/Houses
        public ActionResult Index(string searchText, string selectOption)
        {
            var houses = db.Houses.OrderByDescending(x => x.Roomies.Count).ThenBy(x => x.Address).ToList();

            #region Filtering

            if (!String.IsNullOrEmpty(searchText))
            {
                switch (selectOption)
                {
                    case "Address": houses = houses.Where(x => x.Address.ToUpper().Contains(searchText.ToUpper())).ToList(); break;
                    case "Distinct": houses = houses.Where(x => x.District.ToUpper().Contains(searchText.ToUpper())).ToList(); break;
                    case "Floor": houses = houses.Where(x => x.Floor >= Convert.ToInt32(searchText)).ToList(); break;
                    case "Area": houses = houses.Where(x => x.Area >= Convert.ToInt32(searchText)).ToList(); break;
                    case "Bedrooms": houses = houses.Where(x => x.Bedrooms >= Convert.ToInt32(searchText)).ToList(); break;
                    case "Rent": houses = houses.Where(x => x.Rent >= Convert.ToInt32(searchText)).ToList(); break;
                    case "Number of Roomies": houses = houses.Where(x => x.Roomies.Count >= Convert.ToInt32(searchText)).ToList(); break;
                }
            }

            #endregion
            return View(houses);
        }

        // GET: Administration/Houses/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            House house = db.Houses.Find(id);
            if (house == null)
            {
                return HttpNotFound();
            }

            var roomiesIds = house.Roomies.Select(x => x.Id).ToList();
            ViewBag.SelectedRoomiesIds = db.Roomies.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = String.Format($"{x.LastName}, {x.FirstName}"),
            });

            return View(house);
        }

        // GET: Administration/Houses/Create
        public ActionResult Create()
        {
            House house = new House();

            ViewBag.SelectedRoomiesIds = db.Roomies.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = String.Format($"{x.LastName}, {x.FirstName}"),

            });
            return View(house);
        }

        // POST: Administration/Houses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,PhotoUrl,Address,District,Floor,Area,Bedrooms,Rent")] House house, IEnumerable<int> SelectedRoomiesIds)
        {
            if (ModelState.IsValid)
            {
                db.Houses.Attach(house);
                db.Entry(house).Collection("Roomies").Load();
                house.Roomies.Clear();
                db.SaveChanges();

                if (!(SelectedRoomiesIds is null))
                {
                    foreach (var id in SelectedRoomiesIds)
                    {
                        Roomie roomie = db.Roomies.Find(id);
                        if (roomie != null)
                        {
                            house.Roomies.Add(roomie);
                            roomie.HasHouse = true;
                            roomie.HouseId = house.Id;
                            roomie.House = house;
                            db.Entry(roomie).State = EntityState.Modified;
                        }
                    }
                }
                db.Entry(house).State = EntityState.Added;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var roomiesIds = house.Roomies.Select(x => x.Id).ToList();
            ViewBag.SelectedRoomiesIds = db.Roomies.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = String.Format($"{x.LastName}, {x.FirstName}"),
            });
            return View(house);
        }

        // GET: Administration/Houses/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            House house = db.Houses.Find(id);
            if (house == null)
            {
                return HttpNotFound();
            }

            var roomiesIds = house.Roomies.Select(x => x.Id).ToList();
            ViewBag.SelectedRoomiesIds = db.Roomies.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = String.Format($"{x.LastName}, {x.FirstName}"),
                Selected = roomiesIds.Any(y => y == x.Id)
            });

            return View(house);
        }

        // POST: Administration/Houses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PhotoUrl,Address,District,Floor,Area,Bedrooms,Rent")] House house, IEnumerable<int> SelectedRoomiesIds)
        {
            if (ModelState.IsValid)
            {
                db.Houses.Attach(house);
                db.Entry(house).Collection("Roomies").Load();
                house.Roomies.Clear();
                db.SaveChanges();

                if (!(SelectedRoomiesIds is null))
                {
                    foreach (var id in SelectedRoomiesIds)
                    {
                        Roomie roomie = db.Roomies.Find(id);
                        if (roomie != null)
                        {
                            house.Roomies.Add(roomie);
                            roomie.HouseId = house.Id;
                            roomie.House = house;
                            roomie.HasHouse = true;
                            db.Entry(roomie).State = EntityState.Modified;
                        }
                    }
                    db.SaveChanges();
                }

                db.Entry(house).State = EntityState.Modified;
                db.SaveChanges();

                var houseRoomies = house.Roomies.ToList();
                if (houseRoomies.Count > 1)
                {
                    Comparison compare = new Comparison();
                    try
                    {
                        var tempRoomies = new List<Roomie>();
                        var tempRoomiesTwo = new List<Roomie>();

                        for (int i = 0; i < houseRoomies.Count; i++)
                        {
                            houseRoomies[i].IsMatched = true;
                            tempRoomies.Add(houseRoomies[i]);

                            for (int j = 0; j < houseRoomies.Count; j++)
                            {

                                if (!tempRoomies.Contains(houseRoomies[j]))
                                {
                                    tempRoomiesTwo.Add(houseRoomies[j]);


                                    var test1 = db.Tests.Find(houseRoomies[i].Id);
                                    var test2 = db.Tests.Find(houseRoomies[j].Id);

                                    RoomiesPair newPairFirstVersion = new RoomiesPair()
                                    {
                                        RoomieOneId = houseRoomies[i].Id,
                                        RoomieTwoId = houseRoomies[j].Id,
                                        MatchPercentage = compare.CalculateMatchPercentage(test1, test2)
                                    };

                                    RoomiesPair newPairSecondVersion = new RoomiesPair()
                                    {
                                        RoomieOneId = houseRoomies[j].Id,
                                        RoomieTwoId = houseRoomies[i].Id,
                                        MatchPercentage = compare.CalculateMatchPercentage(test1, test2)

                                    };
                                    var existingPairs = db.RoomiesPair.ToList();

                                    if (!(existingPairs.Contains(newPairFirstVersion) || existingPairs.Contains(newPairSecondVersion)))
                                    {
                                        GenerateMatch(houseRoomies[i], houseRoomies[j]);
                                        houseRoomies[i].IsMatched = true;
                                        houseRoomies[j].IsMatched = true;
                                    }

                                }

                            }

                        }
                    }
                    catch (Exception)
                    {

                        return RedirectToAction("Index");
                    }

                }
                return RedirectToAction("Index");
            }

            db.Houses.Attach(house);
            db.Entry(house).Collection("Roomies").Load();

            var roomiesIds = house.Roomies.Select(x => x.Id).ToList();
            ViewBag.SelectedRoomiesIds = db.Roomies.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = String.Format($"{x.LastName}, {x.FirstName}"),
                Selected = roomiesIds.Any(y => y == x.Id)
            });

            return View(house);
        }

        // GET: Administration/Houses/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            House house = db.Houses.Find(id);
            if (house == null)
            {
                return HttpNotFound();
            }
            return View(house);
        }

        // POST: Administration/Houses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            House house = db.Houses.Find(id);
            house.Roomies.Clear();

            db.Entry(house).State = EntityState.Deleted;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public void GenerateMatch(Roomie roomieOne, Roomie roomieTwo)
        {
            Comparison compare = new Comparison();

            RoomiesPair roomiesPair = new RoomiesPair()
            {
                RoomieOneId = roomieOne.Id,
                RoomieTwoId = roomieTwo.Id,
                MatchPercentage = compare.CalculateMatchPercentage(roomieOne.Test, roomieTwo.Test)
            };

            var roomieOneInDb = db.Roomies.Find(roomiesPair.RoomieOneId);
            roomieOneInDb.IsMatched = true;
            db.Entry(roomieOneInDb).State = EntityState.Modified;

            var roomieTwoInDb = db.Roomies.Find(roomiesPair.RoomieTwoId);
            roomieTwoInDb.IsMatched = true;
            db.Entry(roomieTwoInDb).State = EntityState.Modified;

            db.Entry(roomiesPair).State = EntityState.Added;
            db.SaveChanges();
        }
    }
}
