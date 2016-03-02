#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Diagnostics.CodeAnalysis;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data.Mapping
{
	[ExcludeFromCodeCoverage]
	public class PageMap : EntityTypeConfiguration<Page>
	{
		#region Constructors

		public PageMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Pages");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.ApprovalStatus).IsRequired();
			Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.IsDeleted).IsRequired();
			Property(x => x.IsHomePage).IsRequired();
			Property(x => x.IsPublished).IsRequired();
			Property(x => x.ModifiedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Tags).IsRequired().HasMaxLength(450);
			Property(x => x.Text).IsRequired();
			Property(x => x.Title).IsRequired().HasMaxLength(450);

			// Relationships
			HasRequired(x => x.CreatedBy)
				.WithMany(x => x.CreatedPages)
				.HasForeignKey(x => x.CreatedById)
				.WillCascadeOnDelete(false);
			HasOptional(x => x.EditingBy)
				.WithMany(x => x.PagesBeingEdited)
				.HasForeignKey(x => x.EditingById)
				.WillCascadeOnDelete(false);
			HasOptional(x => x.Parent)
				.WithMany(x => x.Versions)
				.HasForeignKey(x => x.ParentId)
				.WillCascadeOnDelete(false);
		}

		#endregion
	}
}