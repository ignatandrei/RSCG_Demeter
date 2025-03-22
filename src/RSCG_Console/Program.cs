// See https://aka.ms/new-console-template for more information
using RSCG_Console;

Console.WriteLine("Hello, World!");
var dep=new Department();
dep.Employees.Add(new Employee());

foreach(var emp in dep.Employees)
{
    dep.EmployeeNames.Add(emp.Name);
}
