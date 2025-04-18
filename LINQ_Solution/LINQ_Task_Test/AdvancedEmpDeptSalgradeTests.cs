using LINQ_Task.Reposiotry;

namespace LINQ_Task_Test;

public class AdvancedEmpDeptSalgradeTests
{
    // 11. MAX salary
    // SQL: SELECT MAX(Sal) FROM Emp;
    [Fact]
    public void ShouldReturnMaxSalary()
    {
        var emps = Database.GetEmps();

        decimal? maxSalary = emps.Max(emp => emp.Sal); 

        Assert.Equal(5000, maxSalary);
    }

    // 12. MIN salary in department 30
    // SQL: SELECT MIN(Sal) FROM Emp WHERE DeptNo = 30;
    [Fact]
    public void ShouldReturnMinSalaryInDept30()
    {
        var emps = Database.GetEmps();

        decimal? minSalary = emps
            .Where(emp => emp.DeptNo == 30)
            .Min(emp => emp.Sal);

        Assert.Equal(1250, minSalary);
    }

    // 13. Take first 2 employees ordered by hire date
    // SQL: SELECT * FROM Emp ORDER BY HireDate ASC FETCH FIRST 2 ROWS ONLY;
    [Fact]
    public void ShouldReturnFirstTwoHiredEmployees()
    {
        var emps = Database.GetEmps();

        var firstTwo = emps
            .Take(2)
            .OrderBy(emp => emp.HireDate)
            .ToList(); 
        
        Assert.Equal(2, firstTwo.Count);
        Assert.True(firstTwo[0].HireDate <= firstTwo[1].HireDate);
    }

    // 14. DISTINCT job titles
    // SQL: SELECT DISTINCT Job FROM Emp;
    [Fact]
    public void ShouldReturnDistinctJobTitles()
    {
        var emps = Database.GetEmps();

        var jobs = emps
            .DistinctBy(emp => emp.Job)
            .Select(emp => emp.Job)
            .ToList(); 
        
        Assert.Equal(3, jobs.Count);
        Assert.Contains("PRESIDENT", jobs);
        Assert.Contains("SALESMAN", jobs);
        Assert.Contains("CLERK", jobs);
    }

    // 15. Employees with managers (NOT NULL Mgr)
    // SQL: SELECT * FROM Emp WHERE Mgr IS NOT NULL;
    [Fact]
    public void ShouldReturnEmployeesWithManagers()
    {
        var emps = Database.GetEmps();

        var result = emps.Where(emp => emp.Mgr != null).ToList(); 
        
        Assert.Equal(4, result.Count);
        Assert.All(result, e => Assert.NotNull(e.Mgr));
        Assert.DoesNotContain(result, r => r.EName == "KING" || r.Job == "PRESIDENT");
    }

    // 16. All employees earn more than 500
    // SQL: SELECT * FROM Emp WHERE Sal > 500; (simulate all check)
    [Fact]
    public void AllEmployeesShouldEarnMoreThan500()
    {
        var emps = Database.GetEmps();

        var result = emps.All(emp => emp.Sal > 500); 
        
        Assert.True(result);
    }

    // 17. Any employee with commission over 400
    // SQL: SELECT * FROM Emp WHERE Comm > 400;
    [Fact]
    public void ShouldFindAnyWithCommissionOver400()
    {
        var emps = Database.GetEmps();

        var result = emps.Any(emp => emp.Comm > 400); 
        
        Assert.True(result);
    }

    // 18. Self-join to get employee-manager pairs
    // SQL: SELECT E1.EName AS Emp, E2.EName AS Manager FROM Emp E1 JOIN Emp E2 ON E1.Mgr = E2.EmpNo;
    [Fact]
    public void ShouldReturnEmployeeManagerPairs()
    {
        var emps = Database.GetEmps();

        var result = (
            from e in emps
            from m in emps
            where e.Mgr == m.EmpNo
            select new {Employee = e.EName, Manager = m.EName}).ToList();
        
        Assert.Contains(result, r => r.Employee == "SMITH" && r.Manager == "FORD");
        Assert.Contains(result, r => r.Employee == "FORD" && r.Manager == "KING");
        Assert.DoesNotContain(result, r => r.Employee == "KING");
    }

    // 19. Let clause usage (sal + comm)
    // SQL: SELECT EName, (Sal + COALESCE(Comm, 0)) AS TotalIncome FROM Emp;
    [Fact]
    public void ShouldReturnTotalIncomeIncludingCommission()
    {
        var emps = Database.GetEmps();

        var result = (from emp in emps
            let totalIncome = emp.Sal + (emp.Comm ?? 0)
            select new
            {
                emp.EName,
                Total = totalIncome
            }).ToList();
        
        Assert.Equal(emps.Count, result.Count);
        Assert.Contains(result, r => r.EName == "SMITH" && r.Total == 800);
        Assert.Contains(result, r => r.EName == "ALLEN" && r.Total == 1900);
        Assert.Contains(result, r => r.EName == "WARD" && r.Total == 1750);
        Assert.Contains(result, r => r.EName == "FORD" && r.Total == 5000);
        Assert.Contains(result, r => r.EName == "KING" && r.Total == 5000);
    }

    // 20. Join all three: Emp → Dept → Salgrade
    // SQL: SELECT E.EName, D.DName, S.Grade FROM Emp E JOIN Dept D ON E.DeptNo = D.DeptNo JOIN Salgrade S ON E.Sal BETWEEN S.Losal AND S.Hisal;
    [Fact]
    public void ShouldJoinEmpDeptSalgrade()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();
        var grades = Database.GetSalgrades();

        var result = 
                from e in emps
                join d in depts on e.DeptNo equals d.DeptNo
                from s in grades
                where s.Losal <= e.Sal && e.Sal <= s.Hisal
                select new {e.EName, d.DName, s.Grade}; 
        
        Assert.Contains(result, r => r.EName == "SMITH" && r.DName == "RESEARCH" && r.Grade == 1);
        Assert.Contains(result, r => r.EName == "ALLEN" && r.DName == "SALES" && r.Grade == 3);
        Assert.Contains(result, r => r.EName == "WARD" && r.DName == "SALES" && r.Grade == 2);
        Assert.Contains(result, r => r.EName == "KING" && r.DName == "ACCOUNTING" && r.Grade == 5);
        Assert.Contains(result, r => r.EName == "FORD" && r.DName == "ACCOUNTING" && r.Grade == 5);
    }
}