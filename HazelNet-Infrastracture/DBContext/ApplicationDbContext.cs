using HazelNet_Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HazelNet_Infrastracture.DBContext;

public class ApplicationDbContext : DbContext
{
    public DbSet<Deck> Decks { get; set; }
    public DbSet<Card> Cards { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        :base(options) {}
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new CardsEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new DeckEntityTypeConfiguration());
    }
}

