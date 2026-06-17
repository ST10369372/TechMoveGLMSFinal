using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechMoveGLMS.Data;

namespace TechMoveGLMS.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
                "Data Source=localhost\\SQLEXPRESS02;Initial Catalog=TechMoveGLMS_DB;" +
                "Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}