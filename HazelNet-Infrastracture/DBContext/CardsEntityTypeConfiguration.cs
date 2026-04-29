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
        builder.Property(c => c.FrontOfCard);
        
        builder.HasOne(c => c.Deck)
            .WithMany(d => d.Cards)
            .HasForeignKey(d => d.DeckId);
        builder.HasOne(c => c.ReviewHistory)
            .WithOne(rh => rh.Card)
            .HasForeignKey<ReviewHistory>(rh => rh.CardId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}