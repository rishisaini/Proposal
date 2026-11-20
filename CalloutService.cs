using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Repository;
using AssetCare.Repository.Interface;
using AssetCare.Repository.Interfaces;
using AssetCare.Service.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AssetCare.Service
{
    public class CalloutService : ICalloutService
    {
        IBaseService _baseService;
        private IUnitOfWork _unitOfWork;
        private IPropertyService _propertyService;
        private IFMCallOutRepository _fmCallOutRepository;
        private ISupportDocumentRepository _supportDocumentRepository;
        private IAgencyService _agencyService;
        private IUserService _userService;
        private IJobRepository _jobRepository;

        IBuildingService _buildingService;
        public CalloutService(IUnitOfWork unitOfWork, IUserService userService, IAgencyService agencyService,
            IPropertyService propertyService, IFMCallOutRepository fmCallOutRepository,
            ISupportDocumentRepository supportDocumentRepository, IBuildingService buildingService,IJobRepository jobRepository, IBaseService baseService)
        {
            _baseService = baseService;
            _unitOfWork = unitOfWork;
            _propertyService = propertyService;
            _fmCallOutRepository = fmCallOutRepository;
            _supportDocumentRepository = supportDocumentRepository;
            _agencyService = agencyService;
            _userService = userService;
            _buildingService = buildingService;
            _jobRepository = jobRepository;
        }
        public IEnumerable<CallOutViewModel> FilterCallOutData(CallOutFilterModel filterModel, bool includeFullDesc, out int filteredResultsCount,  out int totalResultsCount)
        {
            var currentUSerRole = _baseService.GetCurrentUser().RoleID;
            var currentUSerRoleCode = _baseService.GetCurrentUser().RoleCode;
            var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, filterModel.AgencyId).UserPropertyIds;
            return _fmCallOutRepository.FilterCallOutData(filterModel, includeFullDesc, userPropertyIds,"",  out filteredResultsCount, out totalResultsCount);

        }

        public IEnumerable<CallOutForHPViewModel> FilterCallOutDataForHP(CallOutFilterModel filterModel, bool includeFullDesc, out int filteredResultsCount, out int totalResultsCount)
        {
            using (var transaction = _unitOfWork.BeginSnapshotTransaction())
            {
                //Filter result with user properties
                var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, filterModel.AgencyId).UserPropertyIds;
                var userRole = _baseService.CurrentUserRole;
                transaction.Commit();

                return _fmCallOutRepository.FilterCallOutDataForHP(filterModel, includeFullDesc, userPropertyIds, userRole, out filteredResultsCount, out totalResultsCount);
            }
        }

        public List<Element> GetElementList()
        {
            return _unitOfWork.ElementRepository.GetAll().ToList();
        }       

        public CallOutAddEditViewModel GetAddCalloutViewModel(int agencyId, int propertyId, int workCategoryId, int? calloutId)
        {
            CallOutAddEditViewModel callOutAddEdit = new CallOutAddEditViewModel();
            callOutAddEdit.mode = "add";
            callOutAddEdit.AgencyID = agencyId;
            callOutAddEdit.PropertyID = propertyId;
                      

            if (workCategoryId > 0)
            {
                callOutAddEdit.SelectedSchWorkCategoryId = workCategoryId;
                callOutAddEdit.SchWorkCategory = _unitOfWork.WorkCategoryRepository.GetAll()
                                            .Where(x => x.InList == true && x.WorkCategoryId == workCategoryId)
                                            .FirstOrDefault().Description;
            }


            Property schoolDetails = _propertyService.GetById(propertyId);
            callOutAddEdit.SchoolId = schoolDetails.SchoolId.HasValue ? schoolDetails.SchoolId.Value : 0;
            callOutAddEdit.SchoolName = schoolDetails.PropertyShortDesc;
            callOutAddEdit.SchoolEmail = schoolDetails.Email;
            callOutAddEdit.Phone = schoolDetails.Phone;
            callOutAddEdit.SchoolAddress = schoolDetails.PropertyShortDesc + ", " + schoolDetails.Street + ", " + schoolDetails.Suburb + ", " + schoolDetails.State + ", " + schoolDetails.Postcode;
            callOutAddEdit.ASOName = schoolDetails.ASOName;
            callOutAddEdit.ASOEmail = schoolDetails.ASOEmail;
            callOutAddEdit.ASOContactNumber = schoolDetails.ASOContactNumber; 
            callOutAddEdit.GLName = schoolDetails.GLName;
            callOutAddEdit.GLContactNumber = schoolDetails.GLContactNumber;
            var schoolLimitData = _unitOfWork.SchoolLimitRepository.GetAll()
                                        .Where(x => x.PropertyID == propertyId && (x.EndDate == null || x.EndDate >= DateTime.Now)).FirstOrDefault();
            if (schoolLimitData != null)
            {
                callOutAddEdit.SchoolLimit = decimal.Round(schoolLimitData.SchoolLimitAmount ?? 0, 2, MidpointRounding.AwayFromZero);
                callOutAddEdit.SchoolLimitId = schoolLimitData.SchoolLimitID;
            }



            callOutAddEdit.Area = schoolDetails.EducationArea != null ? schoolDetails.EducationArea.EducationAreaDesc : string.Empty;

            callOutAddEdit.SupportDocument.SupportDocumentClassID = GetSupportDocumentClassId(workCategoryId);
            callOutAddEdit.SupportDocs = JsonConvert.SerializeObject(callOutAddEdit.SupportDocuments);
            //set to true by defaul on Add Mode
            callOutAddEdit.EmailToSchoolAndEmc = true;
            callOutAddEdit.SubContractor = new SubContractor();
            callOutAddEdit.LocalContractorWorkOderDetails = new List<SubContractoWorkOrder>();
            callOutAddEdit.IsInsuranceEvent = false;
            callOutAddEdit.IsSecurityIncident = false;
            callOutAddEdit.IsTotalEstimationOverLimit = false;
            callOutAddEdit.IsReadyForReimbursement = false;
            callOutAddEdit.IsReimbursementComplete = false;
            callOutAddEdit.AsbestosHR = schoolDetails.AsbestosHR;
            callOutAddEdit.DefectMgmt = schoolDetails.DefectMgmt;
            return callOutAddEdit;
        }

        public int GetSupportDocumentClassId(int? workCategoryId)
        {
            int supportDocumentClass = 0;
            if (workCategoryId.HasValue)
            {
                switch (workCategoryId.Value)
                {
                    case 1:
                    case 2:
                    case 100: // Added case for Proposal.
                        supportDocumentClass = (int)SupportDocumentClassEnum.CALLEUR;
                        break;

                    //case 2:
                    //    supportDocumentClass = (int)SupportDocumentClassEnum.CALLMET;
                    //    break;

                    case 4:
                        supportDocumentClass = (int)SupportDocumentClassEnum.PRV;
                        break;
                    case 13:
                        supportDocumentClass = (int)SupportDocumentClassEnum.CALLREA;
                        break;

                    case 17:
                        supportDocumentClass = (int)SupportDocumentClassEnum.CPD;
                        break;
                    case 105:
                        supportDocumentClass = (int)SupportDocumentClassEnum.EQP;
                        break;
                    case 106:
                        supportDocumentClass = (int)SupportDocumentClassEnum.DEQP;
                        break;
                    case 107:
                        supportDocumentClass = (int)SupportDocumentClassEnum.SCH;
                        break;
                    case 108:
                        supportDocumentClass = (int)SupportDocumentClassEnum.ABS;
                        break;
                  
                }
            }

            return supportDocumentClass;
        }

        public List<WorkCategory> GetWorkCategory()
        {
            return _unitOfWork.WorkCategoryRepository.GetAll().Where(x => x.InList == true).ToList();
        }

        public IEnumerable<CallLogFinanceUrgency> GetFinanceUrgency()
        {
            return _unitOfWork.CallLogFinanceUrgencyRepository.GetAll();
        }

        public IEnumerable<CallLogStatus> GetCallLogStatuses()
        {
            return _unitOfWork.CallLogStatusRepository.GetAll();
        }

        public IEnumerable<CallLogType> GetCallLogTypes()
        {
            return _unitOfWork.CallLogTypeRepository.GetAll();
        }

        public IEnumerable<EquipmentTypeBSEMAssembly> GetEquipmentTypeAssemblies()
        {
            return _unitOfWork.AssemblyEquipmentTypesRepository.GetAll();
        }

        public IEnumerable<MaintenanceTasksCallLog> GetMaintenanceTasksCallLogs()
        {
            return _unitOfWork.MaintenanceTasksCallLogRepository.GetAll();
        }

        public IEnumerable<MaintenanceTask> GetMaintenanceTasks()
        {
            return _unitOfWork.MaintenanceTasksRepository.GetAll();
        }

        public IEnumerable<SupportDocumentType> GetSupportDocumentTypes()
        {
            return _unitOfWork.SupportDocumentTypeRepository.GetAll();
        }

        public bool Save(CallOutAddEditViewModel model, string loggedInUser, out int callLogId)
        {
            UpdateUserCapabilities(model, loggedInUser);
            _fmCallOutRepository.Save(model, loggedInUser, out callLogId);
            return true;
        }

        private void UpdateUserCapabilities(CallOutAddEditViewModel model, string loggedInUser)
        {
         
            if (model.IsInsuranceEvent == true)
            {
                //CallLogTypeID,EventNo,LocalCotractorToRepairDamage,IsThirdpartyInvolved,CauseOfTheIssue,SchoolIncidentCauseId
                model.CallLogTypeID = 2;
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "CallLogTypeID");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "EventNo");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "LocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "IsThirdpartyInvolved");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "CauseOfTheIssue");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SchoolIncidentCauseId");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "IsReadyForReimbursement");
            }
            else
            {
                model.CallLogTypeID = model.CallLogTypeID.HasValue? model.CallLogTypeID:1;
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "CallLogTypeID");                
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "EventNo");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "LocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "IsThirdpartyInvolved");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "CauseOfTheIssue");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SchoolIncidentCauseId");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "IsReadyForReimbursement");
            }

            if (model.IsSecInsuranceEvent == true)
            {
                //CallLogTypeID,EventNo,LocalCotractorToRepairDamage,IsThirdpartyInvolved,CauseOfTheIssue,SchoolIncidentCauseId
                model.CallLogTypeID = 2;
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "CallLogTypeID");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecEventNo");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecLocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecIsThirdpartyInvolved");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecCauseOfTheIssue");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecSchoolIncidentCauseId");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecIsReadyForReimbursement");
            }
            else
            {
                model.CallLogTypeID = model.CallLogTypeID.HasValue ? model.CallLogTypeID : 1;
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "CallLogTypeID");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SecEventNo");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SecLocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SecIsThirdpartyInvolved");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SecCauseOfTheIssue");
                model.UserCapabilities.MainSection = removeUserCapability(model.UserCapabilities.MainSection, "SecSchoolIncidentCauseId");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "SecIsReadyForReimbursement");
            }
            // update ProcessedBy and ProcessedDate on creating WOs and on completing callout 
            if (!string.IsNullOrEmpty(model.NewContractors) || model.Complete)
            {
                model.ProcessedBy = loggedInUser;
                model.ProcessedDate = DateTime.Now;
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "ProcessedBy");
                model.UserCapabilities.MainSection = addUserCapability(model.UserCapabilities.MainSection, "ProcessedDate");
            }

            //if (model.HasAtleaseOneClaim.HasValue &&
            //    model.HasAtleaseOneClaim.Value)
            //{
            //        model.ProcessedByIRCM = loggedInUser;
            //        model.ProcessedDateByIRCM = DateTime.Now;
            //        model.UserCapabilities.IRCMSection = addUserCapability(model.UserCapabilities.IRCMSection, "ProcessedByIRCM");
            //        model.UserCapabilities.IRCMSection = addUserCapability(model.UserCapabilities.IRCMSection, "ProcessedDateByIRCM");
            //}
        }

        private string removeUserCapability(string sectionData, string key)
        {
            if (!string.IsNullOrEmpty(sectionData))
            {
                if (!sectionData.Contains(key))
                {
                    sectionData = sectionData.Replace(key, string.Empty);
                }
            }

            return sectionData;
        }
        private string addUserCapability(string sectionData, string key)
        {
            if (!string.IsNullOrEmpty(sectionData))
            {
                if (!sectionData.Contains(key))
                {
                    sectionData = sectionData + "," + key;
                }
            }

            return sectionData;
        }

        public List<string> DetectAlertKeywords(CallOutAddEditViewModel model)
        {
            List<EmailKeyword> keywords = this.GetEmailKeywords();
            List<string> detectedKeywords = new List<string>();
            string callLogDesc = model.CallLogDesc.ToLower();
            foreach (EmailKeyword keyword in keywords)
            {
                if (callLogDesc.Contains(keyword.Keyword))
                {
                    detectedKeywords.Add(keyword.Keyword);
                }
            }

            return detectedKeywords;

        }

        public CallOutAddEditViewModel GetCallOutDetails(int? id, int agencyId, double estimationThreshold)
        {

            return _fmCallOutRepository.GetDetailsById(id, agencyId, estimationThreshold);
        }

        public bool Update(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            UpdateUserCapabilities(model, currentUser.UserCode);
            return _fmCallOutRepository.Update(model, currentUser);
        }

        public void DeleteDocument(int? supportDocumentID)
        {
            _fmCallOutRepository.DeleteDocument(supportDocumentID);
        }

        public AgencyContractor GetAgencyContractor(int agencyId)
        {
            return _agencyService.GetById(agencyId).AgencyContractor;
        }

        public void DeleteCallOut(int? callOutId, int? agencyId)
        {
            _fmCallOutRepository.DeleteCallOut(callOutId, agencyId);
        }

        public int SaveDocument(SupportDocument document, int callOutId, out int documentId)
        {
            return _fmCallOutRepository.SaveDocument(document, callOutId, out documentId);
        }

        public Property GetPropertyDetails(int propertyId)
        {
            Property schoolDetails = _propertyService.GetById(propertyId);
            return schoolDetails;
        }

        public IEnumerable<WorkType> GetWorkTypes()
        {
            return _unitOfWork.WorkTypeRepository.GetAll().ToList();
        }

        public bool SaveInsIncident(CallOutInsAddModel model, UserViewModel currentUser)
        {

            bool status = _fmCallOutRepository.SaveSchoolIncident(model);
            if (status)
            {
                string subject = string.Format("{0} Insurance Callout Job - Contract: {1} FMWeb Incident ID: {2}", model.CallLogFinanceUrgencyDesc, model.AgencyId, model.SchoolIncidentAgencySequenceID);
                Agency agency = _agencyService.GetById(model.AgencyId);

                if (model.ContractorCalledOut || model.ContractorEmailed)
                {
                    sendEmail(model, currentUser, agency.AgencyContractor.Email, "InsuranceIncidentEmailContent.html", subject);
                }
                return true;
            }

            return false;
        }

        private bool sendEmail(CallOutInsAddModel schoolIncident, UserViewModel currentUser, string toEmail, string template, string subject)
        {
            Agency agency = _agencyService.GetById(schoolIncident.AgencyId);
            var request = HttpContext.Current.Request;

            var baseURL = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, request.ApplicationPath);

            var assetCareLink = String.Format("{0}/IRCM/SchoolIncidentDetails?schoolIncidentId={1}", baseURL, schoolIncident.SchoolIncidentID);
            EmailService emailHelper = new EmailService();
            toEmail = toEmail + ";" + currentUser.Email;

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("IncidentID", schoolIncident.SchoolIncidentAgencySequenceID.ToString());
            data.Add("AgencyID", schoolIncident.AgencyId.ToString() /*+ '-' + schoolIncident.Agency?.AgencyName*/);
            data.Add("RequestedBy", schoolIncident.ContactFirstName + " " + schoolIncident.ContactLastName);
            data.Add("SchoolName", schoolIncident.SchoolName);
            data.Add("Phone", schoolIncident.ContactPhone);
            data.Add("AgencyEmail", agency.AgencyContractor?.Email);
            data.Add("Area", schoolIncident.Area);
            data.Add("Region", agency.AgencyName);

            List<string> buildingList = new List<string>();
            foreach (var buildingId in schoolIncident.InsJobBuildingIDs)
            {
                var b = _buildingService.GetByProperty(schoolIncident.PropertyId)
                  .Where(x => x.BuildingID == buildingId)
                  .FirstOrDefault();
                if (b != null)
                {

                    buildingList.Add("(" + b.BuildingNo + ") " + b.BuildingDesc + ((b.RemovedYN.HasValue && b.RemovedYN.Value) ? " (REMOVED)" : ""));
                }
            }
            data.Add("BuildingList", String.Join(",", buildingList));
            data.Add("Rooms", schoolIncident.InsRooms);
            data.Add("RoomLocation", schoolIncident.LocationDesc);
            data.Add("SchoolEmail", schoolIncident.SchoolEmail);
            data.Add("WorkCategoryDesc", "Insurance Works");
            data.Add("CallDate", schoolIncident.CallDate.Value.ToString("dd-MM-yyyy"));
            data.Add("CallTime", schoolIncident.CallDate.Value.ToString("HH:mm tt"));
            data.Add("TotalAmount", "N/A");
            data.Add("ScopeOfWork", schoolIncident.DamageDetailDesc);
            data.Add("JobComments", schoolIncident.SchoolIncidentDesc);
            data.Add("Link", assetCareLink);
            emailHelper.SendMail(toEmail, data, subject, template);
            return true;
        }

        public List<EmailKeyword> GetEmailKeywords()
        {
            return _unitOfWork.EmailKeywordRepository.GetAll().Where(x => x.KeywordTypeID == 1).ToList();
        }

       

        public List<WorkCategory> GetAllWorkCategories()
        {
            return _unitOfWork.WorkCategoryRepository.GetAll().ToList();
        }
       
        public SubContractor GetSubContractortails(int subContractorId)
        {
            return _jobRepository.GetSubContractorDetails(subContractorId);
        }

        public LCWOViewModel GetLCWOCount(int subContractorId)
        {
            return _jobRepository.GetLCWOCount(subContractorId);
        }

        public  TradieResponse GetTradesPeopleList(CallOutFilterModel callOutFilterModel, out int filteredResultsCount, out int totalResultsCount)
        {
            return _fmCallOutRepository.GetTradies(callOutFilterModel,out filteredResultsCount, out totalResultsCount);
        }

        public IEnumerable<TradieViewModel> GetTradieDetailsHP(int woid, int agencyid)
        {
            return _fmCallOutRepository.GetTradieDetailsHP(woid, agencyid);
        }
        public IEnumerable<TradieViewModel> GetSchoolHomeTradies(CallOutFilterModel callOutFilterModel, out int filteredResultsCount, out int totalResultsCount)
        {
            return _fmCallOutRepository.GetSchoolHomeTradies(callOutFilterModel, out filteredResultsCount, out totalResultsCount);
        }

        public int? ConvertCallOutToWorkOrder(CallOutAddEditViewModel model, int subContractorid, string loggedInUser, string comments)
        {

            return _jobRepository.ConvertCallOutToWorkOrder(model, subContractorid, loggedInUser, comments);

        }

        public void SendEmailToContractor(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            //if (model.EmailToSchoolAndEmc)// it was a chkbox, if ticked. Need to know at what point we need to send this email
            //{
            toEmail = model.SchoolEmail + ";" + currentUser.Email;
            //get agency contract
            Agency agencyDetails = _agencyService.GetById(model.AgencyID);
            if (agencyDetails != null)
            {
                toEmail = toEmail + ";" + agencyDetails.ContactEmail;
            }

        
            var callLogUrgency = GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID == model.CallLogFinanceUrgencyID).FirstOrDefault();
            string callOutId = model.AgencySequenceID.HasValue ? model.AgencySequenceID.ToString() : "NA";

            Dictionary<string, string> data = GetEmailData(model, currentUser);

            EmailService emailHelper = new EmailService();


            string subject = "Local Contractor has been assigned, Worker Order ID - " + model.JobId; //WorkerId
            emailHelper.SendMail(toEmail, data, subject, "CallLogEmailContent.html", cc: sinswEmail);



        }

        public bool SendCalloutTotalEstimationOverLimit(CallOutAddEditViewModel model, UserViewModel currentUser, string estimationThresholdText)
        {
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            string ircmEmail = ConfigurationManager.AppSettings["AssetCareSettings:DETMFUEmail"].ToString();
            toEmail = ircmEmail + ";" + model.SchoolEmail + ";" + currentUser.Email;
            //get agency contract
            Agency agencyDetails = _agencyService.GetById(model.AgencyID);
            var callLogUrgency = GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID == model.CallLogFinanceUrgencyID).FirstOrDefault();
            string callOutId = model.AgencySequenceID.HasValue ? model.AgencySequenceID.ToString() : "NA";

            Dictionary<string, string> data = GetEmailData(model, currentUser);
            data.Add("EstimationThreshold", estimationThresholdText);
            EmailService emailHelper = new EmailService();
            var agencyID = model.AgencyID.ToString();
            string subject = $"THE TOTAL ESTIMATION OF WORK ORDERS UNDER CALLOUT WITH ID ({agencyID}, {callOutId}) IS OVER {estimationThresholdText}.";
            return emailHelper.SendMail(toEmail, data, subject, "CalloutTotalEstimationOverLimit.html", cc: sinswEmail);
        }

        public bool SendCalloutCategoryChanged(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            string toEmail = string.Empty;
            string sinswEmail = ConfigurationManager.AppSettings["AssetCareSettings:EmailAutoCC"].ToString();
            string ircmEmail = ConfigurationManager.AppSettings["AssetCareSettings:DETMFUEmail"].ToString();
            toEmail = ircmEmail + ";" + model.SchoolEmail + ";" + currentUser.Email;
            //get agency contract
            Agency agencyDetails = _agencyService.GetById(model.AgencyID);
            var callLogUrgency = GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID == model.CallLogFinanceUrgencyID).FirstOrDefault();
            string callOutId = model.AgencySequenceID.HasValue ? model.AgencySequenceID.ToString() : "NA";

            Dictionary<string, string> data = GetEmailData(model, currentUser);
            var newCategory = (model.CallLogTypeID == 1 ? "REACTIVE" : (model.CallLogTypeID == 2 ? "INSURANCE" : "UNKNOWN"));
            data.Add("Category",newCategory );
            EmailService emailHelper = new EmailService();
            var agencyID = model.AgencyID.ToString();
            string subject = $"THE CATEGORY OF CALLOUT WITH ID ({agencyID}, {callOutId}) CHANGED TO BE {newCategory} IN FMWEB 2.0";
            return emailHelper.SendMail(toEmail, data, subject, "CalloutCategoryChanged.html", cc: sinswEmail);
        }

        private Dictionary<string, string> GetEmailData(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            var callLogUrgency = GetFinanceUrgency().Where(x => x.CallLogFinanceUrgencyID == model.CallLogFinanceUrgencyID).FirstOrDefault();
            Dictionary<string, string> data = new Dictionary<string, string>();
            string callOutId = model.AgencySequenceID.HasValue ? model.AgencySequenceID.ToString() : "NA";
            data.Add("CallOutId", callOutId);
            data.Add("AgencyId", model.AgencyID.ToString());
            data.Add("RequestedBy", currentUser.FullName);
            data.Add("SchoolName", model.SchoolName);
            data.Add("Phone", model.Phone);

            //get agency contract
            Agency agencyDetails = _agencyService.GetById(model.AgencyID);
            if (agencyDetails != null)
            {
                data.Add("AgencyEmail", agencyDetails.ContactEmail);
            }

            //if ((HttpContext.Current.Session["AgencyDetails"]) != null)
            //{
            //    data.Add("Region", ((KeyValuePair<int, string>)HttpContext.Current.Session["AgencyDetails"]).Value);
            //}
            //get buildings list
            List<string> buildingList = new List<string>();
            foreach (int buildingId in model.JobBuildingIDs)
            {

                var b = _buildingService.GetByProperty(model.PropertyID)
                    .Where(x => x.BuildingID == buildingId)
                    .FirstOrDefault();
                if (b != null)
                {
                    buildingList.Add("(" + b.BuildingNo + ") " + b.BuildingDesc);
                }

            }


            data.Add("BuildingList", String.Join(",", buildingList));
            data.Add("Rooms", model.RoomNumber);
            data.Add("RoomLocation", model.RoomLocation);
            data.Add("Area", model.Area);

            data.Add("SchoolEmail", model.SchoolEmail);
            string sCategory = string.Empty;
            switch (model.CallLogTypeID)
            {
                case 1:
                    sCategory = "Reactive";
                    break;
                case 2:
                    sCategory = "Insurance";
                    break;
                case 3:
                    sCategory = "Defect";
                    break;
                default:
                    sCategory = "Reactive";
                    break;
            }

            string workCategory = GetAllWorkCategories()
                .Where(x => x.WorkCategoryId == model.WorkCategoryID)
                .FirstOrDefault().Description;

            data.Add("WorkCategoryDesc", sCategory);

            data.Add("CallDate", model.CallDate.Value.ToString("dd-MM-yyyy"));
            data.Add("CallTime", model.CallDate.Value.ToString("HH:mm tt"));
            data.Add("ScopeOfWork", model.Comments);
            data.Add("JobComments", model.CallLogDesc);
            data.Add("WorkOrderComments", model.WorkOrderComments);
            if (callLogUrgency != null)
            {
                data.Add("CallResponseTime", callLogUrgency.CallLogFinanceUrgencyDesc);
            }

            var request = HttpContext.Current.Request; //ControllerContext.RequestContext.HttpContext.Request;

            data.Add("Link", string.Format("{0}://{1}{2}?id={3}", request.Url.Scheme, request.Url.Authority, request.ApplicationPath + "/CallOut/Detail", model.CallLogFITSID));
            return data;
        }

        public List<BusinessType> GetSubContractorBusinessTypes()
        {
            return _unitOfWork.BusinessTypeRepository.GetAll().ToList();
        }
        public List<SubContractor> GetSubContractorsByBusinessType(int id, int agencyId)
        {
            return _fmCallOutRepository.GetSubContractorsByBusinessType(id, agencyId);
        }

        public List<SubContractor> GetSubContractorsByAgency(int agencyId)
        {
            return _jobRepository.GetSubContractorsByAgency(agencyId);
        }

        public List<SubContractor> GetLocalContractorsByAgency(int agencyId)
        {
            return _jobRepository.GetLocalContractorsByAgency(agencyId);
        }

        public SubContractor GetSubContractorDetails(int subContractorId)
        {
            return _unitOfWork.SubContractorRepository.GetById(subContractorId);
        }

        public TaskListViewModel GetCalloutCounts(int agencyID, int propertyID, string roleID)
        {
            var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, agencyID).UserPropertyIds;
            return _fmCallOutRepository.GetCalloutCounts(agencyID, propertyID, userPropertyIds,roleID, Convert.ToInt32(_baseService.GetCurrentUser().DefaultLocalContractorId));
        }
        public SubContractor GetSubContractorInfo()
        {
            return _fmCallOutRepository.GetSubContractorInfo(Convert.ToInt32(_baseService.GetCurrentUser().UserID));
        }

        public bool IsCalloutTotalEstimationOverLimit(int? id, int agencyId, double estimationThreshold)
        {

            return _fmCallOutRepository.IsTotalEstimationOverLimit(id, agencyId, estimationThreshold);
        }

        public IEnumerable<AssessorStatus> GetAssessorStatuses()
        {
            return _fmCallOutRepository.GetAssessorStatuses();
        }

        public bool UpdateTotalEstimationOverLimitNotificationDate(CallOutAddEditViewModel calloutModel)
        {
            return _fmCallOutRepository.UpdateTotalEstimationOverLimitNotificationDate(calloutModel);
        }

    }
}
