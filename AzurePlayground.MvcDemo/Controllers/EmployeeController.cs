using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AzurePlayground.Model;
using StackExchange.Redis;
using System.Configuration;
using System.Web.UI;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

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
        [OutputCache(CacheProfile="RedisShort", VaryByParam = "none")]
        public async Task<ActionResult> Index()
        {
            var employees = northwindContext.Employees.Include(e => e.ReportsToEmployee);
            return View(await employees.ToListAsync());
        }

        // GET: Employee/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                string cacheConnection = ConfigurationManager.AppSettings["CacheConnection"].ToString();
                return ConnectionMultiplexer.Connect(cacheConnection);
            });

            // Connection refers to a property that returns a ConnectionMultiplexer
            // as shown in the previous example.
            IDatabase cache = lazyConnection.Value.GetDatabase();

            var cachedEmployee = cache.StringGet(id.Value.ToString());
            Employee employee = null;

            if (string.IsNullOrEmpty(cachedEmployee))
            {
                employee = await northwindContext.Employees.FindAsync(id);

                if (employee == null)
                {
                    return HttpNotFound();
                }

                cache.StringSetAsync(
                    employee.EmployeeID.ToString(), 
                    JsonConvert.SerializeObject(employee, 
                        new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                
                employee.Region = "Non-cached";
            }
            else
            {
                employee = JsonConvert.DeserializeObject<Employee>(cachedEmployee, 
                    new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

                employee.Region = "Cached";
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

        // POST: Employee/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        public async Task<ActionResult> QueueEmail(int id)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["AzureHotStorageConnection"].ToString());

            var queueClient = storageAccount.CreateCloudQueueClient();

            var emailQueue = queueClient.GetQueueReference("employee-emails");

            await emailQueue.CreateIfNotExistsAsync();

            var message = new CloudQueueMessage(id.ToString());

            await emailQueue.AddMessageAsync(message);

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
