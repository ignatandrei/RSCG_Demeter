// See https://aka.ms/new-console-template for more information
using RSCG_Console;

Console.WriteLine("Hello, World!");
var dep=new Department();
dep.Employees.Add(new Employee());

foreach (var emp in dep.Employees)
{
    dep.EmployeeNames.Add(emp.Name);
}
var empAll= dep.Employees;
var empWithA = empAll.Where(it => it.Name.StartsWith("a"));
await Task.Run(dep.GetEmployees);
var asda=new List<int>(empAll.Select(it=>it.ID).Distinct().OrderBy(it=>it));
Console.WriteLine(asda.Count);