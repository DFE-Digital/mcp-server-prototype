using Dfe.Mcp.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace Dfe.Mcp.Server.Data;

public class AcademiesDbContext(DbContextOptions<AcademiesDbContext> options, IConfiguration configuration) : DbContext(options)
{
    const string DEFAULT_SCHEMA = "mis_mstr";
    public virtual DbSet<MisEstablishment> Establishments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = configuration.GetConnectionString("AcademiesConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MisEstablishment>(ConfigureEstablishment);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureEstablishment(EntityTypeBuilder<MisEstablishment> establishmentConfiguration)
    {
        establishmentConfiguration.HasKey(e => e.Urn);
        establishmentConfiguration.ToTable("Establishments", DEFAULT_SCHEMA);
        establishmentConfiguration.Property(e => e.AdmissionsPolicy).HasColumnName("admissions_policy");
        establishmentConfiguration.Property(e => e.BehaviourAndAttitudes).HasColumnName("behaviour_and_attitudes");
        establishmentConfiguration.Property(e => e.CategoryOfConcern).HasColumnName("category_of_concern");
        establishmentConfiguration.Property(e => e.DateOfLatestSection8Inspection)
            .HasColumnName("date_of_latest_section_8_inspection");
        establishmentConfiguration.Property(e => e.DesignatedReligiousCharacter)
            .HasColumnName("designated_religious_character");
        establishmentConfiguration.Property(e => e.DidTheLatestSection8InspectionConvertToAFullInspection)
            .HasColumnName("did_the_latest_section_8_inspection_convert_to_a_full_inspection");
        establishmentConfiguration.Property(e => e.DoesTheLatestFullInspectionRelateToTheUrnOfTheCurrentSchool)
            .HasColumnName("does_the_latest_full_inspection_relate_to_the_urn_of_the_current_school");
        establishmentConfiguration.Property(e => e.DoesThePreviousFullInspectionRelateToTheUrnOfTheCurrentSchool)
            .HasColumnName("does_the_previous_full_inspection_relate_to_the_urn_of_the_current_school");
        establishmentConfiguration.Property(e => e.DoesTheSection8InspectionRelateToTheUrnOfTheCurrentSchool)
            .HasColumnName("does_the_section_8_inspection_relate_to_the_urn_of_the_current_school");
        establishmentConfiguration.Property(e => e.EarlyYearsProvisionWhereApplicable)
            .HasColumnName("early_years_provision_where_applicable");
        establishmentConfiguration.Property(e => e.EffectivenessOfLeadershipAndManagement)
            .HasColumnName("effectiveness_of_leadership_and_management");
        establishmentConfiguration.Property(e => e.EventTypeGrouping).HasColumnName("event_type_grouping");
        establishmentConfiguration.Property(e => e.FaithGrouping).HasColumnName("faith_grouping");
        establishmentConfiguration.Property(e => e.InspectionNumberOfLatestFullInspection)
            .HasColumnName("inspection_number_of_latest_full_inspection");
        establishmentConfiguration.Property(e => e.InspectionStartDate).HasColumnName("inspection_start_date");
        establishmentConfiguration.Property(e => e.InspectionType).HasColumnName("inspection_type");
        establishmentConfiguration.Property(e => e.InspectionTypeGrouping).HasColumnName("inspection_type_grouping");
        establishmentConfiguration.Property(e => e.Laestab).HasColumnName("laestab");
        establishmentConfiguration.Property(e => e.LaestabAtTimeOfLatestFullInspection)
            .HasColumnName("laestab_at_time_of_latest_full_inspection");
        establishmentConfiguration.Property(e => e.LaestabAtTimeOfPreviousFullInspection)
            .HasColumnName("laestab_at_time_of_previous_full_inspection");
        establishmentConfiguration.Property(e => e.LaestabAtTimeOfTheSection8Inspection)
            .HasColumnName("laestab_at_time_of_the_section_8_inspection");
        establishmentConfiguration.Property(e => e.LatestSection8InspectionNumberSinceLastFullInspection)
            .HasColumnName("latest_section_8_inspection_number_since_last_full_inspection");
        establishmentConfiguration.Property(e => e.LocalAuthority).HasColumnName("local_authority");
        establishmentConfiguration.Property(e => e.NumberOfOtherSection8InspectionsSinceLastFullInspection)
            .HasColumnName("number_of_other_section_8_inspections_since_last_full_inspection");
        establishmentConfiguration.Property(e => e.NumberOfSection8InspectionsSinceTheLastFullInspection)
            .HasColumnName("number_of_section_8_inspections_since_the_last_full_inspection");
        establishmentConfiguration.Property(e => e.OfstedPhase).HasColumnName("ofsted_phase");
        establishmentConfiguration.Property(e => e.OfstedRegion).HasColumnName("ofsted_region");
        establishmentConfiguration.Property(e => e.OverallEffectiveness).HasColumnName("overall_effectiveness");
        establishmentConfiguration.Property(e => e.ParliamentaryConstituency)
            .HasColumnName("parliamentary_constituency");
        establishmentConfiguration.Property(e => e.PersonalDevelopment).HasColumnName("personal_development");
        establishmentConfiguration.Property(e => e.PreviousBehaviourAndAttitudes)
            .HasColumnName("previous_behaviour_and_attitudes");
        establishmentConfiguration.Property(e => e.PreviousCategoryOfConcern)
            .HasColumnName("previous_category_of_concern");
        establishmentConfiguration.Property(e => e.PreviousEarlyYearsProvisionWhereApplicable)
            .HasColumnName("previous_early_years_provision_where_applicable");
        establishmentConfiguration.Property(e => e.PreviousEffectivenessOfLeadershipAndManagement)
            .HasColumnName("previous_effectiveness_of_leadership_and_management");
        establishmentConfiguration.Property(e => e.PreviousFullInspectionNumber)
            .HasColumnName("previous_full_inspection_number");
        establishmentConfiguration.Property(e => e.PreviousFullInspectionOverallEffectiveness)
            .HasColumnName("previous_full_inspection_overall_effectiveness");
        establishmentConfiguration.Property(e => e.PreviousInspectionStartDate)
            .HasColumnName("previous_inspection_start_date");
        establishmentConfiguration.Property(e => e.PreviousPersonalDevelopment)
            .HasColumnName("previous_personal_development");
        establishmentConfiguration.Property(e => e.PreviousPublicationDate).HasColumnName("previous_publication_date");
        establishmentConfiguration.Property(e => e.PreviousQualityOfEducation)
            .HasColumnName("previous_quality_of_education");
        establishmentConfiguration.Property(e => e.PreviousSafeguardingIsEffective)
            .HasColumnName("previous_safeguarding_is_effective");
        establishmentConfiguration.Property(e => e.PreviousSixthFormProvisionWhereApplicable)
            .HasColumnName("previous_sixth_form_provision_where_applicable");
        establishmentConfiguration.Property(e => e.PublicationDate).HasColumnName("publication_date");
        establishmentConfiguration.Property(e => e.QualityOfEducation).HasColumnName("quality_of_education");
        establishmentConfiguration.Property(e => e.ReligiousEthos).HasColumnName("religious_ethos");
        establishmentConfiguration.Property(e => e.SafeguardingIsEffective).HasColumnName("safeguarding_is_effective");
        establishmentConfiguration.Property(e => e.SchoolName).HasColumnName("school_name");
        establishmentConfiguration.Property(e => e.SchoolNameAtTimeOfLatestFullInspection)
            .HasColumnName("school_name_at_time_of_latest_full_inspection");
        establishmentConfiguration.Property(e => e.SchoolNameAtTimeOfPreviousFullInspection)
            .HasColumnName("school_name_at_time_of_previous_full_inspection");
        establishmentConfiguration.Property(e => e.SchoolNameAtTimeOfTheLatestSection8Inspection)
            .HasColumnName("school_name_at_time_of_the_latest_section_8_inspection");
        establishmentConfiguration.Property(e => e.SchoolOpenDate).HasColumnName("school_open_date");
        establishmentConfiguration.Property(e => e.SchoolTypeAtTimeOfLatestFullInspection)
            .HasColumnName("school_type_at_time_of_latest_full_inspection");
        establishmentConfiguration.Property(e => e.SchoolTypeAtTimeOfPreviousFullInspection)
            .HasColumnName("school_type_at_time_of_previous_full_inspection");
        establishmentConfiguration.Property(e => e.SchoolTypeAtTimeOfTheLatestSection8Inspection)
            .HasColumnName("school_type_at_time_of_the_latest_section_8_inspection");
        establishmentConfiguration.Property(e => e.Section8InspectionOverallOutcome)
            .HasColumnName("section_8_inspection_overall_outcome");
        establishmentConfiguration.Property(e => e.Section8InspectionPublicationDate)
            .HasColumnName("section_8_inspection_publication_date");
        establishmentConfiguration.Property(e => e.SixthForm).HasColumnName("sixth_form");
        establishmentConfiguration.Property(e => e.SixthFormProvisionWhereApplicable)
            .HasColumnName("sixth_form_provision_where_applicable");
        establishmentConfiguration.Property(e => e.TheIncomeDeprivationAffectingChildrenIndexIdaciQuintile)
            .HasColumnName("the_income_deprivation_affecting_children_index_idaci_quintile");
        establishmentConfiguration.Property(e => e.TotalNumberOfPupils).HasColumnName("total_number_of_pupils");
        establishmentConfiguration.Property(e => e.TypeOfEducation).HasColumnName("type_of_education");
        establishmentConfiguration.Property(e => e.Urn).HasColumnName("urn").ValueGeneratedNever();
        establishmentConfiguration.Property(e => e.UrnAtTimeOfLatestFullInspection)
            .HasColumnName("urn_at_time_of_latest_full_inspection");
        establishmentConfiguration.Property(e => e.UrnAtTimeOfPreviousFullInspection)
            .HasColumnName("urn_at_time_of_previous_full_inspection");
        establishmentConfiguration.Property(e => e.UrnAtTimeOfTheSection8Inspection)
            .HasColumnName("urn_at_time_of_the_section_8_inspection");
        establishmentConfiguration.Property(e => e.WebLink).HasColumnName("web_link");
    }
}
