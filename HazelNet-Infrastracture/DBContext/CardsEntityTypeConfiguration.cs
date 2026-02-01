using HazelNet_Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HazelNet_Infrastracture.DBContext;

public class CardsEntityTypeConfiguration :IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.FrontOfCard)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.HasOne(c => c.Deck)
            .WithMany(d => d.Cards)
            .HasForeignKey(d => d.DeckId);
    }
}