using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

/// <summary>Tabla intermedia grupo–usuario con la responsabilidad como parte de la clave.</summary>
public class GroupAssignmentConfiguration : IEntityTypeConfiguration<GroupAssignment>
{
    public void Configure(EntityTypeBuilder<GroupAssignment> builder)
    {
        builder.HasKey(assignment => new { assignment.GroupId, assignment.UserId, assignment.Responsibility });
        builder.HasIndex(assignment => assignment.UserId);

        builder.HasOne(assignment => assignment.Group)
            .WithMany(group => group.Assignments)
            .HasForeignKey(assignment => assignment.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
