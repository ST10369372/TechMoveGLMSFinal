using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechMoveGLMS.Data;

namespace TechMoveGLMS.API.Data
{
    /// <summary>
    /// Design-time factory — used ONLY by EF Core CLI tools (add-migration, update-database).
    /// This is why migrations work even when DI isn't running.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // ⬇ PASTE YOUR NEW CONNECTION STRING HERE
            optionsBuilder.UseSqlServer(
                "Data Source=localhost\\SQLEXPRESS02;Integrated Security=True;" +
                "Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;" +
                "Encrypt=True;TrustServerCertificate=True;Command Timeout=0");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}