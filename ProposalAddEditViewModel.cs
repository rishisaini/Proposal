using AssetCare.Model.Common;
using AssetCare.Model.Models;
using ExpressiveAnnotations.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AssetCare.Model.ViewModels
{
    public class ProposalAddEditViewModel
    {
        public string mode;

        public ProposalAddEditViewModel()
        {
            this.SupportDocument = new SupportDocumentViewModel();
            this.SupportDocuments = new List<SupportDocumentViewModel>();
        }

        public int? AgencySequenceId { get; set; }
        public int ProposalId { get; set; }
        public int AgencyId { get; set; }
        public int SchoolId { get; set; }
        public int PropertyId { get; set; }
        public int? CallOutId { get; set; }
        public string SchoolName { get; set; }
        public string SchoolNameFull { get; set; }
        public string Phone { get; set; }
        public string SchoolEmail { get; set; }
        public string DELEmail { get; set; }

        [Required(ErrorMessage = "Principal email is required")]
        public string PrincipalEmail { get; set; }
        [Required(ErrorMessage = "Principal name is required")]
        public string PrincipalName { get; set; }
        public decimal? SchoolLimit { get; set; }
        public string Area { get; set; }
        public int? WorkCategoryId { get; set; }
        public int? WorkTypeId { get; set; }

        public string ASOName { get; set; }
        public int ASOID { get; set; }
        public string ASOEmail { get; set; }
        public string GLEmail { get; set; }
        public string SGLEmail { get; set; }
        public string DAMEmail { get; set; }
        public string EDAMEmail { get; set; }
        public string EDEmail { get; set; }

        public string ASOContactNumber { get; set; }

        public string CallbackUrl { get; set; }

        public string Approver { get; set; }

        public string Position { get; set; }
        public string Contact { get; set; }
        public string SaveType { get; set; }
        public string Sig { get { return TamperHelper.GenerateSignature(ProposalId); } }

        public int SelectedSchWorkCategoryId { get; set; }
        public string SchWorkCategory { get; set; }
        [Required(ErrorMessage = "Incident Date is required")]
        public DateTime? ProposalDate { get; set; }

        [Required(ErrorMessage = "Call time is required")]
        [Display(Name = "Time of Incident To")]
        public DateTime? CallLogTime { get; set; }

        [Required(ErrorMessage = "Callout description is required.")]
        public string CallLogDesc { get; set; }

        //TODO Buildings
        //[RequiredIf("UserType != 'SL' && UserType != 'GL'", ErrorMessage = "Buildings affected is required")]
        public List<int> JobBuildingIDs { get; set; }

        public int DefaultBuildingId { get; set; }


        //[RequiredIf("UserType != 'SL' && UserType != 'GL'", ErrorMessage = "Rooms are required")]
        [MaxLength(100, ErrorMessage = "Max. 100 Characters")]
        public string Rooms { get; set; }
        public string RoomSchool { get; set; }
        public string RoomLocation { get; set; }
        public string CurrentRoleID { get; set; }
        public string RequesterPosition { get; set; }
        public string RequestedBy { get; set; }
        public string RequesterContact { get; set; }
        public DateTime? ApprovalDate { get; set; }
        //[Required(ErrorMessage = "Requested By is required")]
        //public string RequestedBy { get; set; }
        [Required(ErrorMessage = "Response Time is required")]
        public int? SelectedCallLogFinanceUrgenyId { get; set; }

        //[AssertThat("(showHideSection == false && Submitted == true ) ? SelectedCallStatusId==11: true", ErrorMessage = "Submitted must be selected ")]
        public int? SelectedCallStatusId { get; set; }

        [Required(ErrorMessage = "Requester Title is required")]
        public string RequestorTitle { get; set; }
        public int? JobId { get; set; }
        public string JobComments { get; set; }
        public bool JobCancelled { get; set; }

        public string AcceptedFileTypes
        {
            get
            {
                return ConfigurationManager.AppSettings["AcceptedFileTypes"].ToString();
            }
        }
        public string cssClassSE { get; set; }
        public string cssClassE { get; set; }

        [RequiredIf("WorkCategoryId == 2 || WorkCategoryId == 13", ErrorMessage = "Maintenance task is required")]
        public int? SelectedMaintenanceCallLogId { get; set; }
        public int? SelectedEquipmentTypeId { get; set; }
        public int? SelectedDocumentTypeId { get; set; }
        public SupportDocumentViewModel SupportDocument { get; set; }
        public ProposalRoles ProposalRoles { get; set; }
        public List<SupportDocumentViewModel> SupportDocuments { get; set; }
        public string SupportDocs { get; set; }
        public bool Completed { get; set; }
        public bool FmcReadOnly { get; set; }
        public bool FormReadOnly { get; set; }
        public bool CanDeleteDocument { get; set; }

        public ProposalUserCapabilities UserCapabilities { get; set; }
        public bool DisableField(string section, string field)
        {
            return HelperMethods.DisableProposalField(this.UserCapabilities, section, field);
        }

        public bool EnableAMU100 { get; set; }
        public bool EnablePWANotInvolved { get; set; }

        public AgencyContractor Contractor { get; set; }
        public string SchoolAddress { get; set; }
        public Property SchoolDetails { get; set; }
        public bool CanEditFMCOnly { get; set; }
        public int? SchoolLimitId { get; set; }
        public bool SubmitDisabled { get; set; }
        public DateTime? ReportedDate { get; set; }

        [Required(ErrorMessage = "Work description is required")]
        public string WorkDescription { get; set; }

        public DateTime? SubmittedtDate { get; set; }
        public string PRSubmittedDate { get; set; }

        public DateTime? ProjectDate { get; set; }

        [RequiredIf("Submitted == true && ConceptEstimate == null", ErrorMessage = "Concept estimate is required")]
        public decimal? ConceptEstimate { get; set; }
        public decimal? ConceptEstimateGST { get; set; }

        //[RequiredIf("((SchoolFunded == true || CommunityFunded == true) && AMUFunded !=true)", ErrorMessage = "Funds available is required")]
        public decimal? FundsAvailability { get; set; }
        public decimal? FundsAvailabilityHidden { get; set; }

        public decimal? FundsAvailabilityDisabled { get; set; }
        public decimal? FundsAvailabilityOld { get; set; }

        [Range(0, 100, ErrorMessage = "Enter number between 0 to 100")]
        public int? JointFunding { get; set; }

        public int? ProjectID { get; set; }
        public int? NomID { get; set; }
        public decimal? ConceptEstimateDisabled { get; set; }

        public bool showHideSection { get; set; }


        public bool showHideGLSLSection { get; set; }
        public bool showHideSDEDSection { get; set; }
        public bool showHideReviewSection { get; set; }

        public string UserType { get; set; }

        public List<EndorsementViewModel> SchoolEndorsements { get; set; }

        public List<EndorsementViewModel> Endorsements { get; set; }
        public List<ProposalEndorsementType> EndorsementType { get; set; }

        [RequiredIf("Submitted == true", ErrorMessage = "Project type is required")]
        public int? ProjectTypeID { get; set; }
        [RequiredIf("Submitted == true", ErrorMessage = "Work nominated is required")]
        public int? ProposalWorkNominatedID { get; set; }
        public int? ProposalWorkNominatedIDHidden { get; set; }
        [RequiredIf("Submitted == true", ErrorMessage = "Program is required")]
        public int? ProposalProgramID { get; set; }

        [RequiredIf("(ProposalProgramID ==137 || ProposalProgramID ==138 || ProposalProgramID ==139) ", ErrorMessage = "Proposal Procurement is required")] //TODO: this id is diffenret on Local....
        public int? ProposalProcurementID { get; set; }
        //[RequiredIf("Submitted == true && ProposalProgramID!=14", ErrorMessage = "Sub-Program is required")]
        public int? ProposalSubProgramID { get; set; }
        [RequiredIf("Submitted == true ", ErrorMessage = "PMM profile is required")]
        public int? PMMProfileID { get; set; }

        //[RequiredIf("SchoolFunded == true || CommunityFunded == true", ErrorMessage = "Funding source is required")]
        public int? ProposalSFCID { get; set; }

        public string UserEmail { get; set; }
        public int? UserID { get; set; }

        public int DefaultProgramForLRLV { get; set; }
        public string RCEmail { get; set; }
        public int? UserIDPMASOHidden { get; set; }
        public string RoleID { get; set; }
        [Required(ErrorMessage = "Project title is required")]
        public string ProjectDescription { get; set; }
        public string ProjectDescriptionSchool { get; set; }

        public string Functionality { get; set; }
        public string SubmittedBy { get; set; }

        public bool Submitted { get; set; }
        [Display(Name = "Submit")]
        public bool ProposalSubmission { get; set; }
        public bool ProposalSubmissionDisabled { get; set; }
        public string ReviewedBy { get; set; }
        public bool Reviewed { get; set; }

        [Display(Name = "HAZMAT,Heritage or Safety Implications?")]
        public bool Hazmat { get; set; }

        [Required(ErrorMessage = "School fund code is required")]
        public bool SchoolFunded { get; set; }
        public bool SchoolFundedHidden { get; set; }
        public bool PlannedMaintenance { get; set; }
        public bool PlannedMaintenanceHidden { get; set; }
        public bool AMUFundedHidden { get; set; }
        public bool CommunityFundedHidden { get; set; }

        public bool SchoolRecurrent { get; set; }
        public bool SchoolRecurrentHidden { get; set; }
        public int ProgramSelectListHidden { get; set; }
        public string ProgramName { get; set; }

        public bool ReviewedDisbaled { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string PRReviewDate { get; set; }
        public string AwaitingActionBy { get; set; }
        public int? DaysSinceLogged { get; set; }
        public string statusMessageSchool { get; set; }

        public string ProposalProgramText { get; set; }

        public string ProposalProcurementText { get; set; }
        public string WorkNominatedByText { get; set; }
        public string ProjectTypeText { get; set; }
        public string PMMText { get; set; }
        public string ProposalSubProgramText { get; set; }

        public string ProposalProjectTypeText { get; set; }
        public string ProposalSFCText { get; set; }
        public string ASOText { get; set; }
        public string ProposalStatusText { get; set; }
        public string BuildingList { get; set; }
        public string SPApprovedBy { get; set; }
        public string SPApprovalDate { get; set; }
        public string SPApprovalStatus { get; set; }
        public string SPPosition { get; set; }

        public string SDApprovedBy { get; set; }
        public string SDApprovalDate { get; set; }
        public string SDApprovalStatus { get; set; }
        public string SDPosition { get; set; }

        public string EDApprovedBy { get; set; }
        public string EDApprovalDate { get; set; }
        public string EDApprovalStatus { get; set; }
        public string EDPosition { get; set; }

        public string ASOApprovedBy { get; set; }
        public string ASOApprovalDate { get; set; }
        public string ASOApprovalStatus { get; set; }
        public string ASOPosition { get; set; }

        public string GLApprovedBy { get; set; }
        public string GLApprovalDate { get; set; }
        public string GLApprovalStatus { get; set; }
        public string GLPosition { get; set; }
        public string SGLApprovedBy { get; set; }
        public string SGLApprovalDate { get; set; }
        public string SGLApprovalStatus { get; set; }
        public string SGLPosition { get; set; }

        public string DAMApprovedBy { get; set; }
        public string DAMApprovalDate { get; set; }
        public string DAMApprovalStatus { get; set; }
        public string DAMPosition { get; set; }

        public string EDAMApprovedBy { get; set; }
        public string EDAMApprovalDate { get; set; }
        public string EDAMApprovalStatus { get; set; }
        public string EDAMPosition { get; set; }

        public string PlanningRequirementText { get; set; }
        public string EnvironmentPlanningText { get; set; }
        public string StakeholderText { get; set; }
        public string OperationalText { get; set; }
        public string TechServicesText { get; set; }
        public string HazardousText { get; set; }
        public string ServicesText { get; set; }

        public bool DeliveryTypeLRLV { get; set; }
        public bool DeliveryTypeLRLVHidden { get; set; }
        public bool DeliveryTypeSSD { get; set; }
        public bool DeliveryTypeSSDHidden { get; set; }
        public bool DeliveryTypeAMU { get; set; }
        public bool DeliveryTypeAMUHidden { get; set; }
        public int? CallLogFITSID { get; set; }

        public string CallLogSig { get { return TamperHelper.GenerateSignature(CallLogFITSID.HasValue ? CallLogFITSID.Value : 0); } }
        public int? NewCalloutID { get; set; }

        public int POCDocumentTypeID { get; set; }
        public bool CommunityFunded { get; set; }
        public bool AMUFunded { get; set; }
        
        
        [Display(Name = "School Self-Delivery?")]
        public bool SchoolSelfDelivery { get; set; }

        [Display(Name = "Grant Application")]
        public bool GrantApplication { get; set; }
        public bool GrantApplicationHidden { get; set; }
        public bool SchoolSelfDeliveryHidden { get; set; }



        [Remote("IsValidEntitlement", "Proposals", AdditionalFields = "Rooms", HttpMethod = "POST", ErrorMessage = "Please select one checkbox")]
        public ProposalEntitlementViewModel Entitlements { get; set; }

        public string SchoolSitePlanPDFPath { get; set; }
        public string AsbestosRegisterPDFPath { get; set; }

        public string CurrentUserRoleID { get; set; }
    }

    [Serializable()]
    public class ProposalUserCapabilities
    {
        public string SchoolSection { get; set; }
        public string DetailSection { get; set; }
        public string SchoolEndorsement { get; set; }
        public string Endorsement { get; set; }
        public string EntitlementSection { get; set; }
        public string ClaimSection { get; set; }
        public string DocumentSection { get; set; }


    }



}
