using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
