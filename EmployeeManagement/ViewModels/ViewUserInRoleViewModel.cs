using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.ViewModels
{
    public class ViewUserInRoleViewModel
    {
        public ViewUserInRoleViewModel()
        {
            Users = new List<string>();
        }
        public string Id { get; set; }
        public List<string> Users { get; set; }
    }
}
