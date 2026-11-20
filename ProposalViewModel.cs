using AssetCare.Model.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using ExpressiveAnnotations.Attributes;

namespace AssetCare.Model.ViewModels
{
    public class ProposalViewModel
    {
        public int Id { get; set; }
        public bool CanDelete { get; set; }
        [Required(ErrorMessage = "Please select a school")]
        public int? PropertyId { get; set; }
        [Required(ErrorMessage = "Please select a work category")]
        public int? WorkCategoryId { get; set; }
        public int? AgencyId { get; set; }
        public int ProposalID { get; set; }
        public string SchoolName { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? ProposalDate { get; set; }
        public string Description { get; set; }
        public string WorkCategory { get; set; }
        public string Phone { get; set; }
        public string RoomLocation { get; set; }
        public string Rooms { get; set; }
        public string PrincipalName { get; set; }
        public string PrincipalEmail { get; set; }
        public List<int> JobBuildingIDs { get; set; }
        public string Status { get; set; }
        public int StatusID { get; set; }
        public int? JobId { get; set; }
        public bool CanCreateJob { get; set; }
        public bool CanCreateCalout { get; set; }
        public string WorkNominatedBy { get; set; }
        public string AwaitingActionBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ProposalStatus { get; set; }
        public string ApprovalStatus { get; set; }
        public string RoleId { get; set; }
        public int DaysSinceLogged { get; set; }
        public string ApprovedBy { get; set; }
        public string CreateProject { get; set; }
        public bool EndorsementEmailSent { get; set; }
        public bool EmailSent { get; set; }
        public bool GrantApplication { get; set; }
        public int? ProposalEndorsementId { get; set; }
        public int? ProjectID { get; set; }
        public DateTime? LastApproved { get; set; }
        public string CurrentRoleID { get; set; }
        public int? CalllogFitsID { get; set; }
        public string Sig { get { return TamperHelper.GenerateSignature(this.Id); } }


    }

    public class ProposalForHPViewModel
    {
        [UnSearchable(true)]
        public int Id { get; set; }
        [UnSearchable(true)]
        [Required(ErrorMessage = "Please select a school")]
        public int? SchoolId { get; set; }
        [UnSearchable(true)]
        [Required(ErrorMessage = "Please select an agency")]
        public int? AgencyId { get; set; }
        public int ProposalID { get; set; }
        public string SchoolName { get; set; }


        public DateTime? ProposalDate { get; set; }
        public string ProposalDateList { get; set; }
        public string RequestedBy { get; set; }
        public string WorkNominatedBy { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        public string ProposalStatus { get; set; }
        public string AwaitingActionBy { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ApprovalDateList { get; set; }
        public int DaysSinceLogged { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? LastApproved { get; set; }
        public string LastApprovedList { get; set; }
        public int? ProjectID { get; set; }
        public string Sig { get { return TamperHelper.GenerateSignature(this.Id); } }

    }

    public class ProposalFilterPostModel : DataTableAjaxPostModel
    {
        public int? AgencyId { get; set; }
        public int? Workcategory { get; set; }
        public int? SchoolId { get; set; }
        public int? PropertyId { get; set; }
        public int ExportType { get; set; }

        public bool GrantApplication { get; set; }
        public string SchoolName { get; set; }

    }

    public class ProposalFilterModel : DataTableFilterModel
    {
        public int? AgencyId { get; set; }
        public int? Workcategory { get; set; }
        public int? SchoolId { get; set; }
        public int? PropertyId { get; set; }
        public string SchoolName { get; set; }
        public int DaysSinceLogged { get; set; }
        public bool GrantApplication { get; set; }
    }

    public class ProposalCustomDropdownItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public string Data { get; set; }
    }

    public class EndorsementViewModel
    {
        public int ProposalID { get; set; }

        public int EndorsementID { get; set; }
        public int? EndorsementTypeID { get; set; }
        public string EndorsementName { get; set; }
        public bool? Approved { get; set; }
        public bool? NotApproved { get; set; }

        [DisplayName("Approver")]
        public string ApprovedBy { get; set; }
        public string RecordedDate { get; set; }
        public string RecordedBy { get; set; }

        public string Position { get; set; }

        public string Limit { get; set; }

        [DisplayName("Approval Date")]
        public DateTime? ApprovalDate { get; set; }
        [DisplayName("Comments (Max. 1000 Characters)")]
        public string Comments { get; set; }



    }
    public class EndorsementTypeViewModel
    {
        public int EndorsementTypeID { get; set; }
        public string EndorsementName { get; set; }

    }


    public class ProposalEntitlementViewModel
    {

        public bool? ValidateEntitlements { get; set; }
        public int ProposalID { get; set; }
     
        [AssertThat(" ValidateEntitlements == true ? (REF== true || DA== true || Exempt== true || CDC == true || PRNone == true  )  : true", ErrorMessage = "At least one should be selected")]             
        public bool REF { get; set; }

        public bool DA { get; set; }

        public bool Exempt { get; set; }

        [DisplayName("with consent")]
        public bool WithConsent { get; set; }

        [DisplayName("without consent")]
        public bool WithoutConsent { get; set; }

        public bool CDC { get; set; }
        [DisplayName("NA")]
        public bool PRNone { get; set; }


        //Environmental planning
        [AssertThat(" ValidateEntitlements == true ? ( Arboist==true || Bushfire== true || Contamination== true || FloodProne==true || Heritage== true || Ecology == true || Archaeology == true || EPNone == true)  : true", ErrorMessage = "At least one should be selected")]
        [DisplayName("Arborist")]
        public bool Arboist { get; set; }

        [DisplayName("Bushfire protection zone")]
        public bool Bushfire { get; set; }

        [DisplayName("Contamination issues")]
        public bool Contamination { get; set; }

        [DisplayName("Flood prone land")]
        public bool FloodProne { get; set; }

        public bool Heritage { get; set; }

        public bool Ecology { get; set; }
        public bool Archaeology { get; set; }
        
        [DisplayName("NA")]
        public bool EPNone { get; set; }

        //Stakeholders to be consulted
        [AssertThat(" ValidateEntitlements == true ? (SSU== true || ITD== true || Demountable== true || AssetActivation==true || SCNone== true)  : true", ErrorMessage = "At least one should be selected")]

        public bool SSU { get; set; }

        public bool ITD { get; set; }

        [DisplayName("Demountable unit")]
        public bool Demountable { get; set; }

        [DisplayName("Asset activation")]
        public bool AssetActivation { get; set; }

        [DisplayName("NA")]
        public bool SCNone { get; set; }

        //Operational constraints
        [AssertThat(" ValidateEntitlements == true ? (ServiceIntereption== true || ExamsInOperation== true || VacationPeriod== true || Relocation==true || OCNone== true || LearningWellbeing==true || WHS==true)  : true", ErrorMessage = "At least one should be selected")]

        [DisplayName("Service Interruption")]
        public bool ServiceIntereption { get; set; }

        [DisplayName("Exams in operation")]
        public bool ExamsInOperation { get; set; }

        [DisplayName("Vacation period only")]
        public bool VacationPeriod { get; set; }

        [DisplayName("Relocation of facilities required")]
        public bool Relocation { get; set; }

        [DisplayName("Learning/Wellbeing")]
        public bool LearningWellbeing { get; set; }

        [DisplayName("WHS")]
        public bool WHS { get; set; }

        [DisplayName("NA")]
        public bool OCNone { get; set; }
        //Tech Services
        [AssertThat(" ValidateEntitlements == true ? (StatPlanning== true || TechHeritage== true || Compliance== true || Sustainability==true || Comms== true || TSNone== true || Leaseslicences==true || CentralServices==true)  : true", ErrorMessage = "At least one should be selected")]

        [DisplayName("Stat planning")]
        public bool StatPlanning { get; set; }

        [DisplayName("Heritage")]
        public bool TechHeritage { get; set; }

        [DisplayName("Compliance")]
        public bool Compliance { get; set; }

        [DisplayName("Sustainability")]
        public bool Sustainability { get; set; }

        [DisplayName("Comms")]
        public bool Comms { get; set; }

        [DisplayName("Leases & licences")]
        public bool Leaseslicences { get; set; }

        [DisplayName("Central Services/Contracts")]
        public bool CentralServices { get; set; }

        [DisplayName("NA")]
        public bool TSNone { get; set; }

       

        //Hazardous materials
        [AssertThat(" ValidateEntitlements == true ? (Asbestos== true || Lead== true || Mould== true || HMNone==true )  : true", ErrorMessage = "At least one should be selected")]

        public bool Asbestos { get; set; }

        public bool Lead { get; set; }

        public bool Mould { get; set; }

        [DisplayName("NA")]
        public bool HMNone { get; set; }

        //Services
        [AssertThat(" ValidateEntitlements == true ? (Electrical== true || ElectricalYes== true || ElectricalNo== true || Hydraulics==true || Sewer==true || Mechanical==true || Stormwater==true || IT==true  || Security==true || SNone==true)  : true", ErrorMessage = "At least one should be selected")]

        public bool Electrical { get; set; }

        [DisplayName("Yes")]
        public bool ElectricalYes { get; set; }

        [DisplayName("No")]
        public bool ElectricalNo { get; set; }

        public bool Hydraulics { get; set; }

        public bool Sewer { get; set; }

        public bool Mechanical { get; set; }

        public bool Stormwater { get; set; }

        public bool IT { get; set; }

        public bool Security { get; set; }

        [DisplayName("NA")]
        public bool SNone { get; set; }


        [StringLength(50)]
        public string RecordedBy { get; set; }

        public DateTime? RecordedDate { get; set; }

        [StringLength(50)]
        public string LastRecordedBy { get; set; }

        public DateTime? LastRecordedDate { get; set; }


       




    }

}
