using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AzurePlayground.Model;

namespace AzurePlayground.MvcDemo.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly NorthwindContext northwindContext;
        public EmployeeController(NorthwindContext northwindContext)
        {
            this.northwindContext = northwindContext;
        }

        // GET: Employee
        public async Task<ActionResult> Index()
        {
            var employees = northwindContext.Employees.Include(e => e.Employee1);
            return View(await employees.ToListAsync());
        }

        // GET: Employee/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = await northwindContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // GET: Employee/Create
        public ActionResult Create()
        {
            ViewBag.ReportsTo = new SelectList(northwindContext.Employees, "EmployeeID", "LastName");
            return View();
        }

        // POST: Employee/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Photo,Notes,ReportsTo,PhotoPath")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                northwindContext.Employees.Add(employee);
                await northwindContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ReportsTo = new SelectList(northwindContext.Employees, "EmployeeID", "LastName", employee.ReportsTo);
            return View(employee);
        }

        // GET: Employee/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = await northwindContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            ViewBag.ReportsTo = new SelectList(northwindContext.Employees, "EmployeeID", "LastName", employee.ReportsTo);
            return View(employee);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Photo,Notes,ReportsTo,PhotoPath")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                northwindContext.Entry(employee).State = EntityState.Modified;
                await northwindContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ReportsTo = new SelectList(northwindContext.Employees, "EmployeeID", "LastName", employee.ReportsTo);
            return View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = await northwindContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Employee employee = await northwindContext.Employees.FindAsync(id);
            northwindContext.Employees.Remove(employee);
            await northwindContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                northwindContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
