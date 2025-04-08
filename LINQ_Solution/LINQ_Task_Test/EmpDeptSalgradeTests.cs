using LINQ_Task.Reposiotry;
using Tutorial3.Models;

namespace LINQ_Task_Test;

public class EmpDeptSalgradeTests
{
    // 1. Simple WHERE filter
    // SQL: SELECT * FROM Emp WHERE Job = 'SALESMAN';
    [Fact]
    public void ShouldReturnAllSalesmen()
    {
        List<Emp> result = Database
            .GetEmps()
            .Where(emp => emp.Job.Equals("SALESMAN"))
            .ToList();
        ;

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("SALESMAN", e.Job));
    }


    // 2. WHERE + OrderBy
    // SQL: SELECT * FROM Emp WHERE DeptNo = 30 ORDER BY Sal DESC;
    [Fact]
    public void ShouldReturnDept30EmpsOrderedBySalaryDesc()
    {
        List<Emp> result = Database.GetEmps()
            .Where(emp => emp.DeptNo == 30)
            .OrderByDescending(emp => emp.Sal)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Sal >= result[1].Sal);
    }


    // 3. Subquery using LINQ (IN clause)
    // SQL: SELECT * FROM Emp WHERE DeptNo IN (SELECT DeptNo FROM Dept WHERE Loc = 'CHICAGO');
    [Fact]
    public void ShouldReturnEmployeesFromChicago()
    {
        List<Emp> emps = Database.GetEmps();
        List<Dept> depts = Database.GetDepts();

        List<Emp> result = (
            from e in emps
            where (
                from d in depts
                where d.Loc.Equals("CHICAGO")
                select d.DeptNo
            ).Contains(e.DeptNo)
            select e
        ).ToList();

        Assert.All(result, e => Assert.Equal(30, e.DeptNo));
    }


    // 4. SELECT projection
    // SQL: SELECT EName, Sal FROM Emp;
    [Fact]
    public void ShouldSelectNamesAndSalaries()
    {
        var result = Database
            .GetEmps()
            .Select(emp => new { emp.EName, emp.Sal })
            .ToList();

        Assert.All(result, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.EName));
            Assert.True(r.Sal > 0);
        });
        
        Assert.Equal(5, result.Count);
    }


    // 5. JOIN Emp to Dept
    // SQL: SELECT E.EName, D.DName FROM Emp E JOIN Dept D ON E.DeptNo = D.DeptNo;
    [Fact]
    public void ShouldJoinEmployeesWithDepartments()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();

        var result = emps
            .Join(
                depts,
                emp => emp.DeptNo,
                dept => dept.DeptNo,
                (e, d) => new { e.EName, d.DName }
            ).ToList();

        Assert.Contains(result, r => r.DName == "SALES" && r.EName == "ALLEN");
        Assert.Contains(result, r => r.DName == "SALES" && r.EName == "WARD");
    }


    // 6. Group by DeptNo
    // SQL: SELECT DeptNo, COUNT(*) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCountEmployeesPerDepartment()
    {
        var result = Database
            .GetEmps()
            .GroupBy(emp => emp.DeptNo)
            .Select(deptGroup => new
            {
                DeptNo = deptGroup.Key,
                Count = deptGroup.Count()
            })
            .ToList();

        Assert.Contains(result, deptGroup => deptGroup.DeptNo == 10 && deptGroup.Count == 2);
        Assert.Contains(result, deptGroup => deptGroup.DeptNo == 20 && deptGroup.Count == 1);
        Assert.Contains(result, deptGroup => deptGroup.DeptNo == 30 && deptGroup.Count == 2);
    }


    // 7. SelectMany (simulate flattening)
    // SQL: SELECT EName, Comm FROM Emp WHERE Comm IS NOT NULL;
    [Fact]
    public void ShouldReturnEmployeesWithCommission()
    {
        var result = Database
            .GetDepts()
            .SelectMany(
                dept => Database
                    .GetEmps()
                    .Where(emp => emp.DeptNo == dept.DeptNo)
                    .Select(emp => new { emp.EName, emp.Comm })
            )
            .Where(emp => emp.Comm != null)
            .ToList();


        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotNull(r.Comm));
        Assert.Contains(result, r => r.EName == "ALLEN");
        Assert.Contains(result, r => r.EName == "WARD");
    }


    // 8. Join with Salgrade
    // SQL: SELECT E.EName, S.Grade FROM Emp E JOIN Salgrade S ON E.Sal BETWEEN S.Losal AND S.Hisal;
    [Fact]
    public void ShouldMatchEmployeeToSalaryGrade()
    {
        List<Salgrade> salgrades = Database.GetSalgrades();
        List<Emp> emps = Database.GetEmps();

        var result = (
            from e in emps
            from s in salgrades
            where s.Losal <= e.Sal && e.Sal <= s.Hisal
            select new { e.EName, s.Grade }
        ).ToList();

        Assert.Contains(result, r => r.EName == "SMITH" && r.Grade == 1);
        Assert.Contains(result, r => r.EName == "ALLEN" && r.Grade == 3);
        Assert.Contains(result, r => r.EName == "WARD" && r.Grade == 2);
        Assert.Contains(result, r => r.EName == "KING" && r.Grade == 5);
        Assert.Contains(result, r => r.EName == "FORD" && r.Grade == 5);
    }


    // 9. Aggregation (AVG)
    // SQL: SELECT DeptNo, AVG(Sal) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCalculateAverageSalaryPerDept()
    {
        List<Dept> depts = Database.GetDepts();
        List<Emp> emps = Database.GetEmps();

        var result = emps
            .Join(
                depts,
                emp => emp.DeptNo,
                dept => dept.DeptNo,
                (e, d) => new { e.DeptNo, e.Sal }
            )
            .GroupBy(emp => emp.DeptNo)
            .Select(empGroup => new
            {
                DeptNo = empGroup.Key,
                AvgSal = empGroup.Average(emp => emp.Sal)
            })
            .ToList();

        Assert.Contains(result, r => r.DeptNo == 10 && r.AvgSal == 5000);
        Assert.Contains(result, r => r.DeptNo == 20 && r.AvgSal == 800);
        Assert.Contains(result, r => r.DeptNo == 30 && r.AvgSal > 1000);
        Assert.DoesNotContain(result, r => r.DeptNo == 40);
    }


    // 10. Complex filter with subquery and join
    // SQL: SELECT E.EName FROM Emp E WHERE E.Sal > (SELECT AVG(Sal) FROM Emp WHERE DeptNo = E.DeptNo);
    [Fact]
    public void ShouldReturnEmployeesEarningMoreThanDeptAverage()
    {
        var emps = Database.GetEmps();

        var result = emps
            .Where(e =>
                e.Sal > (
                    emps
                        .Where(inner => inner.DeptNo == e.DeptNo)
                        .Average(inner => inner.Sal)
                )
            )
            .Select(e => e.EName)
            .ToList();


        Assert.Contains("ALLEN", result);
        Assert.DoesNotContain(result, r => r == "KING");
        Assert.DoesNotContain(result, r => r == "FORD");
    }
}