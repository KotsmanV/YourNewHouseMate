﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using YNHM.Database;
using YNHM.Database.Models;
using YNHM.RepositoryServices;

namespace YNHM.WebApp.Areas.Administration.Controllers
{
    public class PeopleController : Controller
    {
        readonly ApplicationDbContext db = new ApplicationDbContext();
        readonly PersonRepository pr = new PersonRepository();

        // GET: People
        public ActionResult Index()
        {
            var people = pr.GetAll();

            return View("~/Views/Administrator/People/Index.cshtml",people);
        }

        // GET: People/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Person person = pr.GetById(id);
            if (person == null)
            {
                return HttpNotFound();
            }
            return View("~/Views/Administrator/People/Details.cshtml",person);
        }

        // GET: People/Create
        public ActionResult Create()
        {
            return View("~/Views/Administrator/People/Create.cshtml");
        }

        // POST: People/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PersonId,FirstName,LastName,Age,MatchPercent,Phone,Email,Facebook,Description,PhotoUrl")] Person person)
        {
            if (ModelState.IsValid)
            {
                pr.Create(person, null);                
                return RedirectToAction("Index");
            }

            return View("~/Views/Administrator/People/Create.cshtml",person);
        }

        // GET: People/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Person person = pr.GetById(id);
            if (person == null)
            {
                return HttpNotFound();
            }
            return View("~/Views/Administrator/People/Edit.cshtml",person);
        }

        // POST: People/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PersonId,FirstName,LastName,Age,MatchPercent,Phone,Email,Facebook,Description,PhotoUrl")] Person person)
        {
            if (ModelState.IsValid)
            {
                pr.Edit(person, null);
                return RedirectToAction("Index");
            }
            return View("~/Views/Administrator/People/Edit.cshtml",person);
        }

        // GET: People/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Person person = pr.GetById(id);
            if (person == null)
            {
                return HttpNotFound();
            }
            return View("~/Views/Administrator/People/Delete.cshtml",person);
        }

        // POST: People/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            pr.Delete(id);
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
    }
}