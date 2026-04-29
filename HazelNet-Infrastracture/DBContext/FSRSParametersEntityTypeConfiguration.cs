using HazelNet_Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HazelNet_Infrastracture.DBContext;

public class FSRSParametersEntityTypeConfiguration : IEntityTypeConfiguration<FSRSParameters>
{
    public void Configure(EntityTypeBuilder<FSRSParameters> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        
        builder.HasOne(p => p.User)
            .WithOne(u => u.FSRSParameters)
            .HasForeignKey<FSRSParameters>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}