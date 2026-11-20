using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetCare.Model.Models
{
    [Table("CallLogFITS")]
    public partial class CallLogFITS
    {
        [Key]
        public int CallLogFITSID { get; set; }

        [ForeignKey("Agency")]
        public int? AgencyID { get; set; }

        public int? AgencySequenceID { get; set; }

        [ForeignKey("Property")]
        public int? PropertyID { get; set; }

        //Depricated
        [Column(TypeName = "money")]
        public decimal? SchoolLimit { get; set; }

        [ForeignKey("WorkCategory")]
        public int? WorkCategoryID { get; set; }

        [ForeignKey("SchoolWorkCategory")]
        public int? SchWorkCategoryID { get; set; }

        [ForeignKey("CallLogStatus")]
        public int? CallLogStatusID { get; set; }

        //TODO
        [ForeignKey("CallLogType")]
        public int? CallLogTypeID { get; set; }

        [ForeignKey("MaintenanceTasksCallLog")]
        public int? MaintenanceTaskCallLogID { get; set; }

        [ForeignKey("ReactiveSubCategory")]
        public int? ReactiveSubCategoryID { get; set; }

        // [ForeignKey("CallLogFinanceUrgency")]
        public int? CallLogFinanceUrgencyID { get; set; }

        public int? AssemblyGroupId { get; set; }

        public int? ComponentCASId { get; set; }

        [StringLength(100)]
        public string RoomNumber { get; set; }

        [StringLength(50)]
        public string RoomLocation { get; set; }

        [StringLength(128)]
        public string RequestedBy { get; set; }

        [StringLength(256)]
        public string RequesterPosition { get; set; }

        public DateTime? CallDate { get; set; }
        public DateTime? IncidentDate { get; set; }

        public DateTime? JobCreatedDate { get; set; }

        [StringLength(128)]
        public string CallTakenBy { get; set; }

        [StringLength(1000)]
        public string CallLogDesc { get; set; }

        [StringLength(100)]
        public string EventNo { get; set; }

        [StringLength(15)]
        public string ContractorWONo { get; set; }

        public DateTime? EmailContractorDate { get; set; }

        public bool? Complete { get; set; }

        public bool? WYD { get; set; }

        public bool? EUR100 { get; set; }

        public bool? OHS { get; set; }

        public bool? CommerceNotInvolved { get; set; }

        public int Version { get; set; }

        [StringLength(128)]
        public string RecordedBy { get; set; }

        public DateTime? RecordedDate { get; set; }

        [StringLength(128)]
        public string LastRecordedBy { get; set; }

        public DateTime? LastRecordedDate { get; set; }

        public bool? SchEUR100 { get; set; }

        public bool? TreeSafetyIssue { get; set; }
        [ForeignKey("Element")]
        public int? ElementId { get; set; }

        [ForeignKey("WorkType")]
        public int? WorkTypeId { get; set; }
        [StringLength(100)]
        public string EmailAlertTypes { get; set; }

        [StringLength(100)]
        public string HAZMAT { get; set; }

        public bool? LocalCotractorToRepairDamage { get; set; }
        [ForeignKey("SchoolIncidentCause")]
        public int? SchoolIncidentCauseId { get; set; }

        [StringLength(20)]
        public string RequesterContact { get; set; }

        public bool? FMCRecommend { get; set; }
        public DateTime? FMCRecommendedDate { get; set; }

        [StringLength(128)]
        public string FMCRecommendedBy { get; set; }

        [StringLength(128)]
        public string ProcessedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public bool? IsInsuranceEvent { get; set; }
        public bool? IsThirdpartyInvolved{ get; set; }

        [StringLength(500)]
        public string CauseOfTheIssue { get; set; }
        
        public int? SchoolUrgencyID { get; set; }

        public bool CapitalDefect { get; set; }
        
        [StringLength(1000)]
        public string CallLogComments { get; set; }
        public DateTime? LastEmailSentDate { get; set; }
        public bool? DeletedYN { get; set; }

        [StringLength(1000)]
        public string Comments { get; set; }
        public DateTime? LastEmailDate { get; set; }

        public string RoleID { get; set; }
        public bool? IsInsuranceEventByWO { get; set; }
        [StringLength(1000)]
        public string IsInsuranceEventByWOComments { get; set; }

        public bool? HasSchoolContributionWorkOrder { get; set; }
        [StringLength(1000)]
        public string HasSchoolContributionWorkOrderComments { get; set; }
        [StringLength(100)]
        public string SiteContactPerson { get; set; }
        [StringLength(20)]
        public string SiteContact { get; set; }
        
        [StringLength(1000)]
        public string CallStatusComments { get; set; }

        [StringLength(64)]
        public string ClaimNumber { get; set; }
        [StringLength(64)]
        public string CORPFileNo { get; set; }
        [ForeignKey("AssessorStatus")]
        public int? AssessorStatusID { get; set; }
        [StringLength(1000)]
        public string CommentsByIRCM { get; set; }
        [StringLength(128)]
        public string ProcessedByIRCM { get; set; }
        public DateTime? ProcessedDateByIRCM { get; set; }
        public bool? IsReadyForReimbursement { get; set; }
        public bool? IsReimbursementComplete { get; set; }
        public DateTime? TotalEstimationOverLimitNotificationDate { get; set; }

        //Security details
        public bool? IsSecInsuranceEvent { get; set; }
        public bool? IsSecurityIncident { get; set; }
        [StringLength(100)]
        public string SecEventNo { get; set; }
        [StringLength(500)]
        public string SecCauseOfTheIssue { get; set; }
        public bool? SecIsThirdPartyInvolved { get; set; }
        public bool? SecLocalCotractorToRepairDamage { get; set; }
        [ForeignKey("SecSchoolIncidentCause")]
        public int? SecSchoolIncidentCauseId { get; set; }

        public DateTime? SecIncidentDate { get; set; }
        public virtual SchoolIncidentCause SecSchoolIncidentCause { get; set; }

        public virtual Property Property { get; set; }
        public virtual Agency Agency { get; set; }
        public virtual WorkCategory WorkCategory { get; set; }
        public virtual WorkCategory SchoolWorkCategory { get; set; }
        public virtual CallLogStatus CallLogStatus { get; set; }
        public virtual CallLogType CallLogType { get; set; }
        public virtual AssessorStatus AssessorStatus { get; set; }
        public virtual CallLogFinanceUrgency CallLogFinanceUrgency { get; set; }

       // public virtual CallLogFinanceUrgency CallLogFinanceUrgencySchool { get; set; }

        public virtual MaintenanceTasksCallLog MaintenanceTasksCallLog { get; set; }
        public virtual SchoolIncidentCause SchoolIncidentCause { get; set; }

        public virtual ICollection<ContractVariation> ContractVariations { get; set; }
        public virtual Element Element { get; set; }
        public virtual WorkType WorkType { get; set; }
        public virtual ReactiveSubCategory ReactiveSubCategory { get; set; }

    }
}
