using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace RSCG_Demeter.Tests
{
    /// <summary>
    /// Provides test source code for testing the Demeter analyzer
    /// </summary>
    internal static class TestSourceBuilder
    {
        /// <summary>
        /// Generates model classes for Department and Employee
        /// </summary>
        public static string GenerateDepartmentAndEmployeeModel()
        {
            return @"
namespace DemeterTest.Models
{
    public class Department
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<Employee> Employees { get; set; }

        public Department()
        {
            Employees = new List<Employee>();
        }

        public List<Employee> GetEmployees()
        {
            return Employees;
        }
    }

    public class Employee
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public Department Department { get; set; }
    }

    public class EmployeeBuilder
    {
        private Employee _employee;

        public EmployeeBuilder()
        {
            _employee = new Employee();
        }

        public EmployeeBuilder SetName(string name)
        {
            _employee.Name = name;
            return this;
        }

        public EmployeeBuilder SetId(int id)
        {
            _employee.ID = id;
            return this;
        }

        public Employee Build()
        {
            return _employee;
        }
    }
}";
        }

        /// <summary>
        /// Generates code that contains Law of Demeter violations
        /// </summary>
        public static string GenerateDemeterViolations()
        {
            return @"
using System;
using System.Linq;
using System.Collections.Generic;
using DemeterTest.Models;

namespace DemeterTest
{
    public class DemeterViolationsDemo
    {
        public void DemeterViolations()
        {
            // Setting up test data
            var dep = new Department { ID = 1, Name = ""Engineering"" };
            dep.Employees.Add(new Employee { ID = 1, Name = ""Alice"", Department = dep });
            dep.Employees.Add(new Employee { ID = 2, Name = ""Bob"", Department = dep });

            // Violation 1: Accessing through a chain of objects
            var empAll = dep.GetEmployees();
            
            // This is a violation of Law of Demeter (multiple dots)
            var filteredIds = new List<int>(empAll.Select(it => it.ID).Distinct().OrderBy(it => it));

            // Violation 2: Using chained static method calls
            var data = new List<string> { ""System"" };
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(it => data.Any(a => !(it.FullName?.StartsWith(a) ?? false)))
                .Distinct();

            // Not a violation: Builder pattern with fluent interface
            var employee = new EmployeeBuilder()
                .SetName(""Ignat"")
                .SetId(1)
                .SetName(""Andrei"")
                .Build();
        }
    }
}";
        }

        /// <summary>
        /// Creates a Roslyn compilation from the provided source code
        /// </summary>
        public static Compilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "DemeterTest",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
