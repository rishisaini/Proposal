using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetCare.Model.Models
{
    [Table("Proposal")]
    public partial class Proposal
    {
        [Key]
        public int ProposalID { get; set; }

        [ForeignKey("Agency")]
        public int? AgencyID { get; set; }

        public int? AgencySequenceID { get; set; }

        [ForeignKey("Property")]
        public int? PropertyID { get; set; }

        //Depricated
        [Column(TypeName = "money")]
        public decimal? SchoolLimit { get; set; }

        [StringLength(50)]
        public string PrincipalName { get; set; }

        [StringLength(50)]
        public string PrincipalEmail { get; set; }

        [ForeignKey("ProposalStatus")]
        public int? ProposalStatusID { get; set; }

        [StringLength(100)]
        public string RoomNumber { get; set; }

        [StringLength(50)]
        public string RoomLocation { get; set; }

        public DateTime? ProposalDate { get; set; }

        [StringLength(50)]
        public string RecordedBy { get; set; }

        public DateTime? RecordedDate { get; set; }

        [StringLength(50)]
        public string LastRecordedBy { get; set; }

        public DateTime? LastRecordedDate { get; set; }

        // public bool? TreeSafetyIssue { get; set; }
        [ForeignKey("Element")]
        public int? ElementId { get; set; }

        [ForeignKey("WorkType")]
        public int? WorkTypeId { get; set; }

        //New Columns
        public DateTime? ReportedDate { get; set; }
        [StringLength(1000)]
        public string WorkDescription { get; set; }

        [StringLength(1000)]
        public string ProjectDescription { get; set; }

        public DateTime? SubmittedtDate { get; set; }

        public DateTime? ProjectDate { get; set; }

        [Column(TypeName = "money")]
        public decimal? ConceptEstimate { get; set; }

        [Column(TypeName = "money")]
        public decimal? FundsAvailability { get; set; }

        public int? PMMProjectID { get; set; }

        public int? NomID { get; set; }

        public DateTime? ProposalExtractDate { get; set; }

        [ForeignKey("ProposalProjectType")]
        public int? ProposalProjectTypeID { get; set; }

        [ForeignKey("ProposalWorkNominated")]
        public int? ProposalWorkNominatedID { get; set; }

        [ForeignKey("ProposalProgram")]
        public int? ProposalProgramID { get; set; }

        [ForeignKey("ProposalSubProgram")]
        public int? ProposalSubProgramID { get; set; }

        [ForeignKey("ProposalPMMProfile")]
        public int? ProposalPMMProfileID { get; set; }

        [ForeignKey("ProposalSFC")]
        public int? ProposalSFCID { get; set; }

        [ForeignKey("ProposalProcurement")]
        public int? ProposalProcurementID { get; set; }
        

        [StringLength(50)]
        public string SubmittedBy { get; set; }

        public bool Submitted { get; set; }

        public bool ProposalSubmission { get; set; }

        [StringLength(50)]
        public string ReviewedBy { get; set; }

        public bool Reviewed { get; set; }

        public bool Emailsent { get; set; }
        public bool SchoolFunded { get; set; }
        public bool CommunityFunded { get; set; }
        public bool AMUFunded { get; set; }
        public bool PlannedMaintenance { get; set; }
        public bool SchoolSelfDelivery { get; set; }
        public bool GrantApplication { get; set; }
        public bool Hazmat { get; set; }

        public bool DeliveryTypeLRLV { get; set; }
        public bool DeliveryTypeSSD { get; set; }
        public bool DeliveryTypeAMU { get; set; }



        public string Functionality { get; set; }

        [StringLength(50)]
        public string UserEmail { get; set; }

        public int? UserID { get; set; }

        public string RoleID { get; set; }

        public DateTime? ReviewDate { get; set; }

        public string AwaitingActionBy { get; set; }

        public int? DaysSinceLogged { get; set; }
        public int? JointFunding { get; set; }

        [Column(TypeName = "money")]
        public decimal? FundsAvailabilityOld { get; set; }

        public bool SchoolRecurrent { get; set; }
        public int? CallLogFITSID { get; set; }


        // End New columns
        public virtual Property Property { get; set; }
        public virtual Agency Agency { get; set; }
        public virtual ProposalStatus ProposalStatus { get; set; }

        public bool? DeletedYN { get; set; }

        public virtual Element Element { get; set; }
        public virtual WorkType WorkType { get; set; }

        public virtual ProposalProjectType ProposalProjectType { get; set; }

        public virtual ProposalWorkNominated ProposalWorkNominated { get; set; }

        public virtual ProposalProgram ProposalProgram { get; set; }

        public virtual ProposalSubProgram ProposalSubProgram { get; set; }

        public virtual ProposalPMMProfile ProposalPMMProfile { get; set; }

        public virtual ProposalSFC ProposalSFC { get; set; }
        public virtual ProposalProcurement ProposalProcurement { get; set; }


    }
}
