using Microsoft.EntityFrameworkCore;

namespace TestProject.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options);
