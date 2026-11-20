using AssetCare.Common;
using AssetCare.Controllers;
using AssetCare.Helpers;
using AssetCare.Model.Common;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Service;
using AssetCare.Service.Interfaces;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;
using Constants = AssetCare.Common.Constants;

namespace AssetCare.Areas.FMWorks.Controllers
{
    [AuthorizeOverride(Roles = "ADMIN,ADMINGEN,SUPPORT,CONTRACTOR,PROJECTMNGR,PROJECTDIR,SCHOOL,SCHOOLMGR,DETAMU,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,CALLCENTERSTAFF,GUEST,DOEUSER,SINSW")]
    public class CallOutController : BaseController
    {
        private IPropertyService _propertyService;
        private IBuildingService _buildingService;
        private IUserService _userService;
        private ICalloutService _calloutService;
        private IDefectService _defectService;
        private ICreateScheduleService _createScheduleService;
        private IIRCMService _ircmService;
        private IAgencyService _agencyService;
        //private ISMSService _smsService;
        private readonly IMapper _mapper;
        private readonly string cachingKey = "CreateScheduleReferenceData";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public CallOutController(ICreateScheduleService createScheduleService, ICalloutService calloutService, IDefectService defectService, 
            IUserService userService, IAgencyService agencyService, IPropertyService propertyService, IBuildingService buildingService, 
            IIRCMService ircmService, IBaseService baseService, IMapper mapper) : base(baseService)
        {
            _calloutService = calloutService;
            _defectService = defectService;
            _baseService = baseService;
            _propertyService = propertyService;
            _buildingService = buildingService;
            _userService = userService;
            _createScheduleService = createScheduleService;
            _ircmService = ircmService;
            _agencyService = agencyService;
            //_smsService = smsService;
            _mapper = mapper;
        }

        [HttpPost]
        public PartialViewResult QuickHelp(bool capitalDefect)
        {
            if (capitalDefect)
            {
                return PartialView("QuickHelp/_CapitalDefectDetail");
            }
            return PartialView("QuickHelp/_ReactiveCalloutDetail");
        }

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (CurrentUser == null) return;
            int agencyId = GetAgencyId();
            List<int> userPropertyIds = CurrentUser.UserPropertyIds;
            List<SelectListItem> schoolList = new List<SelectListItem>();
            var userDetails = _userService.GetByUserCode(User.UserCode(), AgencyId.HasValue ? AgencyId.Value : 0);
            schoolList = _propertyService.GetSchoolLookupWithUserRestrictions(agencyId, userDetails.UserPropertyIds).Select(x => new SelectListItem() { Value = x.Key.ToString(), Text = x.Value }).ToList();
            ViewData["SchoolsList"] = schoolList;
            List<SelectListItem> wcNew = _calloutService.GetWorkCategory().Select(x => new SelectListItem() { Text = String.Format("{0} - {1}", x.Code, x.Description), Value = x.WorkCategoryId.ToString() }).OrderBy(x => x.Text).ToList();
            ViewData["WorkCategoryList"] = wcNew;
            ViewData["Elements"] = _calloutService.GetElementList().Where(e => e.ElementID != 0).Select(x => new SelectListItem() { Value = x.ElementID.ToString(), Text = x.ElementDesc }).ToList();
            ViewData["WorkTypes"] = _calloutService.GetWorkTypes().Select(x => new SelectListItem() { Value = x.WorkTypeID.ToString(), Text = x.Description }).ToList();
            ViewData["WorkTypeList"] = _calloutService.GetWorkTypes();
            ViewData["ReadonlySchools"] = _propertyService.GetReadOnlySchools(agencyId);
            ViewData["Assembly"] = new List<SelectListItem>() { };

            ViewData["SubContractorBusinessType"] = _calloutService.GetSubContractorBusinessTypes().Select(x => new SelectListItem() { Text = String.Format("{0}", x.Description), Value = x.BusinessTypeId.ToString() }).OrderBy(x => x.Text).ToList();

            ViewData["TypeOfIncident"] = _ircmService.CurrentContext.SchoolIncidentCauseRepository.Items()
                 .Select(x => new SelectListItem() { Text = x.SchoolIncidentCauseDesc, Value = x.SchoolIncidentCauseID.ToString() }).OrderBy(a => a.Text).ToList(); ;

            //Security Incident
            ViewData["SecTypeOfIncident"] = _ircmService.CurrentContext.SchoolIncidentCauseRepository.Items()
             .Where(x => x.SchoolIncidentCauseDesc != "Storm Damage" && x.SchoolIncidentCauseDesc != "Accidental Damage")
             .Select(x => new SelectListItem() { Text = x.SchoolIncidentCauseDesc, Value = x.SchoolIncidentCauseID.ToString() })
             .OrderBy(a => a.Text)
             .ToList();

            ViewData["PSMWorkAssignmentStatusId"] = _defectService.GetPSMWorkAssignmentStatus().Select(x => new SelectListItem() { Value = x.Id.ToString(), Text = x.Status });
        }

        private ScheduledRateDetailReferenceData GetReferenceData(int agencyId)
        {
            var refModel = HttpContext.Cache.Get($"{cachingKey}{agencyId}") as ScheduledRateDetailReferenceData;

            if (refModel == null)
            {
                refModel = _createScheduleService.GetDetailReferenceData(agencyId);
                HttpContext.Cache.Insert($"{cachingKey}{agencyId}", refModel, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration);
            }

            return refModel;
        }

        public ActionResult Index(int? schoolId, int? workCategoryId, int? elementId, bool? showIsReadyForReimbursementOnly)
        {
            workCategoryId = 1; //Set workcategory ID to 1 as all will follow EUR work flow
            CallOutViewModel model = new CallOutViewModel();
            model.AgencyId = GetAgencyId();
            model.WorkCategoryId = workCategoryId;
            
            model.PropertyId = schoolId.HasValue ? schoolId : (User.IsSchoolUser() ? CurrentUser.DefaultPropertyId : null);
            model.ElementId = elementId;
            model.CanDelete = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG") ? true : false;
            model.CanCreateJob = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG" || CurrentUser.RoleID == "C" || CurrentUser.RoleID == "CS" || CurrentUser.RoleID == "LC") ? true : false;
            model.CanCreateCalout = (CurrentUser.RoleID == "CS"
                                     || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                                     || CurrentUser.RoleID == "C"
                                     || CurrentUser.RoleID == "F"
                                     || CurrentUser.RoleID == "SM"
                                     || CurrentUser.RoleID == "P"
                                     || CurrentUser.RoleID == "D"
                                     || CurrentUser.RoleID == "GL"
                                     || CurrentUser.RoleID == "SL" 
                                     || CurrentUser.RoleID == "AM"
                                     || CurrentUser.RoleID == "EM"
                                     || CurrentUser.RoleID == "AO"
                                     || CurrentUser.RoleID == "CO") ? true : false;
            model.CurrentRoleID = CurrentUser.RoleID;
            model.ShowIsReadyForReimbursementOnly = (showIsReadyForReimbursementOnly == true);
            return View(model);
        }

        public ActionResult Detail(int? propertyId, int? workCategoryId, int? id, int? maintenanceCallLogId, int? elementId, string callbackUrl = "", string sig ="")
        {
            int agencyId = GetAgencyId();

            CallOutAddEditViewModel callOutAddEdit;
            if (propertyId.HasValue && workCategoryId.HasValue)
            {
                initCallOutMasterData(propertyId, workCategoryId, null, null);
                //get add model
                callOutAddEdit = _calloutService.GetAddCalloutViewModel(agencyId, propertyId.Value, workCategoryId.Value, id);
                callOutAddEdit.SelectedMaintenanceCallLogId = maintenanceCallLogId;
                callOutAddEdit.RequestedBy = string.Format("{0} {1}", CurrentUser.FirstName, CurrentUser.LastName);
                callOutAddEdit.CallTakenBy = CurrentUser.UserCode;
                callOutAddEdit.RequesterPosition = string.IsNullOrEmpty(CurrentUser.JobTitle) ? CurrentUser.RoleDescNew : CurrentUser.JobTitle;
                callOutAddEdit.CallLogStatusID = 1; //set to logged status
                //callOutAddEdit.CallLogFinanceUrgencyID = 0;
                callOutAddEdit.Complete = false;
                callOutAddEdit.RequesterContact = CurrentUser.Mobile ?? CurrentUser.Phone;
                callOutAddEdit.RoleID = CurrentUser.RoleID;
                //As requested by business, removing Cancelled status
                //RemoveCancelledCallLogStatus();
            }
            else
            {
                if (!TamperHelper.ValidateSignature(id.Value, sig))
                {
                    Toast.ShowError(TempData, Constants.NoAccessToRecordViaQuerystringModification);
                    return RedirectToAction("Index");
                }

                var estimationThreshold = CalloutSharedFunctions.GetTotalEstimationThresholdValue();
                //get view model for existing
                callOutAddEdit = _calloutService.GetCallOutDetails(id, agencyId, estimationThreshold);
                callOutAddEdit.FormReadOnly = (callOutAddEdit.JobId != null || callOutAddEdit.CallLogStatusID == 2 || callOutAddEdit.CallLogStatusID == 7) ? true : false;
                callOutAddEdit.SupportDocument.SupportDocumentClassID = _calloutService.GetSupportDocumentClassId(callOutAddEdit.WorkCategoryID.Value);
                //reset download urls
                //foreach (var doc in callOutAddEdit.SupportDocuments)
                //{
                //    if (!String.IsNullOrEmpty(doc.AttachmentLink))
                //    {
                //        doc.AttachmentLink = Url.Content("~/") + doc.AttachmentLink.Remove(0, 1);
                //    }
                //}
                //foreach (var doc in callOutAddEdit.SupportDocuments)
                //{
                //    doc.SupportDocumentIDEnc = AesIdProtector.Encrypt(doc.SupportDocumentID);

                //    if (doc.SupportDocumentID > 0)
                //    {
                //        doc.RawAttachmentLink = String.Format("{0}{1}?token={2}",
                //            Url.HttpRouteUrl("DefaultApi", new
                //            {
                //                controller = "Callout",

                //            }),
                //            "/DownloadFile",
                //            Server.UrlEncode(AesIdProtector.Encrypt(doc.SupportDocumentID))
                //            );
                //    }

                //    doc.SupportDocumentID = -1;
                //}
                this.UseEncSupportDocumentViewModel("CallOut", AgencyId.Value, callOutAddEdit.SupportDocuments);
                callOutAddEdit.mode = "edit";
                callOutAddEdit.CurrentRoleID = CurrentUser.RoleID;
                initCallOutMasterData(callOutAddEdit.PropertyID, callOutAddEdit.WorkCategoryID, callOutAddEdit.ElementId, callOutAddEdit.ComponentCASId, callOutAddEdit.AssemblyGroupId,callOutAddEdit.CapitalDefect);
                //if (callOutAddEdit.CallLogStatusID.HasValue && callOutAddEdit.CallLogStatusID.Value != 2)
                //{
                //    RemoveCancelledCallLogStatus();
                //}
            }

            //LRLV callout (from Proposal) must always show as School Funded only
            if (callOutAddEdit.ProposalId > 0)
            {
                callOutAddEdit.SchoolFunded = true;
                callOutAddEdit.IsInsuranceEvent = false;
                callOutAddEdit.IsSecurityIncident = false;
            }

            if (!CanAccessCallout(callOutAddEdit))
            {
                Toast.ShowError(TempData, Constants.NoAccessToRecord);
                return (CurrentUser.RoleID == "PM" || CurrentUser.RoleID == "PD") ? RedirectToAction("DefectListIndex", "Defect") : RedirectToAction("Index");
            }
            if (!IsInCurrentRegion(callOutAddEdit.AgencyID))
            {
                Toast.ShowError(TempData, Constants.SwitchRegionFirst);
                return (CurrentUser.RoleID == "PM" || CurrentUser.RoleID == "PD") ? RedirectToAction("DefectListIndex", "Defect") : RedirectToAction("Index");
            }
            //callOutAddEdit.SchoolSitePlanPDFPath = new SharedFunctions().GetSchoolSitePlanPDFPath(callOutAddEdit.SchoolId); //comments in pre-prod
            //callOutAddEdit.AsbestosRegisterPDFPath = new SharedFunctions().GetAsbestosRegisterPDFPath(callOutAddEdit.SchoolId); //comments in pre-prod

            callOutAddEdit.CapitalDefectCalloutViewModel = GetCapitalDefectDetail(callOutAddEdit, id, agencyId);
            callOutAddEdit.CapitalDefectCompleteForValidation = callOutAddEdit.CapitalDefectCalloutViewModel.Complete;
            callOutAddEdit.UserCapabilities = GetUserCapabilities(callOutAddEdit);
            callOutAddEdit.CanAddEditCapitalDefect = CanAddEditCapitalDefect(callOutAddEdit);
            callOutAddEdit.UsersCanAddEditCapitalDefect = UsersCanAddEditCapitalDefect();
            callOutAddEdit.ShowSave = ShowSave(callOutAddEdit.UserCapabilities, callOutAddEdit);
            string decodedQueryString = HttpUtility.UrlDecode(Request.QueryString.ToString());
            if (decodedQueryString.IndexOf("callbackUrl=") > 0)
            {
                var callbackUrls = decodedQueryString.Substring(decodedQueryString.ToLower().IndexOf("callbackurl=") + "callbackUrl=".Length);
                callOutAddEdit.CallbackUrl = callbackUrls;
            }

            TempData["CALLOUT_USERCAPABILITIES"] = callOutAddEdit.UserCapabilities;
            if (_agencyService.IsAMURegion(agencyId))
            {
                callOutAddEdit.IsAMURegion = true;
            }
            //if (callOutAddEdit.DefectMgmt==true)
            //    callOutAddEdit.WorkCategoryID = 2;
            return View(callOutAddEdit);
        }

        private bool CanAccessCallout(CallOutAddEditViewModel callOutAddEdit)
        {
            return CanAccessProperty(new PropertyKey() { AgencyID = callOutAddEdit.AgencyID, PropertyID = callOutAddEdit.PropertyID });
        }

        private bool ShowSave(CallOutUserCapabilities userCapabilities, CallOutAddEditViewModel callOutAddEdit)
        {
            if (userCapabilities != null)
            {
                if (string.IsNullOrEmpty(userCapabilities.CallCenterSection) &&
                    string.IsNullOrEmpty(userCapabilities.DocumentSection) &&
                    string.IsNullOrEmpty(userCapabilities.MainSection) &&
                    string.IsNullOrEmpty(userCapabilities.ResponseTimeSection) &&
                    string.IsNullOrEmpty(userCapabilities.IRCMSection))
                {
                    return false;
                }
                else 
                {
                    if (callOutAddEdit.CapitalDefect 
                        && string.IsNullOrEmpty(userCapabilities.CapitalDefectSection) 
                        && (callOutAddEdit.CallLogStatusID == 4 || callOutAddEdit.CallLogStatusID == 5))
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        private CallOutUserCapabilities GetUserCapabilities(CallOutAddEditViewModel model)
        {
            string mainSection = string.Empty;
            string responseTimeSection = string.Empty;
            string callCenterSection = string.Empty;
            string ircmSection = string.Empty;
            string documentSection = string.Empty;
            model.FormReadOnly = (model.JobId != null || model.CallLogStatusID == 2 || model.CallLogStatusID == 7) ? true : false;

            CallOutUserCapabilities userCapability = new CallOutUserCapabilities();
            if (CurrentUser != null)
            {
                //if call id and job id exists
                if (model.CallOutId > 0 && (model.CallLogStatusID == 5 || model.CallLogStatusID == 8 || model.CallLogStatusID == 9 || 
                    model.Complete == true || model.CapitalDefectCalloutViewModel.Complete))
                {
                    if (model.CallLogStatusID == 5 || model.CallLogStatusID == 8 || model.CallLogStatusID == 9
                        || model.Complete == true || model.CapitalDefectCalloutViewModel.Complete == true)
                    {
                        //users cannot change it
                        mainSection = string.Empty;
                        responseTimeSection = string.Empty;
                        callCenterSection = string.Empty;
                        ircmSection = string.Empty;
                        documentSection = string.Empty;
                    }
                    //else if (model.Complete == true || model.CapitalDefectCalloutViewModel.Complete == true)
                    //{
                    //    mainSection = "CallLogStatusID";
                    //    responseTimeSection = string.Empty;
                    //    callCenterSection = "Complete";
                    //    documentSection = string.Empty;
                    //}
                }
                else if (model.CallOutId > 0)
                {
                    if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                        || CurrentUser.RoleID == "C"
                        || CurrentUser.RoleID == "F"
                        || CurrentUser.RoleID == "SM"
                        || CurrentUser.RoleID == "D"
                        || CurrentUser.RoleID == "GL"
                        || CurrentUser.RoleID == "SL"
                        || CurrentUser.RoleID == "AM"
                        || CurrentUser.RoleID == "EM"
                        || CurrentUser.RoleID == "P"
                        || CurrentUser.RoleID == "AO"
                        || CurrentUser.RoleID == "CS"
                        || CurrentUser.RoleID == "SP")
                    {
                        //Call out is assigned to the sub contractor
                        if (model.Complete)
                        {
                            if (CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM")
                            {
                                return new CallOutUserCapabilities() { MainSection = "hideControls" };
                            }
                            else
                            {
                                return userCapability;
                            }
                        }
                    }

                    //If admin, AMU, PWA or Call Center
                    if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                        || CurrentUser.RoleID == "D"
                        || CurrentUser.RoleID == "GL"
                        || CurrentUser.RoleID == "SL"
                        || CurrentUser.RoleID == "AM"
                        || CurrentUser.RoleID == "EM"
                        || CurrentUser.RoleID == "P"
                        || CurrentUser.RoleID == "AO"
                        || CurrentUser.RoleID == "C"
                        || CurrentUser.RoleID == "CS"
                        || CurrentUser.RoleID == "SP")
                    {
                        if (!model.Complete && (model.HasAtleastOneJob.HasValue && !model.HasAtleastOneJob.Value))
                        {
                            mainSection = "AssemblyGroupId,CallLogFinanceUrgencyID,CallLogStatusID,CallLogTypeID,CallStatusComments,CauseOfTheIssue,SecCauseOfTheIssue,ComponentCASId,ElementId,EventNo,SecEventNo,IncidentDate,IncidentLogTime,SecIncidentDate,SecIncidentLogTime,IsInsuranceEvent,IsSecurityIncident,IsThirdpartyInvolved,SecIsThirdPartyInvolved,JobBuildingIDs,LocalCotractorToRepairDamage,IsSecInsuranceEvent,LocalCotractorToSecurityIncident,SecLocalCotractorToRepairDamage,RoomLocation,RoomNumber,SchoolIncidentCauseId,SecSchoolIncidentCauseId,SchoolUrgencyID,SiteContact,SiteContactPerson,WorkTypeId,SchoolFunded";

                            if (model.ProposalId == 0 || model.ProposalId == null)
                                mainSection += ",CallLogDesc";
                            //if(CurrentUser.RoleID == "C" && model.CallLogStatusID == 1)
                            //    mainSection = mainSection.Replace("CallLogStatusID,", "");

                            if (model.CallLogStatusID != 2 && model.CallLogStatusID != 7)
                            {
                                //Calllog cancelled, cannot create work orders
                                //other statuses are handled above
                                callCenterSection = callCenterSection + ",SubContractorId,SubContractorBusinessTypeId,Complete,Comments";
                            }

                            if (!(CurrentUser.RoleID == "CS"
                                  || CurrentUser.RoleID == "AO"
                                  || CurrentUser.RoleID == "D"
                                  || CurrentUser.RoleID == "GL"
                                  || CurrentUser.RoleID == "SL"
                                  || CurrentUser.RoleID == "AM"
                                  || CurrentUser.RoleID == "EM"
                                  || CurrentUser.RoleID == "SP"
                                  || CurrentUser.RoleID == "P") || ((CurrentUser.RoleID == "AO"
                                  || CurrentUser.RoleID == "D") && AgencyId == 7061000)
                                  || (IsAMUUser(CurrentUser) && _agencyService.IsAMURegion(GetAgencyId()))) // AMU agencies do not have FMC role, all FMC role are now assigned to AMU
                            {
                                if (model.CallLogFITSID > 0)
                                {
                                    callCenterSection += ",CallCenter";
                                }
                            }

                            //responseTimeSection = "SelectedCallLogFinanceUrgenyId,SchoolUrgencyID,SelectedCallStatusId,JobComments";
                            documentSection = "btnUpload";
                        }

                        //At least one job is  available
                        if (model.HasAtleastOneJob.HasValue && model.HasAtleastOneJob.Value)
                        {
                            callCenterSection = "SubContractorId,SubContractorBusinessTypeId,Complete,Comments";
                            mainSection = "CallLogStatusID,EventNo,SecEventNo";
                            documentSection = "btnUpload";
                            if (!(CurrentUser.RoleID == "CS"
                                  || CurrentUser.RoleID == "AO"
                                  || CurrentUser.RoleID == "D"
                                  || CurrentUser.RoleID == "GL"
                                  || CurrentUser.RoleID == "SL"
                                  || CurrentUser.RoleID == "AM"
                                  || CurrentUser.RoleID == "EM"
                                  || CurrentUser.RoleID == "P")
                                  || (IsAMUUser(CurrentUser) && _agencyService.IsAMURegion(GetAgencyId()))) // AMU agencies do not have FMC role, all FMC role are now assigned to AMU
                            {
                                if (model.CallLogFITSID > 0)
                                    callCenterSection += ",CallCenter";
                            }
                            if (CurrentUser.RoleID == "SP")
                            {
                                callCenterSection = "";
                                mainSection = "";
                                documentSection = "";
                            }

                        }
                    }
                   
                    if (CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM")
                    {
                        //Callout Not completed and No Job Created Yes
                        if (!model.Complete && (model.HasAtleastOneJob.HasValue && !model.HasAtleastOneJob.Value))
                        {
                            if (model.RoleID == "C")
                                mainSection = "hideControls,RoomLocation,CallLogTypeID,IsInsuranceEvent,IsSecurityIncident,EventNo,SecEventNo,SchoolIncidentCauseId,SecSchoolIncidentCauseId,LocalCotractorToRepairDamage,IsSecInsuranceEvent,LocalCotractorToSecurityIncident,SecLocalCotractorToRepairDamage,IsThirdpartyInvolved,SecIsThirdPartyInvolved,CauseOfTheIssue,SecCauseOfTheIssue,RequestedBy,RequesterPosition,RequesterContact,SiteContactPerson,SiteContact";
                            else
                                mainSection = "hideControls,SchoolUrgencyID,JobBuildingIDs,Rooms,RoomNumber,RoomLocation,CallLogTypeID,SchoolUrgencyID,IsInsuranceEvent,IsSecurityIncident,EventNo,SecEventNo,SchoolIncidentCauseId,SecSchoolIncidentCauseId,LocalCotractorToRepairDamage,IsSecInsuranceEvent,LocalCotractorToSecurityIncident,SecLocalCotractorToRepairDamage,IsThirdpartyInvolved,SecIsThirdPartyInvolved,CauseOfTheIssue,SecCauseOfTheIssue,RequestedBy,RequesterPosition,RequesterContact,SiteContactPerson,SiteContact,SchoolFunded";
                            //responseTimeSection = ",SchoolUrgencyID,JobComments";
                            if (model.ProposalId == 0 || model.ProposalId == null)
                                mainSection += ",CallLogDesc";
                            documentSection = "btnUpload";
                        }
                        else
                        {
                            mainSection = "hideControls";
                        }
                    }

                    if (CanAddEditCapitalDefect(model))
                    {
                        mainSection = mainSection + ",CapitalDefect";
                    }
                }

                //Creating the Callout or Still a Job is not assigned
                if (model.CallOutId == null || model.CallOutId == 0)
                {
                    if (CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM")
                    {
                        mainSection = "hideControls,CallDate,CallLogTime,IncidentDate,IncidentLogTime,SecIncidentDate,SecIncidentLogTime,SchoolUrgencyID,JobBuildingIDs,RoomNumber,RoomLocation,CallLogTypeID,SchoolIncidentCauseId,SecSchoolIncidentCauseId,EventNo,SecEventNo,LocalCotractorToRepairDamage,IsSecInsuranceEvent,LocalCotractorToSecurityIncident,SecLocalCotractorToRepairDamage,SchoolUrgencyID,IsInsuranceEvent,IsSecurityIncident,IsThirdpartyInvolved,SecIsThirdPartyInvolved,CauseOfTheIssue,SecCauseOfTheIssue,RequestedBy,RequesterPosition,RequesterContact,SiteContactPerson,SiteContact,SchoolFunded";
                        //responseTimeSection = "JobComments";
                        if (model.ProposalId == 0 || model.ProposalId == null)
                            mainSection += ",CallLogDesc";
                    }

                    //Local contractor acceptance, Admin, AMU, Local Contractor and PWA can set it
                    if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                        || CurrentUser.RoleID == "C"
                        || CurrentUser.RoleID == "D"
                        || CurrentUser.RoleID == "GL"
                        || CurrentUser.RoleID == "SL"
                        || CurrentUser.RoleID == "AM"
                        || CurrentUser.RoleID == "EM"
                        || CurrentUser.RoleID == "P"
                        || CurrentUser.RoleID == "AO"
                        || CurrentUser.RoleID == "CS")
                    {
                        mainSection = "AssemblyGroupId,CallDate,CallLogFinanceUrgencyID,CallLogStatusID,CallLogTime,CallLogTypeID,CauseOfTheIssue,SecCauseOfTheIssue,Comments,ComponentCASId,ElementId,EventNo,SecEventNo,IncidentDate,IncidentLogTime,SecIncidentDate,SecIncidentLogTime,IsInsuranceEvent,IsSecurityIncident,IsThirdpartyInvolved,SecIsThirdPartyInvolved,JobBuildingIDs,LocalCotractorToRepairDamage,IsSecInsuranceEvent,LocalCotractorToSecurityIncident,LocalCotractorToSecurityIncident,SecLocalCotractorToRepairDamage,RequestedBy,RequesterContact,RequesterPosition,RoomLocation,RoomNumber,SchoolIncidentCauseId,SecSchoolIncidentCauseId,SchoolUrgencyID,SiteContact,SiteContactPerson,WorkTypeId,SchoolFunded";
                        if (model.ProposalId == 0 || model.ProposalId == null)
                            mainSection += ",CallLogDesc";
                        callCenterSection = "Complete,SubContractorBusinessTypeId,SubContractorId,Comments";
                        if (!(CurrentUser.RoleID == "CS" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P"))
                        {
                            if (model.CallLogFITSID > 0)
                                callCenterSection += ",CallCenter";
                        }
                    }

                    //If admin, AMU, PWA or Call Center
                    //if (CurrentUser.RoleID == "A" ||CurrentUser.RoleID == "AG" ||
                    // CurrentUser.RoleID == "D" ||  CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM" || CurrentUser.RoleID == "AO" ||  CurrentUser.RoleID == "CS")
                    //{
                    //    callCenterSection = callCenterSection + ",SelectedCallLogTypeId,SubContractorBusinessTypeId,SubContractorId";
                    //}

                    documentSection = "btnUpload";
                    if (CanAddEditCapitalDefect(model))
                    {
                        mainSection = mainSection + ",CapitalDefect";
                    }
                }

                //Can update for admin, MFU, MFU Officer
                if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                    || CurrentUser.RoleID == "E"
                    || CurrentUser.RoleID == "MO")
                {
                    mainSection = mainSection + ((string.IsNullOrEmpty(mainSection)) ? "":",") + "CallLogStatusID";
                    ircmSection = "ClaimNumber,CORPFileNo,AssessorStatusID,CommentsByIRCM";
                }

                //LRLV callout (from Proposal): always School Funded, none of the three options can be changed
                //Work Order created: none of the three options can be changed
                if (model.ProposalId > 0 || HasWorkOrder(model))
                {
                    //mainSection = mainSection.Replace(",SchoolFunded", "");
                    //mainSection = mainSection.Replace(",IsInsuranceEvent", "");
                    mainSection = RemoveCapabilities(mainSection, "IsInsuranceEvent", "IsSecurityIncident", "SchoolFunded");
                }

                //if (model.HasAtleaseOneClaim.HasValue &&
                //    model.HasAtleaseOneClaim.Value)
                //{

                //    mainSection = (model.IsInsuranceEvent.HasValue &&
                //                    model.IsInsuranceEvent.Value) && 
                //                    (!model.IsReadyForReimbursement.HasValue || 
                //                    model.IsReadyForReimbursement.HasValue && 
                //                    !model.IsReadyForReimbursement.Value) &&
                //                    (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                //                    || CurrentUser.RoleID == "D"
                //                    || CurrentUser.RoleID == "AO") ? mainSection + ",IsReadyForIRCMPayment" : mainSection;
                //}
            }

            userCapability.CapitalDefectSection = GetCapitalDefectUserCapabilities(model, ref mainSection, ref documentSection);
            userCapability.MainSection = mainSection;
            userCapability.ResponseTimeSection = responseTimeSection;
            userCapability.CallCenterSection = callCenterSection;
            userCapability.IRCMSection = ircmSection;
            userCapability.DocumentSection = documentSection;

            //logger.Debug(mainSection);
            return userCapability;
        }

        [HttpPost]
        [ValidateAntiForgeryTokenOnAllPosts]
        public ActionResult Detail(CallOutAddEditViewModel model)
        {
            string saveType = !string.IsNullOrEmpty(model.SaveType) ? model.SaveType.ToLower() : "";
            model.UserCapabilities = TempData["CALLOUT_USERCAPABILITIES"] != null ? (CallOutUserCapabilities)TempData["CALLOUT_USERCAPABILITIES"] : new CallOutUserCapabilities();

            string loggedInUser = CurrentUser.UserCode;
            CalloutSharedFunctions calloutSharedFunctions = new CalloutSharedFunctions(_calloutService, _agencyService, _buildingService, _propertyService);
            //if email ticked
            if (model.EmailToSchoolAndEmc)
            {
                model.EmailDate = (model.EmailDate == null ? DateTime.Now : model.EmailDate);
                model.LastEmailDate = DateTime.Now;
            }

            //Insurance Event / Security Incident / School Funded are mutually exclusive
            if (model.ProposalId > 0)
            {
                //LRLV callout is always School Funded
                model.SchoolFunded = true;
                model.IsInsuranceEvent = false;
                model.IsSecurityIncident = false;
            }
            else if (model.CapitalDefect)
            {
                //Capital Defect cannot be School Funded or a Security Incident
                model.SchoolFunded = false;
                model.IsSecurityIncident = false;
            }
            else if (!HasWorkOrder(model) && CountExclusiveOptions(model) > 1)
            {
                Toast.ShowError(TempData, "Only one of Insurance Event, Security Incident or School Funded can be selected.");
                if (model.CallLogFITSID > 0)
                {
                    return Redirect(Url.Action("Detail") + string.Format("?id={0}&sig={1}", model.CallLogFITSID, TamperHelper.GenerateSignature(model.CallLogFITSID)));
                }
                return RedirectToAction("Detail", new { propertyId = model.PropertyID, workCategoryId = model.WorkCategoryID });
            }
            
            if (model.ProposalId > 0 || model.SchoolFunded)
            {
                model.CallLogTypeID = 4;//Default to Proposal when callout created from proposal...
            } //if(model.IsInsuranceEvent.HasValue && model.IsInsuranceEvent.Value)
            //{
                //model.CallLogTypeID = 2;
            //}
            string userAlertTypes = string.Join(",", _calloutService.DetectAlertKeywords(model).OrderBy(x => x).ToList());
            bool alertTypesChanged = model.EmailAlertTypes != userAlertTypes;
            model.EmailAlertTypes = userAlertTypes;
            model.HAZMAT = !string.IsNullOrEmpty(model.EmailAlertTypes) ? "Yes" : "No";
            if (model.IncidentDate.HasValue && 
                (model.IsInsuranceEvent.HasValue && model.IsInsuranceEvent.Value ||
                model.IsInsuranceEventByWO.HasValue && model.IsInsuranceEventByWO.Value))
            {
                model.IncidentDate = (model.IncidentLogTime != null) ?
                                        model.IncidentDate.Value.AddHours(model.IncidentLogTime.Value.Hour).AddMinutes(model.IncidentLogTime.Value.Minute):
                                        model.IncidentDate;
            }

            if (model.SecIncidentDate.HasValue &&
               (model.IsSecurityIncident.HasValue && model.IsSecurityIncident.Value ))
               //||model.IsInsuranceEventByWO.HasValue && model.IsInsuranceEventByWO.Value))
            {
                model.SecIncidentDate = (model.SecIncidentLogTime != null) ?
                                        model.SecIncidentDate.Value.AddHours(model.SecIncidentLogTime.Value.Hour).AddMinutes(model.SecIncidentLogTime.Value.Minute) :
                                        model.SecIncidentDate;
            }

            if (!model.CallOutId.HasValue)
            {
                //Add New
                model.CallDate = model.CallDate.Value.AddHours(model.CallLogTime.Value.Hour).AddMinutes(model.CallLogTime.Value.Minute);
                //if(model.IncidentDate.HasValue)
                //    model.IncidentDate = model.IncidentDate.Value.AddHours(model.IncidentLogTime.Value.Hour).AddMinutes(model.IncidentLogTime.Value.Minute);
                var format = "dd/MM/yyyy";
                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };
                model.SupportDocuments = JsonConvert.DeserializeObject<List<SupportDocumentViewModel>>(model.SupportDocs, dateTimeConverter);
                
                bool status = _calloutService.Save(model, loggedInUser, out int callLogId);
                model.CallOutId = callLogId;

                if (model.CapitalDefect == false)
                {
                    calloutSharedFunctions.SendEmail(model, CurrentUser);
                }
                if (model.CapitalDefect == true)
                {
                    SaveCapitalDefect(model, calloutSharedFunctions);
                }
            }
            else
            {
                //Edit
                bool status = _calloutService.Update(model, CurrentUser);

                if (model.CapitalDefect == true || model.CapitalDefectCalloutViewModel.DefectFlaggedDate.HasValue)
                {
                    SaveCapitalDefect(model, calloutSharedFunctions);
                }
            }

            //Send Asbestos email only if the existing alert types are different
            if (alertTypesChanged)
            {
                calloutSharedFunctions.SendCalloutDiscriptionAlertEmail(model, CurrentUser, GetAgencyId());
            }
            
            if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                || CurrentUser.RoleID == "D"
                || CurrentUser.RoleID == "GL"
                || CurrentUser.RoleID == "SL"
                || CurrentUser.RoleID == "AM"
                || CurrentUser.RoleID == "EM"
                || CurrentUser.RoleID == "P"
                || CurrentUser.RoleID == "AO"
                || CurrentUser.RoleID == "C")
            {
                //Security Incident callouts cannot create Work Orders
                if (!string.IsNullOrEmpty(model.NewContractors) && model.IsSecurityIncident == true)
                {
                    Toast.ShowError(TempData, "A Work Order cannot be created for a Security Incident callout.");
                    model.NewContractors = null;
                }

                if (!string.IsNullOrEmpty(model.NewContractors))
                {
                    string[] newContractors = model.NewContractors.Split(',');

                    //Dictionary<string, string> dictionary = newContractors.Select(item => item.Split(':')).ToDictionary(s => s[0], s => s[1]);
                    if (newContractors != null && newContractors.Length > 0)
                    {
                        foreach (string row in newContractors)
                        {
                            string[] rowData = row.Split(':');
                            if (rowData[0] != null && rowData[0].Length > 0)
                            {
                                int contractorID = Convert.ToInt32(rowData[0]);
                                string comments = model.Comments;//rowData[1] != null ? rowData[1] : string.Empty; // we can get comments from CallOutAddEditViewModel Comments, not sure why it was appended to newContractors in calloutDetails.js -> saveWorkOrderData BUG 3655 fix: Error while create WO with comma in WO comment
                                int? workOrderId = _calloutService.ConvertCallOutToWorkOrder(model, contractorID, loggedInUser, comments);
                                model.WorkerId = workOrderId;
                                model.WorkOrderComments = comments;
                                calloutSharedFunctions.SendEmailToContractor(model, CurrentUser, contractorID);
                            }
                        }
                    }
                }
            }

            //_smsService.SendSMS("+13858326994", "460019773", "New work Order is created" + ( model.AgencySequenceID.HasValue ? model.AgencySequenceID.ToString() : "NA"));
            if (saveType == "saveandexit")
            {
                return model.CapitalDefect || (CurrentUser.RoleID == "PM" || CurrentUser.RoleID == "PD") ? RedirectToAction("DefectListIndex", "Defect") : RedirectToAction("Index");
            }
            else if (saveType == "save")
            {
                string queryString = string.Format("?id={0}&sig={1}", model.CallLogFITSID, TamperHelper.GenerateSignature(model.CallLogFITSID));
                return Redirect(Url.Action("Detail") + queryString);
            }
            else
            {
                return model.CapitalDefect ? RedirectToAction("DefectListIndex", "Defect") : RedirectToAction("Index");
            }
        }

        public ActionResult CalloutReport(int? calloutId)
        {
            if (calloutId.HasValue)
            {
                var estimationThreshold = CalloutSharedFunctions.GetTotalEstimationThresholdValue();
                CallOutAddEditViewModel model = _calloutService.GetCallOutDetails(calloutId.Value, -1, estimationThreshold);
                if (model != null)
                {
                    model.Contractor = _calloutService.GetAgencyContractor(model.AgencyID);

                    return View(model);
                }


            }
            return View(new CallOutAddEditViewModel());

        }

        public ActionResult ConsolidatedCallout(int? propertyId)
        {
            initCallOutMasterData(propertyId, null, null, null);
            FillInsuranceOptions();
            int agencyId = GetAgencyId();

            CallOutAddEditViewModel callOutAddEdit = _calloutService.GetAddCalloutViewModel(agencyId, propertyId.Value, 0, 0);
            CallOutInsAddModel model = new CallOutInsAddModel()
            {
                SchoolId = callOutAddEdit.SchoolId,
                SchoolName = callOutAddEdit.SchoolName,
                Phone = callOutAddEdit.Phone,
                SchoolLimit = callOutAddEdit.SchoolLimit,
                SchooLimitId = callOutAddEdit.SchoolLimitId,
                Area = callOutAddEdit.Area,
                RequestedBy = string.Format("{0} {1}", CurrentUser.FirstName, CurrentUser.LastName),
                InsCallTakenBy = CurrentUser.UserCode,
                RequestorTitle = CurrentUser.RoleID == "SM" ? "School Principal" : "School User",
                ContactFirstName = CurrentUser.FirstName,
                ContactLastName = CurrentUser.LastName,
                CalloutType = "fm",
                CallDate = DateTime.Now,
                CallLogTime = DateTime.Now,
                IncidentDate = DateTime.Now,
                IncidentLogTime = DateTime.Now,
                SelectedCallStatusId = 1,
                EmailToSchoolAndEmc = true,
                //Insurance
                InsCallDate = DateTime.Now,
                InsCallTime = DateTime.Now,
                IncidentTimeTo = DateTime.Now,
                IncidentTimeFrom = DateTime.Now,
                LoggerRoleDesc = CurrentUser.RoleDescNew,

            };



            //ModelState.Clear();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryTokenOnAllPosts]
        public ActionResult ConsolidatedCallout(CallOutInsAddModel model)
        {
            CalloutSharedFunctions calloutSharedFunctions = new CalloutSharedFunctions(_calloutService, _agencyService, _buildingService, _propertyService);
            int callOutId = 0;
            model.LoggedInUserId = CurrentUser.UserCode;
            model.LoggedInUserRoleId = CurrentUser.RoleID;
            model.AgencyId = GetAgencyId();
            if (model.CalloutType == "fm")
            {
                model.CallDate = model.CallDate.Value.AddHours(model.CallLogTime.Value.Hour).AddMinutes(model.CallLogTime.Value.Minute);
                model.IncidentDate = model.CallDate.Value.AddHours(model.IncidentLogTime.Value.Hour).AddMinutes(model.IncidentLogTime.Value.Minute);
                model.SelectedSchWorkCategoryId = model.WorkCategoryId;
                //Save Call out
                string loggedInUser = CurrentUser.UserCode;
                //if email ticked
                if (model.EmailToSchoolAndEmc)
                {
                    model.LastEmailDate = DateTime.Now;

                }


                //model.sc
                CallOutAddEditViewModel callOutModel = _mapper.Map<CallOutAddEditViewModel>(model);
                callOutModel.EmailDate = model.LastEmailDate;

                //TODO: Business not interested to go live after developing the below feature.
                //Commenting out for future use
                //calloutSharedFunctions.SetAMUEURBasedOnCallLogKeywords(callOutModel);
                bool status = _calloutService.Save(callOutModel, loggedInUser, out callOutId);
                //TODO: Business not interested to go live after developing the below feature.
                //Commenting out for future use
                //calloutSharedFunctions.SendAsbestosEmail(callOutModel, CurrentUser, GetAgencyId());
                calloutSharedFunctions.SendEmail(callOutModel, CurrentUser);
            }
            else if (model.CalloutType == "ins")
            {
                model.ConfirmedBySchool = User.IsInRoles("SCHOOL, SCHOOLMGR");
                if (!String.IsNullOrEmpty(model.LossItems))
                {
                    var losses = JsonConvert.DeserializeObject<List<LossDetails>>(model.LossItems);
                    model.SchoolIncidentLosses = losses;
                }

                bool status = _calloutService.SaveInsIncident(model, CurrentUser);
            }

            return RedirectToAction("Index", "SchoolHome", new { area = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenOnAllPosts]
        public JsonResult IsValidIncidentFromDate(CallOutInsAddModel details)
        {
            DateTime? incidentFromDateTime = details.IncidentFrom;
            DateTime? incidentTimeFrom = details.IncidentTimeFrom;
            if (incidentFromDateTime.HasValue)
            {

                incidentFromDateTime = details.IncidentTimeFrom.HasValue ? details.IncidentFrom.Value.AddHours(details.IncidentTimeFrom.Value.Hour)
                   .AddMinutes(details.IncidentTimeFrom.Value.Minute) : incidentFromDateTime;
                if (incidentFromDateTime > DateTime.Now)
                {
                    return Json("Incident From time cannot be in future");
                }
                //if (incidentFrom > DateTime.Now)
            }

            DateTime? incidentToDateTime = details.IncidentTo;
            DateTime? incidentTimeTo = details.IncidentTimeTo;
            if (incidentToDateTime.HasValue)
            {

                incidentToDateTime = details.IncidentTimeTo.HasValue ? details.IncidentTo.Value.AddHours(details.IncidentTimeTo.Value.Hour)
                   .AddMinutes(details.IncidentTimeTo.Value.Minute) : incidentToDateTime;
                if (incidentFromDateTime > incidentToDateTime)
                {
                    return Json("Incident From should be less than Incident To");
                }
                //if (incidentFrom > DateTime.Now)
            }


            return Json(true);

        }

        [HttpPost]
        [ValidateAntiForgeryTokenOnAllPosts]
        public JsonResult IsValidIncidentToDate(CallOutInsAddModel details)
        {

            DateTime? incidentToDateTime = details.IncidentTo;
            DateTime? incidentTimeTo = details.IncidentTimeTo;
            if (incidentToDateTime.HasValue)
            {

                incidentToDateTime = details.IncidentTimeTo.HasValue ? details.IncidentTo.Value.AddHours(details.IncidentTimeTo.Value.Hour)
                   .AddMinutes(details.IncidentTimeTo.Value.Minute) : incidentToDateTime;
                if (incidentToDateTime > DateTime.Now)
                {
                    return Json("Incident To time cannot be in future");
                }
            }


            return Json(true);

        }

        [HttpPost]
        public PartialViewResult FileNotFoundError()
        {
            return PartialView("_FileNotFoundError");
        }
        

        private int GetAgencyId()
        {
            var agencyId = 0;
            if (Session["AgencyDetails"] != null)
            {
                agencyId = ((KeyValuePair<int, string>)Session["AgencyDetails"]).Key;
            }
            if (agencyId == 0)
                Toast.ShowError(TempData, Constants.ToastErrorNoAgencySelected);
            return agencyId;
        }

        private bool IsAMUUser(UserViewModel user)
        {
            if (user.RoleID == "D" || user.RoleID == "P" ||
               user.RoleID == "CO" || user.RoleID == "AO")
            {
                return true;
            }

            return false;
        }

        private static bool HasWorkOrder(CallOutAddEditViewModel model)
        {
            return (model.HasAtleastOneJob.HasValue && model.HasAtleastOneJob.Value)
                || (model.JobId.HasValue && model.JobId.Value > 0);
        }

        private static int CountExclusiveOptions(CallOutAddEditViewModel model)
        {
            return (model.IsInsuranceEvent == true ? 1 : 0)
                + (model.IsSecurityIncident == true ? 1 : 0)
                + (model.SchoolFunded? 1 : 0);
        }

        //Removes exact field names from a comma separated capability list
        private static string RemoveCapabilities(string section, params string[] fields)
        {
            if (string.IsNullOrEmpty(section))
            {
                return section;
            }

            return string.Join(",", section.Split(',')
                .Where(f => !fields.Contains(f.Trim(), StringComparer.OrdinalIgnoreCase)));
        }

        private bool CheckCanEditFMCOnly(CallOutAddEditViewModel model)
        {
            bool canEditFMCOnly = false;

            if (CurrentUser != null && (CurrentUser.RoleID == "CS" || CurrentUser.RoleID == "C" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG") && (model.Complete == false) && (model.CallLogStatusID != 2) && (model.CallLogStatusID != 7))
            {
                canEditFMCOnly = true;
            }


            return canEditFMCOnly;
        }

        private void RemoveCancelledCallLogStatus()
        {

            List<SelectListItem> callStatus = (List<SelectListItem>)ViewData["CallLogStatus"];
            SelectListItem cancelledStatus = callStatus.Where(x => x.Value == "2").FirstOrDefault();
            if (cancelledStatus != null)
            {
                callStatus.Remove(cancelledStatus);
                ViewData["CallLogStatus"] = callStatus;
            }

        }
        private bool CheckCanDeleteDocument(CallOutAddEditViewModel model)
        {
            bool canDelete = false;
            //if (model.mode == "add")
            //{
            //    if (CurrentUser != null && (CurrentUser.RoleID == "C" || CurrentUser.RoleID == "A"|| CurrentUser.RoleID == "AG"))
            //    {
            //        canDelete = true;
            //    }
            //}

            if (model.mode == "edit")
            {
                if (CurrentUser != null && (CurrentUser.RoleID == "C"
                                            || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                                            || CurrentUser.RoleID == "D"
                                            || CurrentUser.RoleID == "GL"
                                            || CurrentUser.RoleID == "SL" 
                                            || CurrentUser.RoleID == "AM"
                                            || CurrentUser.RoleID == "EM"
                                            || CurrentUser.RoleID == "P"
                                            || CurrentUser.RoleID == "CO"
                                            || CurrentUser.RoleID == "PO"
                                            || CurrentUser.RoleID == "AO"
                                            || CurrentUser.RoleID == "F"
                                            || CurrentUser.RoleID == "SM"
                                            || CurrentUser.RoleID == "S") &&
                    (model.JobId == null || model.JobId <= 0) && (model.CallLogStatusID != 2) && (model.CallLogStatusID != 7))
                {
                    canDelete = true;
                }
            }

            return canDelete;
        }

        private bool CheckFMCReadOnly(CallOutAddEditViewModel model)
        {
            bool fmcReadOnly = true;
            if (model.mode == "add")
            {
                if (CurrentUser != null && (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                                            || CurrentUser.RoleID == "C"
                                            || CurrentUser.RoleID == "F"
                                            || CurrentUser.RoleID == "CS"
                                            || CurrentUser.RoleID == "SM"
                                            || CurrentUser.RoleID == "S"
                                            || CurrentUser.RoleID == "P"
                                            || CurrentUser.RoleID == "D"
                                            || CurrentUser.RoleID == "GL"
                                            || CurrentUser.RoleID == "SL" 
                                            || CurrentUser.RoleID == "AM"
                                            || CurrentUser.RoleID == "EM"
                                            || CurrentUser.RoleID == "AO"
                                            || CurrentUser.RoleID == "CO"))
                {
                    fmcReadOnly = false;
                }
            }

            if (model.mode == "edit")
            {
                if (CurrentUser != null &&
                    (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                     || CurrentUser.RoleID == "C"
                     || CurrentUser.RoleID == "F"
                     || CurrentUser.RoleID == "CS"
                     || CurrentUser.RoleID == "SM"
                     || CurrentUser.RoleID == "S"
                     || CurrentUser.RoleID == "P"
                     || CurrentUser.RoleID == "D"
                     || CurrentUser.RoleID == "GL"
                     || CurrentUser.RoleID == "SL" 
                     || CurrentUser.RoleID == "AM"
                     || CurrentUser.RoleID == "EM"
                     || CurrentUser.RoleID == "AO"
                     || CurrentUser.RoleID == "CO") &&
                     model.CallLogStatusID != 2 && (model.CallLogStatusID != 7))
                {
                    if (model.Complete == false)
                    {
                        fmcReadOnly = false;
                    }

                    if (model.Complete == true && model.JobCancelled == true)
                    {
                        fmcReadOnly = false;
                    }

                }
            }

            return fmcReadOnly;
        }

        private bool EnableAMU100(CallOutAddEditViewModel model)
        {
            bool enableAMU100 = false;
            if (CurrentUser != null &&
                (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                 || CurrentUser.RoleID == "C"
                 || CurrentUser.RoleID == "D"
                 || CurrentUser.RoleID == "GL"
                 || CurrentUser.RoleID == "SL" 
                 || CurrentUser.RoleID == "AM"
                 || CurrentUser.RoleID == "EM"
                 || CurrentUser.RoleID == "S"
                 || CurrentUser.RoleID == "AO"
                 || CurrentUser.RoleID == "CO")
                && model.Complete == false
                && (model.CallLogStatusID != 2 && model.CallLogStatusID != 7)
                && (model.JobId == 0 | model.JobId == null))
            {
                enableAMU100 = true;
            }


            return enableAMU100;
        }

        private bool EnablePWANotInvolved(CallOutAddEditViewModel model)
        {
            bool enablePWANotInvolved = false;
            if (CurrentUser != null &&
                (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AG"
                 || CurrentUser.RoleID == "D"
                 || CurrentUser.RoleID == "GL"
                 || CurrentUser.RoleID == "SL" 
                 || CurrentUser.RoleID == "AM"
                 || CurrentUser.RoleID == "EM"
                 || CurrentUser.RoleID == "S"
                 || CurrentUser.RoleID == "AO"
                 || CurrentUser.RoleID == "CO")
                && model.Complete == false
                && (model.CallLogStatusID != 2 && model.CallLogStatusID != 7)
                && (model.JobId == 0 | model.JobId == null))
            {
                enablePWANotInvolved = true;
            }


            return enablePWANotInvolved;
        }

        private void FillInsuranceOptions()
        {
            ViewData["WhoNoticedIncident"] = _ircmService.CurrentContext.SchoolAlarmNotificationRepository.Items()
                .Select(x => new SelectListItem() { Text = x.SchoolAlarmNotificationDesc, Value = x.SchoolAlarmNotificationID.ToString() }).OrderBy(a => a.Text).ToList();

            ViewData["TypeOfIncident"] = _ircmService.CurrentContext.SchoolIncidentCauseRepository.Items()
                 .Select(x => new SelectListItem() { Text = x.SchoolIncidentCauseDesc, Value = x.SchoolIncidentCauseID.ToString() }).OrderBy(a => a.Text).ToList(); ;
            ViewData["LossStatus"] = _ircmService.CurrentContext.SchoolIncidentLossStatusRepository.Items()
                 .Select(x => new SelectListItem() { Text = x.SchoolIncidentLossStatusDesc, Value = x.SchoolIncidentLossStatusID.ToString() }).OrderBy(a => a.Text).ToList(); ; ;
        }
        private void initCallOutMasterData(int? propertyId, int? workCategoryId, int? elementId, int? componentCASId, int? assemblyGroupId = null, bool isCaptitalDefect = false)
        {

            if (workCategoryId.HasValue)
            {
                int supportDocumentClass = _calloutService.GetSupportDocumentClassId(workCategoryId);

                //Load Document Type
                List<SelectListItem> supportDocumentType = workCategoryId.HasValue ? _calloutService.GetSupportDocumentTypes()
                                      .Where(d => d.SupportDocumentClassID == supportDocumentClass)
                                      .Select(x => new SelectListItem() { Text = x.SupportDocumentTypeDesc, Value = x.SupportDocumentTypeID.ToString() }).OrderBy(a => a.Text).ToList() :
                                      new List<SelectListItem>();
                ViewData["SupportDocumentType"] = supportDocumentType;
            }

            //ViewData["SubContractors"] = _calloutService.GetSubContractorsByAgency(GetAgencyId()).Where(c => (propertyId == 2036 && c.ID == 391) || (propertyId != 2036)).Select(x => new SelectListItem() { Text = x.Name, Value = x.ID.ToString() });
            //ViewData["SubContractors"] = _calloutService.GetSubContractorsByAgency(GetAgencyId()).OrderBy(x => x.Name).OrderByDescending(x => x.ATSIVerified).Select(x => new SelectListItem() { Text = x.Name, Value = x.ID.ToString() });


            ViewData["BuildingsList"] = propertyId.HasValue ? _buildingService.GetByProperty(propertyId.Value).Select(x => new BuildingViewModel
            {
                BuildingID = x.BuildingID,
                BuildingDesc = "(" + x.BuildingNo + ") " + x.BuildingDesc + ((x.RemovedYN.HasValue && x.RemovedYN.Value) ? " (REMOVED)" : "")
            }).ToList() : new List<BuildingViewModel>();
            //load callLogUrgency
            List<SelectListItem> callLogUrgency = _calloutService.GetFinanceUrgency()
                                    .Select(x => new SelectListItem() { Text = x.CallLogFinanceUrgencyDesc, Value = x.CallLogFinanceUrgencyID.ToString() }).ToList();
            ViewData["CallLogUrgency"] = callLogUrgency;

            //load Call Status
            LoadCallStatus(isCaptitalDefect);

            //load Call Type
            List<SelectListItem> callLogType = _calloutService.GetCallLogTypes()
                                    .Select(x => new SelectListItem() { Text = x.CallLogTypeDesc, Value = x.CallLogTypeID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["CallLogType"] = callLogType;

            //Load Equipement Type
            List<SelectListItem> assemblyEquipmentTypes = _calloutService.GetEquipmentTypeAssemblies()
                                  .Select(x => new SelectListItem() { Text = x.EquipmentTypeBSEMAssemblyDesc, Value = x.EquipmentTypeBSEMAssemblyID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["AssemblyEquipmentTypes"] = assemblyEquipmentTypes;

            //Load Maintenance CallLogs Type
            List<CustomDropdownItem> maintenanceCallLog = _calloutService.GetMaintenanceTasksCallLogs()
                .Select(x => new CustomDropdownItem { Text = x.MaintenanceTaskCallLogDesc, Value = x.MaintenanceTaskCallLogID.ToString(), Data = x.Type })
                .OrderBy(x => x.Text)
               .ToList();
            ViewData["MaintenanceCallLog"] = maintenanceCallLog;



            ViewData["Assembly"] = new List<SelectListItem>() { };
            if (elementId.HasValue)
            {
                ViewData["Assembly"] = _createScheduleService.GetAssemblyList(elementId.Value).Select(x => new SelectListItem() { Value = x.AssemblyGroupID.ToString(), Text = x.SubElementDesc }).ToList();

            }

            ViewData["SubAssembly"] = new List<SelectListItem>() { };
            if (componentCASId.HasValue && assemblyGroupId.HasValue)
            {
                ViewData["SubAssembly"] = _createScheduleService.GetSubAssemblyList(assemblyGroupId.Value).Select(x => new SelectListItem() { Value = x.ComponentCASID.ToString(), Text = x.ComponentDesc }).ToList(); ;

            }
            //Assessor Status for IRCM
            List<SelectListItem> assessorStatus = _calloutService.GetAssessorStatuses()
                        .OrderBy(x => x.AssessorStatusID)
                        .Select(x => new SelectListItem() { Text = x.AssessorStatusDesc, Value = x.AssessorStatusID.ToString() }).ToList();

            ViewData["AssessorStatus"] = assessorStatus;

        }

        private void LoadCallStatus(bool captitalDefect)
        {
            if (captitalDefect)
            {
                List<SelectListItem> defectStatus = _calloutService.GetCallLogStatuses().Where(x => x.DefectsYN).OrderBy(x => x.DefectsStatusSequence)
                                       .Select(x => new SelectListItem() { Text = x.CallLogStatusDesc, Value = x.CallLogStatusID.ToString() }).ToList();
                ViewData["CallLogStatus"] = defectStatus;
            }
            else
            {
                List<SelectListItem> callStatus = _calloutService.GetCallLogStatuses().Where(x => x.CalloutYN).OrderBy(x => x.CallLogStatusSequence)
                                       .Select(x => new SelectListItem() { Text = x.CallLogStatusDesc, Value = x.CallLogStatusID.ToString() }).ToList();
                ViewData["CallLogStatus"] = callStatus;
            }
        }

        #region Defect Management

        private CapitalDefectCalloutViewModel GetCapitalDefectDetail(CallOutAddEditViewModel callOutAddEdit, int? callLogFITSId, int agencyId)
        {
            // New callout
            if (callLogFITSId == null)
            {
                return new CapitalDefectCalloutViewModel();
            }

            // defect details 
            CapitalDefectCalloutViewModel model = _defectService.GetDefectCalloutHeaderDetails(callLogFITSId, agencyId, callOutAddEdit.PropertyID, 2, CurrentUser);
            if (model == null)
            {
                return new CapitalDefectCalloutViewModel();
            }

            model.CallLogStatusIDForValidation = callOutAddEdit.CallLogStatusID ?? 0;
            model.CapitalDefectCheckForValidation = callOutAddEdit.CapitalDefect;
            if (callOutAddEdit.CapitalDefect && model.WorkTypeId == null)
            {
                callOutAddEdit.WorkTypeId = 8; // Defect
            }

            return model;
        }

        private string GetCapitalDefectUserCapabilities(CallOutAddEditViewModel model, ref string mainSection, ref string documentSection)
        {
            var defectCallUserCapabilities = string.Empty;

            if(!model.DefectMgmt.HasValue || model.DefectMgmt.HasValue == false || model.CapitalDefectCalloutViewModel.Complete == true || model.Complete == true)
            {
                return defectCallUserCapabilities;
            }

            if (CanAddEditCapitalDefect(model))
            {
                defectCallUserCapabilities = "DefectEmailToSchoolAndPMPD,DefectFlaggedDate,DefectReversalDate,FirstEmailSentDate,LastEmailSentDate";
            }

            //Existing Defect
            if (model.CapitalDefectCalloutViewModel.PSMDefectCallLogId != 0)
            {
                if (UsersCanAddEditCapitalDefect() && model.CapitalDefect)
                {
                    defectCallUserCapabilities = "DefectEmailToSchoolAndPMPD,DefectFlaggedDate,DefectReversalDate,FirstEmailSentDate,LastEmailSentDate";
                }

                if (CurrentUser.RoleID == "PM" || CurrentUser.RoleID == "PD" || CurrentUser.RoleID == "A")
                {
                    if (model.CapitalDefect && !model.CapitalDefectCalloutViewModel.Complete)
                    {
                        defectCallUserCapabilities = "PSMWorkAssignmentStatusId,PMComments,Complete,DefectFlaggedDate,DefectReversalDate,FirstEmailSentDate,LastEmailSentDate";
                    }
                    else
                    {
                        defectCallUserCapabilities = "";
                    }
                }
            }

            if (CurrentUser.RoleID == "PD" || CurrentUser.RoleID == "PM")
            {
                if (!string.IsNullOrEmpty(defectCallUserCapabilities))
                {
                    mainSection = mainSection + ",CallLogStatusID";
                    documentSection = "btnUpload";
                }
            }

            if (CurrentUser.RoleID != "PD" && CurrentUser.RoleID != "PM" && model.CapitalDefect)
            {
                mainSection = mainSection.Replace("CallLogStatusID", "");
                mainSection = mainSection.Replace("WorkTypeId", "");
            }

            return defectCallUserCapabilities;
        }

        private void SaveCapitalDefect(CallOutAddEditViewModel model, CalloutSharedFunctions calloutSharedFunctions)
        {
            bool saveStatus = _defectService.SaveOrUpdateCapitalDefect(model, CurrentUser);
            if (model.CapitalDefectCalloutViewModel.DefectEmailToSchoolAndPMPD)
            {
                calloutSharedFunctions.SendDefectCreatedEmailToPM(model, CurrentUser);
            }
        }

        private bool CanAddEditCapitalDefect(CallOutAddEditViewModel model)
        {
            bool allowedUsers = UsersCanAddEditCapitalDefect();

            // Registered, Transferred - Defect, Technical Review Complete
            var allowedStatus = model.CallLogStatusID == 1 || model.CallLogStatusID == 10 || model.CallLogStatusID == 12;

            var hasWo = model.HasAtleastOneJob.HasValue && model.HasAtleastOneJob.Value;

            var isInsuranceEvent = model.IsInsuranceEvent.HasValue && model.IsInsuranceEvent.Value;

            var defectFlagged = model.CapitalDefect;

            var complete = model.Complete || model.CapitalDefectCalloutViewModel.Complete;

            var canEdit = allowedUsers && allowedStatus && !complete && !hasWo && !isInsuranceEvent && !defectFlagged;

            return canEdit;
        }

        private bool UsersCanAddEditCapitalDefect()
        {
            return CurrentUser.RoleID == "A"
                               || CurrentUser.RoleID == "C"
                               || CurrentUser.RoleID == "CO"
                               || CurrentUser.RoleID == "D"
                               || CurrentUser.RoleID == "AO"
                               || CurrentUser.RoleID == "P"
                               || CurrentUser.RoleID == "AM"
                               || CurrentUser.RoleID == "EM"
                               || CurrentUser.RoleID == "GL"
                               || CurrentUser.RoleID == "SL"
                               || CurrentUser.RoleID == "NP"
                               || CurrentUser.RoleID == "ND"
                               || CurrentUser.RoleID == "NO"
                               || CurrentUser.RoleID == "SO"
                               || CurrentUser.RoleID == "SP";
        }
        #endregion
    }
}