using AssetCare.Common;
using AssetCare.Controllers;
using AssetCare.Helpers;
using AssetCare.Model.Common;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Service.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;
using Constants = AssetCare.Common.Constants;

namespace AssetCare.Areas.FMWorks.Controllers
{
    [AuthorizeOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,SUPERREP,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
    public class ProposalsController : BaseController
    {
        private IPropertyService _propertyService;
        private IBuildingService _buildingService;
        private IUserService _userService;
        private IProposalService _proposalService;
        private ICreateScheduleService _createScheduleService;
        private readonly string cachingKey = "CreateScheduleReferenceData";


        public ProposalsController(ICreateScheduleService createScheduleService, IProposalService proposalService, IUserService userService,
            IPropertyService propertyService, IBuildingService buildingService, IBaseService baseService) : base(baseService)
        {
            _proposalService = proposalService;
            _baseService = baseService;
            _propertyService = propertyService;
            _buildingService = buildingService;
            _userService = userService;
            _createScheduleService = createScheduleService;
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
            List<SelectListItem> wcNew = _proposalService.GetWorkCategory().Select(x => new SelectListItem() { Text = String.Format("{0} - {1}", x.Code, x.Description), Value = x.WorkCategoryId.ToString() }).OrderBy(x => x.Text).ToList();
            ViewData["WorkCategoryList"] = wcNew;
            ViewData["Elements"] = _proposalService.GetElementList().Where(e => e.ElementID != 0).Select(x => new SelectListItem() { Value = x.ElementID.ToString(), Text = x.ElementDesc }).ToList();
            ViewData["WorkTypes"] = _proposalService.GetWorkTypes().Select(x => new SelectListItem() { Value = x.WorkTypeID.ToString(), Text = x.Description }).ToList();
            ViewData["WorkTypeList"] = _proposalService.GetWorkTypes();
            ViewData["ReadonlySchools"] = _propertyService.GetReadOnlySchools(agencyId);
            ViewData["UserRole"] = userDetails.RoleID;
            ViewData["FullName"] = userDetails.FullName;

        }
        [HttpPost]
        public PartialViewResult QuickHelp()
        {
            return PartialView("QuickHelp/_ProposalDetail");
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

        public ActionResult Index(int? schoolId, int? workCategoryId, int? elementId)
        {
            ProposalViewModel model = new ProposalViewModel();
            model.AgencyId = GetAgencyId();
            model.WorkCategoryId = workCategoryId;

            model.PropertyId = schoolId.HasValue ? schoolId : (User.IsSchoolUser() ? CurrentUser.DefaultPropertyId : null);
            // model.ElementId = elementId;
            model.CanDelete = CurrentUser.RoleID == "A" ? true : false;
            model.CanCreateJob = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "C") ? true : false;
            model.CanCreateCalout = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "C" || CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED" || CurrentUser.RoleID == "S" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "CO") ? true : false;
            model.CurrentRoleID = CurrentUser.RoleID;
            return View(model);
        }
        public ActionResult GrantApplication(int? schoolId, int? workCategoryId, int? elementId)
        {
            ProposalViewModel model = new ProposalViewModel();
            model.AgencyId = GetAgencyId();
            model.WorkCategoryId = workCategoryId;
            model.GrantApplication = true;
            model.PropertyId = schoolId.HasValue ? schoolId : (User.IsSchoolUser() ? CurrentUser.DefaultPropertyId : null);
            // model.ElementId = elementId;
            model.CanDelete = CurrentUser.RoleID == "A" ? true : false;
            model.CanCreateJob = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "C") ? true : false;
            model.CanCreateCalout = (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "C" || CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED" || CurrentUser.RoleID == "S" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "CO") ? true : false;
            model.CurrentRoleID = CurrentUser.RoleID;
            return View(model);
        }
        public ActionResult Detail(int? propertyId, int? workCategoryId, int? id, int? maintenanceCallLogId, int? elementId, string callbackUrl = "", string sig ="")
        {
            int agencyId = GetAgencyId();
            ProposalAddEditViewModel proposalAddEdit;
            int defaultBuilding = 0;
            if (propertyId.HasValue)
            {

                initProposalMasterData(propertyId, workCategoryId, agencyId, 0, out defaultBuilding);
                //get add model
                proposalAddEdit = _proposalService.GetAddProposalViewModel(agencyId, propertyId.Value, id);
                proposalAddEdit.SelectedMaintenanceCallLogId = maintenanceCallLogId;
                proposalAddEdit.RequestedBy = string.Format("{0} {1}", CurrentUser.FirstName, CurrentUser.LastName);
                proposalAddEdit.RequestorTitle = CurrentUser.JobTitle;
                proposalAddEdit.RequesterPosition = string.IsNullOrEmpty(CurrentUser.JobTitle) ? CurrentUser.RoleDescNew : CurrentUser.JobTitle;
                proposalAddEdit.RequesterContact = CurrentUser.Mobile ?? CurrentUser.Phone;

                proposalAddEdit.SelectedCallStatusId = 1; //set to logged status
                                                          //if(CurrentUser.RoleID=="F" || CurrentUser.RoleID == "SM")
                proposalAddEdit.PrincipalName = proposalAddEdit.PrincipalName?.Contains("ContactName") != true? proposalAddEdit.PrincipalName   // null or a real name — return as-is
                                                : "";
                proposalAddEdit.RoleID = CurrentUser.RoleID;
                proposalAddEdit.JobBuildingIDs = new List<int>() { defaultBuilding };
            }
            else
            {
                if (!TamperHelper.ValidateSignature(id.Value, sig))
                {
                    Toast.ShowError(TempData, Constants.NoAccessToRecordViaQuerystringModification);
                    return RedirectToAction("Index");
                }

                //get view model for existing
                proposalAddEdit = _proposalService.GetProposalDetails(id, agencyId);
                proposalAddEdit.FormReadOnly = (proposalAddEdit.JobId != null || proposalAddEdit.SelectedCallStatusId == 2) ? true : false;
                proposalAddEdit.RequestedBy = string.Format("{0} {1}", CurrentUser.FirstName, CurrentUser.LastName);
                proposalAddEdit.RequestorTitle = CurrentUser.JobTitle;
                proposalAddEdit.RequesterPosition = string.IsNullOrEmpty(CurrentUser.JobTitle) ? CurrentUser.RoleDescNew : CurrentUser.JobTitle;
                proposalAddEdit.RequesterContact = CurrentUser.Mobile ?? CurrentUser.Phone;
                List<ProposalEndorsementType> endorsementType = _proposalService.GetEndorsementTypes().Where(x => x.ViewType == "AMU").ToList();
                List<ProposalEndorsementType> schoolEndorsementType = _proposalService.GetEndorsementTypes().Where(x => x.ViewType == "School").ToList();
                proposalAddEdit.SupportDocument.SupportDocumentClassID = _proposalService.GetSupportDocumentClassId(1);//TODO:
                //reset download urls
                //foreach (var doc in proposalAddEdit.SupportDocuments)
                //{
                //    if (!String.IsNullOrEmpty(doc.AttachmentLink))
                //    {
                //        doc.AttachmentLink = Url.Content("~/") + doc.AttachmentLink.Remove(0, 1);
                //    }
                //}
                this.UseEncSupportDocumentViewModel("Proposal", AgencyId.Value, proposalAddEdit.SupportDocuments);

                //Endorsement
                if (proposalAddEdit.Endorsements == null || proposalAddEdit.Endorsements.Count == 0)
                {
                    proposalAddEdit.Endorsements = new List<EndorsementViewModel>();
                    for (int i = 0; i < endorsementType.Count; i++)
                    {
                        EndorsementViewModel model = new EndorsementViewModel();
                        model.EndorsementTypeID = endorsementType[i].ProposalEndorsementTypeID;
                        model.EndorsementName = endorsementType[i].EndorsementTypeName;
                        model.Limit = endorsementType[i].Limit;


                        proposalAddEdit.Endorsements.Add(model);
                    }
                }
                endorsementType = endorsementType
              .Where(x => !proposalAddEdit.Endorsements.Any(y => y.EndorsementTypeID == x.ProposalEndorsementTypeID)).ToList();

                for (int i = 0; i < endorsementType.Count; i++)
                {
                    EndorsementViewModel model = new EndorsementViewModel();
                    model.EndorsementTypeID = endorsementType[i].ProposalEndorsementTypeID;
                    model.EndorsementName = endorsementType[i].EndorsementTypeName;
                    model.Limit = endorsementType[i].Limit;
                    proposalAddEdit.Endorsements.Add(model);

                }
                proposalAddEdit.Endorsements = proposalAddEdit.Endorsements.OrderBy(x => x.EndorsementTypeID).ToList();

                //School Endorsement
                if (proposalAddEdit.SchoolEndorsements == null || proposalAddEdit.SchoolEndorsements.Count == 0)
                {
                    proposalAddEdit.SchoolEndorsements = new List<EndorsementViewModel>();
                    for (int i = 0; i < schoolEndorsementType.Count; i++)
                    {
                        EndorsementViewModel model = new EndorsementViewModel();
                        model.EndorsementTypeID = schoolEndorsementType[i].ProposalEndorsementTypeID;
                        model.EndorsementName = schoolEndorsementType[i].EndorsementTypeName;
                        model.Limit = schoolEndorsementType[i].Limit;


                        proposalAddEdit.SchoolEndorsements.Add(model);
                    }
                }
                schoolEndorsementType = schoolEndorsementType
             .Where(x => !proposalAddEdit.SchoolEndorsements.Any(y => y.EndorsementTypeID == x.ProposalEndorsementTypeID)).ToList();

                for (int i = 0; i < schoolEndorsementType.Count; i++)
                {
                    EndorsementViewModel model = new EndorsementViewModel();
                    model.EndorsementTypeID = schoolEndorsementType[i].ProposalEndorsementTypeID;
                    model.EndorsementName = schoolEndorsementType[i].EndorsementTypeName;
                    model.Limit = schoolEndorsementType[i].Limit;
                    proposalAddEdit.SchoolEndorsements.Add(model);
                }
                proposalAddEdit.SchoolEndorsements = proposalAddEdit.SchoolEndorsements.OrderBy(x => x.EndorsementTypeID).ToList();
                string decodedQueryString = HttpUtility.UrlDecode(Request.QueryString.ToString());
                if (decodedQueryString.IndexOf("callbackUrl=") > 0)
                {
                    var callbackUrls = decodedQueryString.Substring(decodedQueryString.ToLower().IndexOf("callbackurl=") + "callbackUrl=".Length);
                    proposalAddEdit.CallbackUrl = callbackUrls;
                }


                proposalAddEdit.mode = "edit";

                initProposalMasterData(proposalAddEdit.PropertyId, proposalAddEdit.WorkCategoryId, proposalAddEdit.AgencyId, proposalAddEdit.ProposalProgramID, out defaultBuilding);
            }

            if (!CanAccessCallout(proposalAddEdit))
            {
                Toast.ShowError(TempData, Constants.NoAccessToRecord);
                return RedirectToAction("Index");
            }
            if (!IsInCurrentRegion(proposalAddEdit.AgencyId))
            {
                Toast.ShowError(TempData, Constants.SwitchRegionFirst);
                return RedirectToAction("Index");
            }
            proposalAddEdit.UserType = CurrentUser.RoleID;

            proposalAddEdit.UserCapabilities = GetUserCapabilities(proposalAddEdit);
            ViewBag.Number = proposalAddEdit.ProposalWorkNominatedID;
            TempData["PROPOSAL_USERCAPABILITIES"] = proposalAddEdit.UserCapabilities;
            TempData["WorkNominatedID"] = proposalAddEdit.ProposalWorkNominatedID;
            proposalAddEdit.Approver = CurrentUser.FirstName + " " + CurrentUser.LastName;
            proposalAddEdit.Position = CurrentUser.RoleDescNew;
            proposalAddEdit.ApprovalDate = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy HH: mm:ss"));
            //TempData["ProposalRoles"] = _proposalService.GetProposalRoles(proposalAddEdit.AgencyId, proposalAddEdit.PropertyId);
            proposalAddEdit.CurrentRoleID = CurrentUser.RoleID;
            proposalAddEdit.FmcReadOnly = "SP".Equals(CurrentUser.RoleID);
            proposalAddEdit.SchoolFundedHidden = proposalAddEdit.SchoolFunded;
            //proposalAddEdit.PrincipalEmail = proposalAddEdit.SchoolDetails.Email;
            //if (!(CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED"))
            //{
            //    if (proposalAddEdit.WorkDescription == null)
            //        proposalAddEdit.WorkDescription = " ";
            //}
            //proposalAddEdit.SchoolSitePlanPDFPath = new SharedFunctions().GetSchoolSitePlanPDFPath(proposalAddEdit.SchoolId);
            //proposalAddEdit.AsbestosRegisterPDFPath = new SharedFunctions().GetAsbestosRegisterPDFPath(proposalAddEdit.SchoolId);
            proposalAddEdit.CurrentUserRoleID = CurrentUser.RoleID;
            return View(proposalAddEdit);
        }

        private bool CanAccessCallout(ProposalAddEditViewModel proposalAddEdit)
        {
            return CanAccessProperty(new PropertyKey() { AgencyID = proposalAddEdit.AgencyId, PropertyID = proposalAddEdit.PropertyId });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenOnAllPosts]
        public ActionResult Detail(ProposalAddEditViewModel model)
        {
            string saveType = !string.IsNullOrEmpty(model.SaveType) ? model.SaveType.ToLower() : "";
            int? workNominatedID = (int?)TempData["WorkNominatedID"];
            var conceptEstimateValue = model.ConceptEstimate == null ? model.ConceptEstimateDisabled : model.ConceptEstimate;
            if ((CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED" || CurrentUser.RoleID == "A") && (workNominatedID == 1 || workNominatedID == 2))
                model.ProposalWorkNominatedID = workNominatedID;
            if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM" || CurrentUser.RoleID == "GL")
            {
                //TODO
                //if (model.DeliveryTypeLRLV || model.DeliveryTypeLRLVHidden)
                //    model.ProposalProgramID = model.DefaultProgramForLRLV;
                //model.UserEmail = model.UserID != null ? _proposalService.GetPMASO().First(x => x.UserID == model.UserID).Email : string.Empty;
                if (model.UserID != null && !(model.SubmitDisabled || model.Submitted))
                    model.RoleID = "SO";
                if (CurrentUser.RoleID == "SO")//TODO:
                {
                    model.ReviewedBy = CurrentUser.FirstName + " " + CurrentUser.LastName;
                    model.RoleID = "";
                    model.ReviewDate = DateTime.Now;
                }
                if (model.RoleID != null && model.RoleID != "GL" && model.RoleID != "SL" && model.RoleID != "AM" && model.RoleID != "EM" && model.RoleID != "ED")
                {
                    if ((model.ProposalWorkNominatedID == 2 || model.ProposalWorkNominatedIDHidden == 2) && (model.Submitted || model.SubmitDisabled))
                    {
                        //var conceptEstimateValue = model.ConceptEstimate == null ? model.ConceptEstimateDisabled : model.ConceptEstimate;
                        if (model.Endorsements == null)
                            model.RoleID = "SM";
                        //if (conceptEstimateValue <= 165000 )
                        //    model.RoleID = "SM";
                        //else if (conceptEstimateValue > 165000 && conceptEstimateValue <= 5500000)
                        //    model.RoleID = "SD";
                        //else if (conceptEstimateValue > 5500000)
                        //    model.RoleID = "ED";
                    }
                    else if (model.ProposalWorkNominatedID != 2 && (model.Submitted || model.SubmitDisabled))
                    {
                        model.RoleID = "D";
                    }
                }
            }

            //to set fundsavailabilityold when concept estimate > fundsavailable
            var conceptEstimatevalue = model.ConceptEstimate == null ? model.ConceptEstimateDisabled : model.ConceptEstimate;
            if (model.ProposalId > 0 && (model.Submitted || model.SubmitDisabled) && model.FundsAvailabilityOld == null)
            {
                if (conceptEstimatevalue > model.FundsAvailability)
                {
                    model.FundsAvailabilityOld = model.FundsAvailability;
                    model.FundsAvailability = conceptEstimatevalue;
                }
            }

            //if (conceptEstimatevalue < 10000 || (model.DeliveryTypeLRLV==true || model.DeliveryTypeLRLVHidden==true))
            //{
            model.ProposalProgramID = model.ProposalProgramID == null ? model.ProgramSelectListHidden : model.ProposalProgramID;
            //}

            //
            model.SchoolFunded = model.SchoolFunded == false ? model.SchoolFundedHidden : model.SchoolFunded;

            //model.PrincipalEmail = model.SchoolEmail;
            //model.UserCapabilities = GetUserCapabilities(model);

            string loggedInUser = CurrentUser.UserCode;

            model.UserCapabilities = TempData["PROPOSAL_USERCAPABILITIES"] != null ? (ProposalUserCapabilities)TempData["PROPOSAL_USERCAPABILITIES"] : new ProposalUserCapabilities();
            model.Approver = CurrentUser.FirstName + " " + CurrentUser.LastName;
            model.Position = CurrentUser.RoleDescNew;
            model.Contact = CurrentUser.Mobile ?? CurrentUser.Phone;
            bool bEndorsement = true;
            bool bSchoolEndorsement = true;

            if (!model.CallOutId.HasValue)
            {
                int callLogId = 0;
                //Add New
                model.ProposalDate = model.ProposalDate.Value.AddHours(model.CallLogTime.Value.Hour).AddMinutes(model.CallLogTime.Value.Minute);
                var format = "dd/MM/yyyy";
                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };
                model.SupportDocuments = JsonConvert.DeserializeObject<List<SupportDocumentViewModel>>(model.SupportDocs, dateTimeConverter);
                bool status = _proposalService.Save(model, loggedInUser, out callLogId);
                model.CallOutId = callLogId;
                sendNewProposalEmail(model);
                if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND")
                {
                    //if (model.UserEmail != null && model.UserEmail.Length > 0) { }
                    if (model.UserID != null && !model.Submitted && !model.Reviewed)
                        sendPMASOEmail(model, "AO");
                }

                if (model.SchoolFundedHidden)
                    model.ProposalSFCID = 1;
                else
                    model.ProposalSFCID = 3;//Set value to 3 for school users only

            }
            else
            {
                List<EndorsementViewModel> endorsement = model.Endorsements != null ? model.Endorsements.Where(x => x.Approved != null).ToList() : null;
                if (endorsement != null && endorsement.Count > 0)
                {
                    if (endorsement[0].Approved == true)
                        StatusMessage(endorsement[0].EndorsementTypeID, model);
                    else
                        bEndorsement = false;
                }

                List<EndorsementViewModel> schoolEndorsement = model.SchoolEndorsements != null ? model.SchoolEndorsements.Where(x => x.Approved != null).ToList() : null;
                if (schoolEndorsement != null && schoolEndorsement.Count > 0)
                {
                    if (schoolEndorsement[0].Approved == true)
                        StatusMessage(schoolEndorsement[0].EndorsementTypeID, model);
                    else
                        bSchoolEndorsement = false;
                }
                if (!model.Reviewed)
                    model.Reviewed = model.ReviewedDisbaled == true ? true : false;
                //DeliveryType Radio button values
                model.DeliveryTypeAMU = model.DeliveryTypeAMUHidden;
                model.DeliveryTypeSSD = model.DeliveryTypeSSDHidden;
                model.DeliveryTypeLRLV = model.DeliveryTypeLRLVHidden;
               // model.DeliveryTypePlannedMaintenance = model.DeliveryTypePlannedMaintenanceHidden;

                // Prevent disabled entitlement checkboxes from overwriting existing values
                if (model.Entitlements != null && model.ProposalId > 0)
                {
                        var existingProposal = _proposalService.GetProposalDetails(model.ProposalId, model.AgencyId);
                        var existingEnt = existingProposal?.Entitlements;
                        if (existingEnt != null)
                        {
                            // copy back any entitlement fields that are disabled in the UI so they are not overwritten with defaults
                            var entViewModelType = model.Entitlements.GetType();
                            var existingType = existingEnt.GetType();
                            var props = entViewModelType.GetProperties();
                            foreach (var p in props)
                            {
                                // only consider simple boolean/nullable boolean properties
                                if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
                                {
                                    var name = p.Name;
                                    if (model.DisableField("entitlementsection", name))
                                    {
                                        var val = existingType.GetProperty(name)?.GetValue(existingEnt, null);
                                        p.SetValue(model.Entitlements, val, null);
                                    }
                                }
                            }
                        }
                    
                }

                bool status = _proposalService.Update(model, loggedInUser);
                if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM" || CurrentUser.RoleID == "GL")
                {
                    //if (model.UserEmail != null && model.UserEmail.Length > 0) { }
                    if (!model.Submitted && !model.Reviewed)
                    {
                        if (model.UserID != null && (model.UserIDPMASOHidden == null || model.UserIDPMASOHidden != model.UserID))
                            sendPMASOEmail(model, "AO");
                    }
                }
                if (CurrentUser.RoleID == "SO" && model.UserID != null)
                {
                    sendPMASOEmail(model, "SO");
                }

                if (model.AwaitingActionBy == "None" && model.ProposalSubmission == true)
                {
                    // Final Email after completing all steps
                    sendPMASOEmail(model, "EM");
                }

            }

            if (bSchoolEndorsement)
                sendSchoolEndorsementEmail(model);
            if (bEndorsement)
                sendEndorsementEmail(model);





            //if it is just save, then stay in the same pahe
            if (saveType == "save")
            {
                if (model.SelectedCallStatusId == 15)
                {
                    if (string.IsNullOrEmpty(model.CallbackUrl))
                    {
                        return RedirectToAction("GrantApplication");
                    }
                    else
                    {
                        if (!Url.IsLocalUrl(model.CallbackUrl))
                            return RedirectToAction("GrantApplication");
                        return Redirect(model.CallbackUrl);
                    }
                }
                else
                    return RedirectToAction("Detail", new { id = model.ProposalId, sig = model.Sig, callbackUrl = model.CallbackUrl });
            }
            else if (saveType == "saveandexit")
            {
                if (model.SelectedCallStatusId == 15)
                {
                    if (string.IsNullOrEmpty(model.CallbackUrl))
                    {
                        return RedirectToAction("GrantApplication");
                    }
                    else
                    {
                        if (!Url.IsLocalUrl(model.CallbackUrl))
                            return RedirectToAction("GrantApplication");
                        return Redirect(model.CallbackUrl);
                    }
                }
                if (string.IsNullOrEmpty(model.CallbackUrl))
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    if (!Url.IsLocalUrl(model.CallbackUrl))
                        return RedirectToAction("Index");
                    return Redirect(model.CallbackUrl);
                }

            }
            else
            {
                return RedirectToAction("Index");
            }


        }

        [HttpPost]
        public PartialViewResult FileNotFoundError()
        {
            return PartialView("_FileNotFoundError");
        }

        public void StatusMessage(int? endorsementID, ProposalAddEditViewModel model)
        {
            string statusMessage = string.Empty;
            var conceptEstimateGST = model.ConceptEstimateDisabled +
                                     model.ConceptEstimateDisabled * (decimal)(0.1);
            switch (endorsementID)
            {
                case 1:
                    if (conceptEstimateGST > 165000)
                    {
                        model.AwaitingActionBy = "Director School";
                        model.RoleID = "SD";
                    }
                    else
                    {
                        model.AwaitingActionBy = "Asset Services Officer";
                        model.RoleID = "D";
                    }
                    break;
                case 2:
                    if (conceptEstimateGST > 550000)
                    {
                        model.AwaitingActionBy = "Exec Director School";
                        model.RoleID = "ED";
                    }
                    else
                    {
                        model.AwaitingActionBy = "Asset Services Officer";
                        model.RoleID = "D";
                    }

                    break;
                case 3:
                    model.AwaitingActionBy = "Asset Services Officer";
                    model.RoleID = "D";
                    break;
                case 4:
                    model.AwaitingActionBy = "Group Leader";
                    model.RoleID = "GL";
                    break;
                case 5:
                    //if (conceptEstimateGST <= 165000)
                    //{
                    //    model.AwaitingActionBy = "Director AMU";
                    //    model.RoleID = "AM";
                    //}
                    //if (conceptEstimateGST > 165000)
                    //{
                    model.AwaitingActionBy = "Senior Group Leader";
                    model.RoleID = "SL";
                    //}
                    break;
                case 6:
                    model.AwaitingActionBy = "Director AMU";
                    model.RoleID = "AM";
                    break;
                case 7:
                    if (conceptEstimateGST > 550000)
                    {
                        model.AwaitingActionBy = "Exec Director Asset Management";
                        model.RoleID = "EM";
                    }
                    else
                    {
                        model.AwaitingActionBy = "None";
                        model.RoleID = "";
                    }
                    break;
                case 8:
                    model.AwaitingActionBy = "None";
                    model.RoleID = "";
                    break;

            }

        }

        private void sendNewProposalEmail(ProposalAddEditViewModel model)
        {
            List<SelectListItem> workNominated = _proposalService.GetWorkNominated().Where(x => x.WorkNominatedName == "School Funded Project" || x.WorkNominatedName == "School Principal")
                                 .Select(x => new SelectListItem() { Text = x.WorkNominatedName, Value = x.ProposalWorkNominatedID.ToString() }).OrderBy(a => a.Text).ToList();
            //ProposalRoles proposalRoles = (ProposalRoles)TempData["ProposalRoles"];
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            //get agency contract
            //AgencyContractor agencyContractor = _proposalService.GetAgencyContractor(model.AgencyId);
            if (model.ProposalWorkNominatedID == 1 || model.ProposalWorkNominatedID == 2)
                toEmail = model.SchoolEmail + ";";
            else
                toEmail += CurrentUser.Email + ";" + model.ASOEmail;// sinswEmail;// CurrentUser.Email + ";" + proposalRoles.ASO + ";" + proposalRoles.GroupLeader + ";" + proposalRoles.SeniorLeader;
            if (model.AwaitingActionBy != "None")
            {
                Dictionary<string, string> data = GetEmailData(model, "New", false);
                EmailHelper emailHelper = new EmailHelper();
                string subject = "New proposal created: Proposal ID - " + model.ProposalId;
                if (model.GrantApplication)
                    subject += " (Grant Application)";
                emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
            }
        }
        private void sendSchoolEndorsementEmail(ProposalAddEditViewModel model)
        {
            List<SelectListItem> workNominated = _proposalService.GetWorkNominated().Where(x => x.WorkNominatedName == "School Funded Project" || x.WorkNominatedName == "School Principal")
                                 .Select(x => new SelectListItem() { Text = x.WorkNominatedName, Value = x.ProposalWorkNominatedID.ToString() }).OrderBy(a => a.Text).ToList();
            //ProposalRoles proposalRoles = (ProposalRoles)TempData["ProposalRoles"];
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            //get agency contract
            AgencyContractor agencyContractor = _proposalService.GetAgencyContractor(model.AgencyId);
            int? schoolEndorsementTypeId = 0;
            List<EndorsementViewModel> endorsement = model.SchoolEndorsements != null ? model.SchoolEndorsements.Where(x => x.Approved != null).ToList() : null;
            if (endorsement != null && endorsement.Count > 0)
            {
                if (endorsement[0].Approved == true)
                    schoolEndorsementTypeId = endorsement == null ? 0 : endorsement.Select(x => x.EndorsementTypeID).FirstOrDefault();
            }
            bool approveGrantApplication = false;
            if (schoolEndorsementTypeId > 0)
            {
                switch (schoolEndorsementTypeId)
                {
                    case 1:
                        toEmail = model.DELEmail == null ? sinswEmail : model.DELEmail + ";"; //proposalRoles.SchoolDirector
                        //approveGrantApplication = true;
                        break;
                    case 2:
                        toEmail = model.EDEmail == null ? sinswEmail : model.EDEmail + ";";
                        break;
                    case 3:
                        toEmail = model.ASOEmail == null ? sinswEmail : model.ASOEmail + ";";// sinswEmail + ";"; //proposalRoles.ASO
                        break;
                    default:
                        break;
                }
                if (model.AwaitingActionBy != "None")
                {
                    Dictionary<string, string> data = GetEmailData(model, "", approveGrantApplication);
                    EmailHelper emailHelper = new EmailHelper();
                    string subject = "Proposal awaiting approval: Proposal ID - " + model.ProposalId;
                    emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
                }
            }
            else if (model.Submitted == true && model.AwaitingActionBy == null && (model.UserEmail != "1"))
            {
                approveGrantApplication = true;
                Dictionary<string, string> data = GetEmailData(model, "", approveGrantApplication);
                EmailHelper emailHelper = new EmailHelper();
                string subject = "";
                if(model.GrantApplication && model.RoleID=="SM")
                    subject = "Proposal awaiting approval: Proposal ID - " + model.ProposalId + " (Grant Application - Approved)";
                else
                    subject = "Proposal awaiting approval: Proposal ID - " + model.ProposalId;
                emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
            }
            //else if(model.Submitted == true && model.SelectedCallStatusId == 15 && schoolEndorsementTypeId==0)
            //{
            //    Dictionary<string, string> data = GetEmailData(model, "", approveGrantApplication);
            //    EmailHelper emailHelper = new EmailHelper();
            //    string subject = "";
            //    if ((model.GrantApplication || model.GrantApplicationHidden) && model.RoleID == "SM")
            //        subject = "Proposal has been Archived: Proposal ID - " + model.ProposalId + " (Grant Application)";
            //    emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
            //}
            else if (model.SelectedCallStatusId == 15 && schoolEndorsementTypeId == 0 && (model.GrantApplication || model.GrantApplicationHidden) && (model.CurrentRoleID == "AO" || model.CurrentRoleID == "D"))
            {
                Dictionary<string, string> data = GetEmailData(model, "", approveGrantApplication);
                EmailHelper emailHelper = new EmailHelper();
                string subject = "";
                if ((model.GrantApplication || model.GrantApplicationHidden) && (model.CurrentRoleID == "AO" || model.CurrentRoleID == "D"))
                {
                    toEmail = model.PrincipalEmail == null ? sinswEmail : model.PrincipalEmail + ";";
                    subject = "Proposal has been Archived: Proposal ID - " + model.ProposalId + " (Grant Application - Not Approved)";
                }
                emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
            }

        }
        private void sendEndorsementEmail(ProposalAddEditViewModel model)
        {
            List<SelectListItem> workNominated = _proposalService.GetWorkNominated().Where(x => x.WorkNominatedName == "School Funded Project" || x.WorkNominatedName == "School Principal")
                                 .Select(x => new SelectListItem() { Text = x.WorkNominatedName, Value = x.ProposalWorkNominatedID.ToString() }).OrderBy(a => a.Text).ToList();
            //ProposalRoles proposalRoles = (ProposalRoles)TempData["ProposalRoles"];
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            //get agency contract
            AgencyContractor agencyContractor = _proposalService.GetAgencyContractor(model.AgencyId);
            int? schoolEndorsementTypeId = 0;
            List<EndorsementViewModel> endorsement = model.Endorsements != null ? model.Endorsements.Where(x => x.Approved != null).ToList() : null;
            if (endorsement != null && endorsement.Count > 0)
            {
                if (endorsement[0].Approved == true)
                    schoolEndorsementTypeId = endorsement == null ? 0 : endorsement.Select(x => x.EndorsementTypeID).FirstOrDefault();
            }
            if (schoolEndorsementTypeId > 0)
            {
                //TODO: this will come from ProposalRoles table, once we have list of emails...

                switch (schoolEndorsementTypeId)
                {
                    case 4:
                        toEmail = model.GLEmail == null ? sinswEmail : model.GLEmail + ";";
                        break;
                    case 5:
                        toEmail = model.SGLEmail == null ? sinswEmail : model.SGLEmail + ";";
                        break;
                    case 6:
                        toEmail = model.DAMEmail == null ? sinswEmail : model.DAMEmail + ";";
                        break;
                    case 7:
                        toEmail = model.EDAMEmail == null ? sinswEmail : model.EDAMEmail + ";";
                        break;
                    //case 8:
                    //    toEmail = model.DAMEmail == null ? sinswEmail : model.DAMEmail + ";";  // When all aprrovals done, email goes to DAM for final submission
                    //    break;
                    default:
                        break;
                }
                if ((model.DeliveryTypeLRLV != true && model.DeliveryTypeLRLVHidden != true) || schoolEndorsementTypeId == 5)
                {
                    if (model.AwaitingActionBy != "None")
                    {
                        Dictionary<string, string> data = GetEmailData(model, "", false);
                        EmailHelper emailHelper = new EmailHelper();
                        string subject = "Proposal awaiting approval: Proposal ID - " + model.ProposalId;
                        emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
                    }
                    else if (model.AwaitingActionBy == "None")
                    {
                        toEmail = model.DAMEmail == null ? sinswEmail : model.DAMEmail + ";";  // When all aprrovals done, email goes to DAM for final submission
                        Dictionary<string, string> data = GetEmailData(model, "", false);
                        EmailHelper emailHelper = new EmailHelper();
                        string subject = "Proposal awaiting for final submission: Proposal ID - " + model.ProposalId;
                        emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html", cc: sinswEmail);
                    }
                }
            }
        }

        private void sendPMASOEmail(ProposalAddEditViewModel model, string user)
        {
            List<SelectListItem> workNominated = _proposalService.GetWorkNominated().Where(x => x.WorkNominatedName == "School Funded Project" || x.WorkNominatedName == "School Principal")
                                 .Select(x => new SelectListItem() { Text = x.WorkNominatedName, Value = x.ProposalWorkNominatedID.ToString() }).OrderBy(a => a.Text).ToList();

            var filteredData = workNominated.SingleOrDefault(x => x.Value == model.ProposalWorkNominatedID.ToString());
            string toEmail = null;
            if (model.UserID != null)
            {
                toEmail = _proposalService.GetRegionalContact()
                               .Where(x => x.Id == model.UserID) //Userid is Id from RegionalContact
                               .FirstOrDefault().Email;
            }
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();

            toEmail = toEmail == null ? sinswEmail : toEmail + ";" + sinswEmail;//remove sinswEmail after testing
            Dictionary<string, string> data = GetEmailData(model, "SO", false);
            EmailHelper emailHelper = new EmailHelper();
            if (model.AwaitingActionBy != "None")
            {
                string subject = string.Empty;
                if (user == "AO" || user == "P" || user == "D" || user == "GL" || user == "SL" || user == "AM" || user == "EM")
                    subject = "New Proposal has been assigned for review - Proposal ID: " + model.ProposalId;
                else if (user == "SO")
                    subject = "New Proposal has been Reviewed by PM ASO - Proposal ID: " + model.ProposalId;
                else
                    subject = "Proposal has been Approved - Proposal ID: " + model.ProposalId;
                if (!model.ReviewedDisbaled)
                    emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html");
            }
            else if (model.AwaitingActionBy == "None")
            {
                string subject = "Proposal has been Approved - Proposal ID: " + model.ProposalId;
                emailHelper.SendMail(toEmail, data, subject, "ProposalEmailContent.html");
            }
        }

        private Dictionary<string, string> GetEmailData(ProposalAddEditViewModel model, string user, bool approveGrantApplication)
        {
            var callLogUrgency = _proposalService.GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID == model.SelectedCallLogFinanceUrgenyId).FirstOrDefault();
            Dictionary<string, string> data = new Dictionary<string, string>();
            string callOutId = model.AgencySequenceId.HasValue ? model.AgencySequenceId.ToString() : "NA";
            data.Add("ProposalId", Convert.ToString(model.ProposalId));
            data.Add("AgencyId", model.AgencyId.ToString());
            data.Add("RequestedBy", CurrentUser.FullName);
            data.Add("SchoolName", model.SchoolName);
            data.Add("Phone", model.Phone);
            data.Add("GrantApplication", (model.GrantApplication || model.GrantApplicationHidden) == true ? "True" : "");
            //data.Add("NotificationResult", "False");
            string notificationGA = "";
            string approvedGrantApplication = @"This email is to inform you that your 'Grant Application' proposal has been reviewed and approved by the Asset Management Unit (AMU).
                                                Please print off a copy of the project proposal approval from FMWeb 2.0 and attach with your application as evidence of landholder consent.
                                                <br \>Please contact your ASO to discuss the next steps in the process and to provide any additional support you may need.";
            string rejectedGrantApplication = @"This email is to inform you that your 'Grant Application' proposal has been reviewed and is not approved by the Asset Management Unit (AMU).<br \>
                                                Please print off a copy of the project proposal approval from FMWeb 2.0 and attach with your application as evidence of landholder consent.<br \>
                                                <br \>Please contact your ASO to discuss the next steps in the process and to provide any additional support you may need.";
            if (model.SelectedCallStatusId == 15 && (model.GrantApplication || model.GrantApplicationHidden) && !approveGrantApplication)
            {
                notificationGA = rejectedGrantApplication;
                //data.Add("NotificationCondition", "True");
            }
            else if (approveGrantApplication && model.SelectedCallStatusId != 15 && (model.GrantApplication || model.GrantApplicationHidden))
            {
                notificationGA = approvedGrantApplication;
                //data.Add("NotificationCondition", "True");
            }
            data.Add("NotificationGrantApplication", notificationGA);

            if (model.AwaitingActionBy == null)
            {
                if (user == "SO" && model.UserID != null && !model.Reviewed)
                {
                    data.Add("Notification", "Below notification is awaiting PM ASO's approval:");
                }
                else
                {
                    if ((CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "SO") && model.SelectedCallStatusId != 15)
                        data.Add("Notification", "Below notification is awaiting Asset Services Officer's approval:");
                    else
                    {
                        if (user == "New")
                            data.Add("Notification", " ");
                        else if (model.ProposalWorkNominatedID == 2)
                        {
                            if (model.GrantApplication || model.GrantApplicationHidden)
                                if (model.SelectedCallStatusId != 15)
                                    data.Add("Notification", "Grant Application Proposal awaiting Premier's Office confirmation:");
                                else
                                    data.Add("Notification", " ");
                            else
                                data.Add("Notification", "Below notification is awaiting  School Principal's approval:");
                        }
                        else
                            data.Add("Notification", "Below notification is awaiting  Asset Services Officer's approval:");
                    }
                }

            }
            else
            {
                if (model.AwaitingActionBy != "None")
                    data.Add("Notification", "Below notification is awaiting " + model.AwaitingActionBy + "'s approval:");
                else if (model.AwaitingActionBy != "None" && model.RoleID.Length == 0)
                    data.Add("Notification", "Below notification is awaiting submission:");
                else
                    data.Add("Notification", string.Empty);
            }

            //get agency contract
            AgencyContractor agencyContractor = _proposalService.GetAgencyContractor(model.AgencyId);
            if (agencyContractor != null)
            {
                data.Add("AgencyEmail", agencyContractor.Email);
            }

            if ((System.Web.HttpContext.Current.Session["AgencyDetails"]) != null)
            {
                data.Add("Region", ((KeyValuePair<int, string>)Session["AgencyDetails"]).Value);
            }
            //get buildings list
            List<string> buildingList = new List<string>();
            if (model.JobBuildingIDs != null)
            {
                foreach (int buildingId in model.JobBuildingIDs)
                {

                    var b = _buildingService.GetByProperty(model.PropertyId)
                        .Where(x => x.BuildingID == buildingId)
                        .FirstOrDefault();
                    if (b != null)
                    {
                        buildingList.Add("(" + b.BuildingNo + ") " + b.BuildingDesc);
                    }

                }
            }

            data.Add("BuildingList", String.Join(",", buildingList));
            data.Add("Rooms", model.Rooms);
            data.Add("RoomLocation", model.RoomLocation);
            data.Add("Area", model.Area);
            data.Add("PrincipalName", model.PrincipalName);

            data.Add("SchoolEmail", model.SchoolEmail);

            //string workCategory = _proposalService.GetWorkCategory()
            //    .Where(x => x.WorkCategoryId == model.WorkCategoryId)
            //    .FirstOrDefault().Description;

            data.Add("WorkCategoryDesc", model.WorkDescription);

            data.Add("ProposalDate", model.ProposalDate.Value.ToString("dd-MM-yyyy"));
            data.Add("ProposalTime", model.ProposalDate.Value.ToString("HH:mm tt"));
            data.Add("ScopeOfWork", model.JobComments);
            data.Add("JobComments", model.ProjectDescription);
            data.Add("FundsAvailable", Convert.ToString(model.FundsAvailability == null ? model.FundsAvailabilityDisabled : model.FundsAvailability));
            data.Add("ConceptEstimate", Convert.ToString(model.ConceptEstimate == null ? model.ConceptEstimateDisabled : model.ConceptEstimate));
            if (callLogUrgency != null)
            {
                data.Add("CallResponseTime", callLogUrgency.CallLogFinanceUrgencyDesc);
            }

            var request = ControllerContext.RequestContext.HttpContext.Request;

            data.Add("Link", string.Format("{0}://{1}{2}?id={3}", request.Url.Scheme, request.Url.Authority, request.ApplicationPath + "/CallOut/Detail", model.ProposalId));
            return data;
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

        private ProposalUserCapabilities GetUserCapabilities(ProposalAddEditViewModel details)
        {
            if (details.Submitted)
                details.SubmitDisabled = true;
            //SerializableDictionary<string, string> editableFields = new SerializableDictionary<string, string>();
            string schoolSection = string.Empty;
            string detailSection = string.Empty;
            string schoolEndorsement = string.Empty;
            string endorsement = string.Empty;
            string entitlementSection = string.Empty;
            string claimSection = string.Empty;
            string documentSection = string.Empty;
            ProposalUserCapabilities userCapability = new ProposalUserCapabilities();
            if (CurrentUser != null)
            {
                if (CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED")
                {
                    details.showHideSection = true;
                    details.cssClassSE = "card-body collapse show";
                    details.cssClassE = "card-body collapse";
                }
                else
                {
                    if (CurrentUser.RoleID == "SO")
                    {
                        details.showHideReviewSection = true;
                        if (!details.Reviewed)
                            userCapability.DetailSection = "reviewed,";
                    }
                    else
                    {
                        if (!details.Submitted && (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "A"))
                            userCapability.ClaimSection = "submitted,SchoolSelfDelivery";//,hazmat";
                        details.showHideReviewSection = false;
                    }

                    details.showHideSection = false;
                    details.cssClassSE = "card-body collapse";
                    details.cssClassE = "card-body collapse show";

                }
                if (CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM")
                    details.showHideGLSLSection = true;
                else
                    details.showHideGLSLSection = false;

                if (CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED")
                    details.showHideSDEDSection = true;
                else
                    details.showHideSDEDSection = false;


                if (!details.Submitted && (CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM"))
                {
                    userCapability.DetailSection = "pmaso,";
                    //return userCapability;
                }

                if (!details.Submitted)
                {
                    userCapability.SchoolSection = "PrincipalName,SchoolEmail,ProposalStatus,WorkDescription";
                    if (CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "F" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D")
                        userCapability.DetailSection += "GrantApplication,";
                }
                else
                {
                    bool grantApplication = details.SchoolEndorsements.Where(x => x.EndorsementTypeID == 1).FirstOrDefault().Approved ?? false;
                    if (!grantApplication && details.SelectedCallStatusId != 15 && (details.GrantApplication || details.GrantApplicationHidden))//Archived
                    {
                        
                        if (CurrentUser.RoleID == "SM") //only availabke for school principal
                            userCapability.SchoolSection = "ProposalStatus";
                        if (CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED")
                            userCapability.DetailSection += "btnUpload";
                    }
                    
                    if (details.ProposalSubmissionDisabled != true && details.AwaitingActionBy == "None")
                    {
                        if (((CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "A") &&
                            (details.ProgramName == "School Recurrent Projects" || details.ProgramName == "School Capital Projects"
                            || details.ProgramName == "Community Funded Works - Recurrent"
                            || details.ProgramName == "Community Funded Works - Capital"))
                            && !(details.SchoolSelfDelivery)
                            && (details.SelectedCallStatusId!=3)
                            )
                        {
                            userCapability.ClaimSection += ",proposalsubmission";
                        }


                    }
                    // Enable file upload for all users before final proposal submission
                    if (details.ProposalSubmissionDisabled != true && details.CallLogFITSID == null)
                    {
                        userCapability.DetailSection += "btnUpload,";
                        details.ShowDocumentDeleteBtn = false;
                        if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D")
                        {
                            userCapability.SchoolSection += ",ProposalStatus";
                        }
                    }

                    //if ((CurrentUser.RoleID == "A" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "D") && details.ProposalSubmissionDisabled != true && details.CallLogFITSID == null && (details.GrantApplication || details.GrantApplicationHidden))
                    //{ userCapability.DetailSection += "btnUpload,"; }


                    if (details.AwaitingActionBy == null)
                    {
                        userCapability.DetailSection += "FundsAvailability";
                    }


                    return userCapability;
                    //if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM")

                    // userCapability.SchoolSection = "PrincipalName,SchoolEmail,ProposalStatus,WorkDescription";
                }

            }
            if (!details.Submitted)
            {
                if (CurrentUser.RoleID == "SM" || CurrentUser.RoleID == "F" || CurrentUser.RoleID == "SD" || CurrentUser.RoleID == "ED")
                {
                    userCapability.DetailSection += "RoomNoSchool,RoomLocationSchool,BuildingAffectedSchool,Schoolfundcode,schoolFunded,communityFunded,FundsAvailability,btnUpload,amuFunded,SchoolSelfDelivery,plannedMaintenance";
                    userCapability.SchoolSection += ",WorkDescriptionSchool";
                    //if (details.ProposalWorkNominatedID == 2)
                    //    //userCapability.DetailSection += ",FundsAvailability";
                    return userCapability;
                }
                if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM")
                {
                    userCapability.DetailSection += "Estimate,Program,SubProgram,ProjectType,ProjectDescription,Functionality,WorkNominated,btnUpload,ProposalProcurement,RoomNo,RoomLocation,BuildingAffected,Schoolfundcode,FundsAvailability,schoolFunded,communityFunded,amuFunded,SchoolSelfDelivery,plannedMaintenance";
                    //if (!(details.SchoolEndorsements.Any(x => x.Approved == true) || details.Endorsements.Any(x => x.Approved == true)))
                    userCapability.DetailSection += ",ConceptEstimate,jointfunding";
                    if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM" || CurrentUser.RoleID == "GL")
                    {
                        if (!details.Reviewed && !details.Submitted)
                            userCapability.DetailSection += ",pmaso";
                        if (!details.Reviewed && CurrentUser.RoleID == "SO")
                            userCapability.DetailSection += ",reviewed";
                    }
                    if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "P" || CurrentUser.RoleID == "SO" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "ND" || CurrentUser.RoleID == "SL" || CurrentUser.RoleID == "GL" || CurrentUser.RoleID == "AM" || CurrentUser.RoleID == "EM") { }
                    else
                    {
                        userCapability.DetailSection += ",IsSchoolFunded"; //Enable Admin to select School Funded Y/N
                                                                           //if (details.ProposalWorkNominatedID == 2)
                                                                           // userCapability.DetailSection += ",FundsAvailability";
                    }
                    userCapability.EntitlementSection = "PMMProfile,REF,DA,Exempt,WithConsent,WithoutConsent,CDC,Arboist,Bushfire,Contamination,FloodProne,Heritage,Ecology,Archaeology,EPNone,SSU,ITD,Demountable,AssetActivation,SCNone,PRNone,ServiceIntereption,ExamsInOperation,VacationPeriod,Relocation,OCNone,Asbestos,Asbestos,Mould,HMNone,Electrical,ElectricalYes,ElectricalNo,Hydraulics,Sewer,Mechanical,Stormwater,IT,Security,SNone,Lead,LearningWellbeing,WHS,StatPlanning,TechHeritage,Compliance,Sustainability,Comms,Leaseslicences,CentralServices,TSNone";
                    //if (!(details.SchoolEndorsements.Any(x => x.Approved == false || x.Approved == null)))
                    //    userCapability.SchoolSection = string.Empty;
                    if (CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D")
                    {
                        userCapability.ClaimSection += ",Hazmat";
                    }
                    if (details.ProposalId > 0)
                        userCapability.DetailSection += ",btnUpload";
                    return userCapability;
                }

            }


            userCapability.SchoolSection = schoolSection;
            userCapability.DetailSection = detailSection;
            userCapability.SchoolEndorsement = schoolEndorsement;
            userCapability.Endorsement = endorsement;
            userCapability.EntitlementSection = entitlementSection;
            userCapability.ClaimSection = claimSection;



            return userCapability;
        }
        public ActionResult ProposalReport(int? proposalId, int? agencyId, int? propertyId)
        {

            //initProposalMasterData(propertyId, 1,agencyId);
            //var pp = ViewData["ProposalProgram"];

            if (proposalId.HasValue)
            {
                ProposalAddEditViewModel model = _proposalService.GetProposalDetails(proposalId.Value, -1);
                List<string> buildingList = new List<string>();

                //Building List
                if (model.JobBuildingIDs != null)
                {
                    foreach (int buildingId in model.JobBuildingIDs)
                    {

                        var b = _buildingService.GetByProperty(model.PropertyId)
                            .Where(x => x.BuildingID == buildingId)
                            .FirstOrDefault();
                        if (b != null)
                        {
                            buildingList.Add("(" + b.BuildingNo + ") " + b.BuildingDesc);
                        }

                    }
                }

                //Program
                if (model.ProposalProgramID != null)
                {
                    var proposalProgram = _proposalService.GetPrograms()
                           .Where(x => x.ProposalProgramID == model.ProposalProgramID)
                           .FirstOrDefault();
                    model.ProposalProgramText = proposalProgram.ProgramName;
                }

                //Proposal Procurement
                if (model.ProposalProcurementID != null)
                {
                    var proposalProgram = _proposalService.GetAllProposalProcurement()
                           .Where(x => x.ProposalProcurementID == model.ProposalProcurementID)
                           .FirstOrDefault();
                    model.ProposalProcurementText = proposalProgram.ProcurmentMethod;
                }

                //Sub-Program
                if (model.ProposalSubProgramID != null)
                {
                    var proposalProgram = _proposalService.GetSubPrograms()
                           .Where(x => x.ProposalSubProgramID == model.ProposalSubProgramID)
                           .FirstOrDefault();
                    model.ProposalSubProgramText = proposalProgram.SubProgramName;
                }

                //Work Nominated By
                if (model.ProposalWorkNominatedID != null)
                {
                    var proposalProgram = _proposalService.GetWorkNominated()
                           .Where(x => x.ProposalWorkNominatedID == model.ProposalWorkNominatedID)
                           .FirstOrDefault();
                    model.WorkNominatedByText = proposalProgram.WorkNominatedName;
                }

                //Project Type
                if (model.ProjectTypeID != null)
                {
                    var proposalProgram = _proposalService.GetProjectTypes()
                           .Where(x => x.ProposalProjectTypeID == model.ProjectTypeID)
                           .FirstOrDefault();
                    model.ProposalProjectTypeText = proposalProgram.ProjectTypeName;
                }

                //PMM
                if (model.PMMProfileID != null)
                {
                    var proposalProgram = _proposalService.GetPMMProfiles()
                           .Where(x => x.ProposalPMMProfileID == model.PMMProfileID)
                           .FirstOrDefault();
                    model.PMMText = proposalProgram.PMMProfileName;
                }

                //School Fund Code
                if (model.ProposalSFCID != null)
                {
                    var proposalProgram = _proposalService.GetProposalSFC()
                           .Where(x => x.ProposalSFCID == model.ProposalSFCID)
                           .FirstOrDefault();
                    model.ProposalSFCText = proposalProgram.SchoolFundCode;
                }
                //Proposal Status
                if (model.SelectedCallStatusId != null)
                {
                    var proposalProgram = _proposalService.GetProposalStatuses()
                           .Where(x => x.ProposalStatusID == model.SelectedCallStatusId)
                           .FirstOrDefault();
                    model.ProposalStatusText = proposalProgram.ProposalStatusDesc;
                }


                //ASO
                //if (model.UserID != null)
                //{
                //    var proposalProgram = _proposalService.GetRegionalContact()
                //           .Where(x => x.AgencyId == model.AgencyId)
                //           .FirstOrDefault();
                //    model.ASOText = proposalProgram.Name;
                //}

                //School Endorsement
                if (model.SchoolEndorsements.Count() > 0)
                {
                    var lstSchoolPrincipal = model.SchoolEndorsements.Where(x => x.EndorsementTypeID == 1).FirstOrDefault();
                    var lstSchoolDirector = model.SchoolEndorsements.Where(x => x.EndorsementTypeID == 2).FirstOrDefault();
                    var lstSchoolED = model.SchoolEndorsements.Where(x => x.EndorsementTypeID == 3).FirstOrDefault();

                    //School Principal
                    if (lstSchoolPrincipal != null)
                    {
                        model.SPApprovalDate = lstSchoolPrincipal.ApprovalDate != null ? DateTime.Parse(lstSchoolPrincipal.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.SPPosition = lstSchoolPrincipal.Position;
                        model.SPApprovalStatus = lstSchoolPrincipal.Approved == true ? "Approved" : "Not Approved";
                        model.SPApprovedBy = lstSchoolPrincipal.ApprovedBy;
                    }

                    //School Director
                    if (lstSchoolDirector != null)
                    {
                        model.SDApprovalDate = lstSchoolDirector.ApprovalDate != null ? DateTime.Parse(lstSchoolDirector.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.SDPosition = lstSchoolDirector.Position;
                        model.SDApprovalStatus = lstSchoolDirector.Approved == true ? "Approved" : "Not Approved";
                        model.SDApprovedBy = lstSchoolDirector.ApprovedBy;
                    }

                    //Executive Director
                    if (lstSchoolED != null)
                    {
                        model.EDApprovalDate = lstSchoolED.ApprovalDate != null ? DateTime.Parse(lstSchoolED.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.EDPosition = lstSchoolED.Position;
                        model.EDApprovalStatus = lstSchoolED.Approved == true ? "Approved" : "Not Approved";
                        model.EDApprovedBy = lstSchoolED.ApprovedBy;
                    }

                }

                //Endorsements
                if (model.Endorsements.Count() > 0)
                {
                    var lstASO = model.Endorsements.Where(x => x.EndorsementTypeID == 4).FirstOrDefault();
                    var lstGL = model.Endorsements.Where(x => x.EndorsementTypeID == 5).FirstOrDefault();
                    var lstSGL = model.Endorsements.Where(x => x.EndorsementTypeID == 6).FirstOrDefault();
                    var lstDAM = model.Endorsements.Where(x => x.EndorsementTypeID == 7).FirstOrDefault();
                    var lstEDAM = model.Endorsements.Where(x => x.EndorsementTypeID == 8).FirstOrDefault();

                    //Asset Service Officer
                    if (lstASO != null)
                    {
                        model.ASOApprovalDate = lstASO.ApprovalDate != null ? DateTime.Parse(lstASO.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.ASOPosition = lstASO.Position;
                        model.ASOApprovalStatus = lstASO.Approved == true ? "Approved" : "Not Approved";
                        model.ASOApprovedBy = lstASO.ApprovedBy;
                    }

                    //Group Leader
                    if (lstGL != null)
                    {
                        model.GLApprovalDate = lstGL.ApprovalDate != null ? DateTime.Parse(lstGL.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.GLPosition = lstGL.Position;
                        model.GLApprovalStatus = lstGL.Approved == true ? "Approved" : "Not Approved";
                        model.GLApprovedBy = lstGL.ApprovedBy;
                    }

                    //Senior Group Leader
                    if (lstSGL != null)
                    {
                        model.SGLApprovalDate = lstSGL.ApprovalDate != null ? DateTime.Parse(lstSGL.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.SGLPosition = lstSGL.Position;
                        model.SGLApprovalStatus = lstSGL.Approved == true ? "Approved" : "Not Approved";
                        model.SGLApprovedBy = lstSGL.ApprovedBy;
                    }

                    //Director Asset Management
                    if (lstDAM != null)
                    {
                        model.DAMApprovalDate = lstDAM.ApprovalDate != null ? DateTime.Parse(lstDAM.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.DAMPosition = lstDAM.Position;
                        model.DAMApprovalStatus = lstDAM.Approved == true ? "Approved" : "Not Approved";
                        model.DAMApprovedBy = lstDAM.ApprovedBy;
                    }

                    //Executive Director Asset Management
                    if (lstEDAM != null)
                    {
                        model.EDAMApprovalDate = lstEDAM.ApprovalDate != null ? DateTime.Parse(lstEDAM.ApprovalDate.ToString()).ToString("dd/MM/yyyy") : "";
                        model.EDAMPosition = lstEDAM.Position;
                        model.EDAMApprovalStatus = lstEDAM.Approved == true ? "Approved" : "Not Approved";
                        model.EDAMApprovedBy = lstEDAM.ApprovedBy;
                    }

                }

                if (model.Entitlements != null)
                {
                    EntitlementCheckboxes(model);

                }


                if (model != null)
                {
                    model.PRSubmittedDate = model.SubmittedtDate != null ? DateTime.Parse(model.SubmittedtDate.ToString()).ToString("dd/MM/yyyy") : "";
                    model.PRReviewDate = model.PRReviewDate != null ? DateTime.Parse(model.PRReviewDate.ToString()).ToString("dd/MM/yyyy") : "";
                    model.Contractor = _proposalService.GetAgencyContractor(model.AgencyId);
                    model.BuildingList = String.Join(",", buildingList);
                    model.ConceptEstimateGST = model.ConceptEstimate > 0 ? (model.ConceptEstimate + (model.ConceptEstimate * 10 / 100)) : model.ConceptEstimate;
                    return View(model);
                }


            }
            return View(new ProposalAddEditViewModel());

        }

        public ProposalAddEditViewModel EntitlementCheckboxes(ProposalAddEditViewModel model)
        {
            //Planning Requirements
            if (model.Entitlements.Exempt == true)
                model.PlanningRequirementText += "Exempt,";
            if (model.Entitlements.REF == true)
                model.PlanningRequirementText += "REF,";
            if (model.Entitlements.CDC == true)
                model.PlanningRequirementText += "CDC,";
            if (model.Entitlements.DA == true)
                model.PlanningRequirementText += "DA,";
            if (model.Entitlements.PRNone == true)
                model.PlanningRequirementText += "NA,";
            model.PlanningRequirementText = model.PlanningRequirementText != null ? model.PlanningRequirementText.TrimEnd(',') : string.Empty;

            //Environmental Planning
            if (model.Entitlements.Arboist == true)
                model.EnvironmentPlanningText += "Arboist,";
            if (model.Entitlements.Bushfire == true)
                model.EnvironmentPlanningText += "Bushfire protection zone,";
            if (model.Entitlements.Contamination == true)
                model.EnvironmentPlanningText += "Contamination issues,";
            if (model.Entitlements.FloodProne == true)
                model.EnvironmentPlanningText += "Flood prone land,";
            if (model.Entitlements.Heritage == true)
                model.EnvironmentPlanningText += "Heritage,";
            if (model.Entitlements.Ecology == true)
                model.EnvironmentPlanningText += "Ecology,";
            if (model.Entitlements.Archaeology == true)
                model.EnvironmentPlanningText += "Archaeology,";
            if (model.Entitlements.EPNone == true)
                model.EnvironmentPlanningText += "NA,";
            model.EnvironmentPlanningText = model.EnvironmentPlanningText != null ? model.EnvironmentPlanningText.TrimEnd(',') : string.Empty;

            //Stakeholders to be consulted
            if (model.Entitlements.SSU == true)
                model.StakeholderText += "SSU,";
            if (model.Entitlements.ITD == true)
                model.StakeholderText += "ITD,";
            if (model.Entitlements.Demountable == true)
                model.StakeholderText += "Demountable unit,";
            if (model.Entitlements.AssetActivation == true)
                model.StakeholderText += "Asset activation,";
            if (model.Entitlements.SNone == true)
                model.StakeholderText += "NA,";
            model.StakeholderText = model.StakeholderText != null ? model.StakeholderText.TrimEnd(',') : string.Empty;

            //Operational constraints
            if (model.Entitlements.ServiceIntereption == true)
                model.OperationalText += "Service interruption,";
            if (model.Entitlements.ExamsInOperation == true)
                model.OperationalText += "Exams in opeartion,";
            if (model.Entitlements.VacationPeriod == true)
                model.OperationalText += "Vacation period only,";
            if (model.Entitlements.Relocation == true)
                model.OperationalText += "Relocation of facilities required,";
            if (model.Entitlements.LearningWellbeing == true)
                model.OperationalText += "Learning/Wellbeing,";
            if (model.Entitlements.WHS == true)
                model.OperationalText += "WHS,";
            if (model.Entitlements.OCNone == true)
                model.OperationalText += "NA,";
            model.OperationalText = model.OperationalText != null ? model.OperationalText.TrimEnd(',') : string.Empty;

            //Tech services
            if (model.Entitlements.StatPlanning == true)
                model.TechServicesText += "Stat planning,";
            if (model.Entitlements.TechHeritage == true)
                model.TechServicesText += "Heritage,";
            if (model.Entitlements.Compliance == true)
                model.TechServicesText += "Compliance,";
            if (model.Entitlements.Sustainability == true)
                model.TechServicesText += "Sustainability,";
            if (model.Entitlements.Comms == true)
                model.TechServicesText += "Comms,";
            if (model.Entitlements.Leaseslicences == true)
                model.TechServicesText += "Leases & licences,";
            if (model.Entitlements.CentralServices == true)
                model.TechServicesText += "CentralServices/Contracts,";
            if (model.Entitlements.TSNone == true)
                model.TechServicesText += "NA,";
            model.TechServicesText = model.TechServicesText != null ? model.TechServicesText.TrimEnd(',') : string.Empty;

            //Hazardous material
            if (model.Entitlements.Asbestos == true)
                model.HazardousText += "Asbestos,";
            if (model.Entitlements.Lead == true)
                model.HazardousText += "Lead,";
            if (model.Entitlements.Mould == true)
                model.HazardousText += "Mould,";
            if (model.Entitlements.HMNone == true)
                model.HazardousText += "NA,";
            model.HazardousText = model.HazardousText != null ? model.HazardousText.TrimEnd(',') : string.Empty;

            //Services
            if (model.Entitlements.Electrical == true)
                model.ServicesText += "Electrical,";
            if (model.Entitlements.Hydraulics == true)
                model.ServicesText += "Hydraulics,";
            if (model.Entitlements.Sewer == true)
                model.ServicesText += "Sewer,";
            if (model.Entitlements.Mechanical == true)
                model.ServicesText += "Mechanical,";
            if (model.Entitlements.Stormwater == true)
                model.ServicesText += "Stormwater,";
            if (model.Entitlements.IT == true)
                model.ServicesText += "IT,";
            if (model.Entitlements.Security == true)
                model.ServicesText += "Security,";
            if (model.Entitlements.SNone == true)
                model.ServicesText += "NA,";
            model.ServicesText = model.ServicesText != null ? model.ServicesText.TrimEnd(',') : string.Empty;



            return model;
        }


        private void initProposalMasterData(int? propertyId, int? workCategoryId, int? AgencyId, int? programId, out int defaultBuilding)
        {

            ViewData["BuildingsList"] = propertyId.HasValue ? _buildingService.GetByProperty(propertyId.Value).Select(x => new BuildingViewModel
            {
                BuildingID = x.BuildingID,
                BuildingDesc = "(" + x.BuildingNo + ") " + x.BuildingDesc + ((x.RemovedYN.HasValue && x.RemovedYN.Value) ? " (REMOVED)" : "")
            }).ToList() : new List<BuildingViewModel>();

            var building = _buildingService.GetByProperty(propertyId.Value).ToList().Where(x => x.BuildingDesc.Equals("Grounds")).Count();//==null?0: _buildingService.GetByProperty(propertyId.Value).Where(x => x.BuildingDesc.Equals("Grounds")).FirstOrDefault().BuildingID;

            defaultBuilding = propertyId.HasValue ? building == 0 ? 0 : _buildingService.GetByProperty(propertyId.Value).Where(x => x.BuildingDesc.Equals("Grounds")).FirstOrDefault().BuildingID : 0;
            //load callLogUrgency
            List<SelectListItem> callLogUrgency = _proposalService.GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID < 3)
                                    .Select(x => new SelectListItem() { Text = x.CallLogFinanceUrgencyDesc, Value = x.CallLogFinanceUrgencyID.ToString() }).ToList();
            ViewData["CallLogUrgency"] = callLogUrgency;

            //load Call Status
            List<SelectListItem> proposalStatus = _proposalService.GetProposalStatuses()
                                    .Select(x => new SelectListItem() { Text = x.ProposalStatusDesc, Value = x.ProposalStatusID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["ProposalStatus"] = proposalStatus;
            //load Call Type
            List<SelectListItem> callLogType = _proposalService.GetCallLogTypes()
                                    .Select(x => new SelectListItem() { Text = x.CallLogTypeDesc, Value = x.CallLogTypeID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["CallLogType"] = callLogType;

            //Load Equipement Type
            List<SelectListItem> assemblyEquipmentTypes = _proposalService.GetEquipmentTypeAssemblies()
                                  .Select(x => new SelectListItem() { Text = x.EquipmentTypeBSEMAssemblyDesc, Value = x.EquipmentTypeBSEMAssemblyID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["AssemblyEquipmentTypes"] = assemblyEquipmentTypes;

            //Load Maintenance CallLogs Type
            List<CustomDropdownItem> maintenanceCallLog = _proposalService.GetMaintenanceTasksCallLogs()
                .Select(x => new CustomDropdownItem { Text = x.MaintenanceTaskCallLogDesc, Value = x.MaintenanceTaskCallLogID.ToString(), Data = x.Type })
                .OrderBy(x => x.Text)
               .ToList();
            ViewData["MaintenanceCallLog"] = maintenanceCallLog;

            List<SelectListItem> projectType = _proposalService.GetProjectTypes()
                                   .Select(x => new SelectListItem() { Text = x.ProjectTypeName, Value = x.ProposalProjectTypeID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["ProjectType"] = projectType;

            List<SelectListItem> workNominated = _proposalService.GetWorkNominated()
                                  .Select(x => new SelectListItem() { Text = x.WorkNominatedName, Value = x.ProposalWorkNominatedID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["WorkNominated"] = workNominated;

            List<SelectListItem> proposalProgram = _proposalService.GetPrograms()
                                  .Select(x => new SelectListItem() { Text = x.ProgramName, Value = x.ProposalProgramID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["ProposalProgram"] = proposalProgram;

            List<SelectListItem> proposalSubProgram = _proposalService.GetSubPrograms(programId)
                                  .Select(x => new SelectListItem() { Text = x.SubProgramName, Value = x.ProposalSubProgramID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["ProposalSubProgram"] = proposalSubProgram;

            List<SelectListItem> pmmProfile = _proposalService.GetPMMProfiles()
                                .Select(x => new SelectListItem() { Text = x.PMMProfileName, Value = x.ProposalPMMProfileID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["PMMProfile"] = pmmProfile;

            List<SelectListItem> proposalSFC = _proposalService.GetProposalSFC()
                              .Select(x => new SelectListItem() { Text = x.SchoolFundCode, Value = x.ProposalSFCID.ToString() }).OrderBy(a => a.Text).ToList();
            proposalSFC.Insert(0, (new SelectListItem { Text = "---Select---", Value = "0" }));
            ViewData["ProposalSFC"] = proposalSFC;

            List<SelectListItem> proposalProcurement = _proposalService.GetProposalProcurement()
                             .Select(x => new SelectListItem() { Text = x.ProcurmentMethod, Value = x.ProposalProcurementID.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["ProposalProcurement"] = proposalProcurement;

            List<SelectListItem> pmASO = _proposalService.GetRegionalContact().Where(x => x.AgencyId == AgencyId)
                              .Select(x => new SelectListItem() { Text = x.Name, Value = x.Id.ToString() }).OrderBy(a => a.Text).ToList();
            ViewData["PMASO"] = pmASO;
            //ViewData["ProposalRoles"] = _proposalService.GetProposalRoles((int)AgencyId, (int)propertyId);
            workCategoryId = 1;
            //if (workCategoryId.HasValue)
            //{
            int supportDocumentClass = _proposalService.GetSupportDocumentClassId(1);

            //Load Document Type
            List<SelectListItem> supportDocumentType = workCategoryId.HasValue ? _proposalService.GetSupportDocumentTypes()
                                  .Where(d => d.SupportDocumentClassID == supportDocumentClass)
                                  .Select(x => new SelectListItem() { Text = x.SupportDocumentTypeDesc, Value = x.SupportDocumentTypeID.ToString() }).OrderBy(a => a.Text).ToList() :
                                  new List<SelectListItem>();
            ViewData["SupportDocumentType"] = supportDocumentType;

            //}

        }


    }
}
