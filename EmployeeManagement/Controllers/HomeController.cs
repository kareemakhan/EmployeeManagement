using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmployeeManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache memoryCache;
        private readonly AppDBContext context;

        public HomeController(IEmployeeRepository employeeRepository,
                              IWebHostEnvironment webHostEnvironment,
                              ILogger<HomeController> logger,
                              IMemoryCache memoryCache,
                              AppDBContext context)
        {
            _employeeRepository = employeeRepository;
            this.webHostEnvironment = webHostEnvironment;
            _logger = logger;
            this.memoryCache = memoryCache;
            this.context = context;
        }
      
        public IActionResult Index()
        {
            _logger.LogWarning("This is a warning from index");
            return View();
        }
        public IActionResult ListEmployee()
        {
            //var employees = _employeeRepository.GetAllEmployees();
            List<Employee> employees;
            if(!memoryCache.TryGetValue("Employees", out employees))   //tryget function is use to get data in the memory cache
            {
                var cacheEntryOption = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMilliseconds(100));    //sets the expiry of cache memory
                memoryCache.Set("Employees", context.Employees.ToList(), cacheEntryOption);   //initializing the memory cache
            }
            employees = memoryCache.Get("Employees") as List<Employee>;            //fetching stored data
            return View(employees);
            
        }
        public IActionResult Announcement()
        {
            return View();
        }

        public ViewResult Details(int? id)
        {
            Employee employee = _employeeRepository.GetEmployee(id.Value);
            if(employee == null)
            {
                Response.StatusCode = 404;
                return View("EmployeeNotFound", id.Value);
            }
            Employee model = employee;
            ViewBag.PageTitle = "Employee Details";
            return View(model);
        }

        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ViewResult CreateEmployee()
        {
            return View();
        }
        //Since RedirectToAction and View both implements IActionResult interface thus we can use it as return type
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateEmployee(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = ProcessUploadedFile(model);
                Employee newEmployee = new Employee
                {
                    Name = model.Name,
                    Department = model.Department,
                    Email = model.Email,
                    PhotoPath = uniqueFileName
                };
                _employeeRepository.Add(newEmployee);
                return RedirectToAction("details", new { id = newEmployee.Id });
            }
            return View();
        }

        private string ProcessUploadedFile(CreateEmployeeViewModel model)
        {
            string uniqueFileName = null;
            if (model.Photo != null)
            {
                //using WebrootPath to provide us the absolute physical path os the wwwroot folder and uploadFolder give us the complete path to images folder on our server
                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                //to have unique file names using guid

                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                /*using copyTo method provided by IFormFile of photo property to copy
                the uploaded file to images folder */
                FileStream fs = new FileStream(filePath, FileMode.Create);
                model.Photo.CopyTo(fs);
                /*When you use the CopyTo() method from the IFormFile interface, you instantiate a new filestream 
                 * object as the parameter, resulting in you not closing the filestream object. This will give SystemIO exceptions 
                 * later on if you want to delete those images from the server. A simple fix is just to instantiate the filestream object 
                 * before using the CopyTo() method, and thereafter calling the Close() */

                fs.Close();
            }

            return uniqueFileName;
        }

        [HttpGet]
        [Authorize]
        public ViewResult EditEmployee(int id)
        {
            Employee employee = _employeeRepository.GetEmployee(id);
            EditEmployeeViewModel editEmployeeViewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                ExistingPhotoPath = employee.PhotoPath
            };
            return View(editEmployeeViewModel);
        }
        [HttpPost]
        [Authorize]
        public IActionResult EditEmployee(EditEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                Employee employee = _employeeRepository.GetEmployee(model.Id);
                employee.Name = model.Name;
                employee.Email = model.Email;
                employee.Department = model.Department;
                if(model.Photo != null)
                {
                    if(model.ExistingPhotoPath != null)
                    {
                        string filePath = Path.Combine(webHostEnvironment.WebRootPath, "images", model.ExistingPhotoPath);
                        System.IO.File.Delete(filePath);
                    }
                    employee.PhotoPath = ProcessUploadedFile(model);
                }
                _employeeRepository.Update(employee);
                return RedirectToAction("listEmployee");
                
            }
            return View();
        }
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteEmployee(int id)
        {
            var employee = _employeeRepository.GetEmployee(id);
            if(employee != null)
            {
                _employeeRepository.Delete(id);
            }
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("There is an error");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
