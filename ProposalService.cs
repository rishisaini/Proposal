using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Repository;
using AssetCare.Repository.Interface;
using AssetCare.Repository.Interfaces;
using AssetCare.Service.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AssetCare.Service
{
    public class ProposalService : IProposalService
    {
        IBaseService _baseService;
        private IUnitOfWork _unitOfWork;
        private IPropertyService _propertyService;
        private IFMProposalRepository _fmProposalsRepository;
        private ISupportDocumentRepository _supportDocumentRepository;
        private IAgencyService _agencyService;
        private IUserService _userService;
        IBuildingService _buildingService;
        public ProposalService(IUnitOfWork unitOfWork, IUserService userService, IAgencyService agencyService,
            IPropertyService propertyService, IFMProposalRepository fmProposalsRepository,
            ISupportDocumentRepository supportDocumentRepository, IBuildingService buildingService, IBaseService baseService)
        {
            _baseService = baseService;
            _unitOfWork = unitOfWork;
            _propertyService = propertyService;
            _fmProposalsRepository = fmProposalsRepository;
            _supportDocumentRepository = supportDocumentRepository;
            _agencyService = agencyService;
            _userService = userService;
            _buildingService = buildingService;
        }
        public IEnumerable<ProposalViewModel> FilterProposalData(ProposalFilterModel filterModel, bool includeFullDesc, out int filteredResultsCount, out int totalResultsCount)
        {
            var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, filterModel.AgencyId).UserPropertyIds;
            return _fmProposalsRepository.FilterProposalData(filterModel, includeFullDesc, userPropertyIds, out filteredResultsCount, out totalResultsCount);

        }


        public IEnumerable<ProposalForHPViewModel> FilterProposalDataForHP(ProposalFilterModel filterModel, bool includeFullDesc, out int filteredResultsCount, out int totalResultsCount)
        {

            using (var transaction = _unitOfWork.BeginSnapshotTransaction())
            {
                //Filter result with user properties
                
                var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, filterModel.AgencyId).UserPropertyIds;
                transaction.Commit();

                return _fmProposalsRepository.FilterProposalDataForHP(_baseService.GetCurrentUser().RoleID,filterModel, includeFullDesc, userPropertyIds, out filteredResultsCount, out totalResultsCount);
            }
        }

        public IEnumerable<ProposalForHPViewModel> FilterProposalDataForEndorsement(ProposalFilterModel filterModel, bool includeFullDesc, out int filteredResultsCount, out int totalResultsCount,string location)
        {

            using (var transaction = _unitOfWork.BeginSnapshotTransaction())
            {
                //Filter result with user properties

                var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, filterModel.AgencyId).UserPropertyIds;
                transaction.Commit();

                return _fmProposalsRepository.FilterProposalDataForEndorsement(_baseService.GetCurrentUser().RoleID, filterModel, includeFullDesc, userPropertyIds, out filteredResultsCount, out totalResultsCount, location);
            }
        }


        public List<Element> GetElementList()
        {
            return _unitOfWork.ElementRepository.GetAll().ToList();
        }

        public int GetSupportDocumentClassId(int? workCategoryId)
        {
            int supportDocumentClass = 0;
            if (workCategoryId.HasValue)
            {
                switch (workCategoryId.Value)
                {
                    case 1:
                        supportDocumentClass = (int)SupportDocumentClassEnum.PROPOSAL;
                        break;
                }
            }

            return supportDocumentClass;
        }

        public Property GetPropertyDetails(int propertyId)
        {
            Property schoolDetails = _propertyService.GetById(propertyId);
            return schoolDetails;
        }
        public int SaveDocument(SupportDocument document, int callOutId, out int documentId)
        {
            return _fmProposalsRepository.SaveDocument(document, callOutId, out documentId);
        }


        public ProposalAddEditViewModel GetAddProposalViewModel(int agencyId, int propertyId, int? calloutId)
        {
            ProposalAddEditViewModel proposalAddEdit = new ProposalAddEditViewModel();
            proposalAddEdit.mode = "add";
            proposalAddEdit.AgencyId = agencyId;
            proposalAddEdit.PropertyId = propertyId;

            List<ProposalEndorsementType> endorsementType = GetEndorsementTypes().Where(x => x.ViewType == "AMU").ToList();
            List<ProposalEndorsementType> schoolEndorsementType = GetEndorsementTypes().Where(x => x.ViewType == "School").ToList();
            proposalAddEdit.EndorsementType = endorsementType;
            proposalAddEdit.Endorsements = new List<EndorsementViewModel>();
            proposalAddEdit.SchoolEndorsements = new List<EndorsementViewModel>();
            if (proposalAddEdit.Endorsements == null || proposalAddEdit.Endorsements.Count == 0)
            {
                for (int i = 0; i < endorsementType.Count; i++)
                {
                    EndorsementViewModel model = new EndorsementViewModel();
                    model.EndorsementTypeID = proposalAddEdit.EndorsementType[i].ProposalEndorsementTypeID;
                    model.EndorsementName = proposalAddEdit.EndorsementType[i].EndorsementTypeName;
                    model.Limit = proposalAddEdit.EndorsementType[i].Limit;


                    proposalAddEdit.Endorsements.Add(model);
                }
            }

            if (proposalAddEdit.SchoolEndorsements == null || proposalAddEdit.SchoolEndorsements.Count == 0)
            {
                for (int i = 0; i < schoolEndorsementType.Count; i++)
                {
                    EndorsementViewModel model = new EndorsementViewModel();
                    model.EndorsementTypeID = schoolEndorsementType[i].ProposalEndorsementTypeID;
                    model.EndorsementName = schoolEndorsementType[i].EndorsementTypeName;
                    model.Limit = schoolEndorsementType[i].Limit;


                    proposalAddEdit.SchoolEndorsements.Add(model);
                }
            }

            Property schoolDetails = _propertyService.GetById(propertyId);
            proposalAddEdit.SchoolId = schoolDetails.SchoolId.HasValue ? schoolDetails.SchoolId.Value : 0;
            proposalAddEdit.SchoolName = schoolDetails.PropertyShortDesc;
            proposalAddEdit.SchoolEmail = schoolDetails.Email;
            proposalAddEdit.PrincipalEmail = schoolDetails.Email;
            proposalAddEdit.ASOName = schoolDetails.ASOName;
            proposalAddEdit.ASOEmail = schoolDetails.ASOEmail;
            proposalAddEdit.ASOContactNumber = schoolDetails.ASOContactNumber;
            proposalAddEdit.Phone = schoolDetails.Phone;
            proposalAddEdit.Phone = schoolDetails.Phone;
            proposalAddEdit.PrincipalName = schoolDetails.ContactName;
            //proposalAddEdit.ASOID = _userService.GetUserASOPM(schoolDetails.ASOEmail);

            var schoolLimitData = _unitOfWork.SchoolLimitRepository.GetAll()
                                        .Where(x => x.PropertyID == propertyId && (x.EndDate == null || x.EndDate >= DateTime.Now)).FirstOrDefault();
            if (schoolLimitData != null)
            {
                proposalAddEdit.SchoolLimit = decimal.Round(schoolLimitData.SchoolLimitAmount ?? 0, 2, MidpointRounding.AwayFromZero);
                proposalAddEdit.SchoolLimitId = schoolLimitData.SchoolLimitID;
            }
            proposalAddEdit.SupportDocument.SupportDocumentClassID = GetSupportDocumentClassId(1);
            proposalAddEdit.SupportDocs = JsonConvert.SerializeObject(proposalAddEdit.SupportDocuments);


            proposalAddEdit.Area = schoolDetails.EducationArea != null ? schoolDetails.EducationArea.EducationAreaDesc : string.Empty;

            return proposalAddEdit;
        }

        public List<WorkCategory> GetWorkCategory()
        {
            return _unitOfWork.WorkCategoryRepository.GetAll().Where(x => x.InList == true).ToList();
        }

        public IEnumerable<CallLogFinanceUrgency> GetFinanceUrgency()
        {
            return _unitOfWork.CallLogFinanceUrgencyRepository.GetAll();
        }

        public IEnumerable<ProposalStatus> GetProposalStatuses()
        {
            return _unitOfWork.ProposalStatusRepository.GetAll();
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

        public IEnumerable<SupportDocumentType> GetSupportDocumentTypes()
        {
            return _unitOfWork.SupportDocumentTypeRepository.GetAll();
        }

        public IEnumerable<ProposalEndorsementType> GeEndorsementTypes()
        {
            return _unitOfWork.EndorsementTypeRepository.GetAll();
        }

        public bool Save(ProposalAddEditViewModel model, string loggedInUser, out int callLogId)
        {

            _fmProposalsRepository.Save(model, loggedInUser, out callLogId);
            return true;
        }

        public ProposalAddEditViewModel GetProposalDetails(int? id, int agencyId)
        {

            return _fmProposalsRepository.GetDetailsById(id, agencyId);
        }

        public bool Update(ProposalAddEditViewModel model, string loggedInUser)
        {
            return _fmProposalsRepository.Update(model, loggedInUser);
        }

        public AgencyContractor GetAgencyContractor(int agencyId)
        {
            return _agencyService.GetById(agencyId).AgencyContractor;
        }

        public void DeleteProposal(int? callOutId, int? agencyId)
        {
            _fmProposalsRepository.DeleteProposal(callOutId, agencyId);
        }


        public IEnumerable<WorkType> GetWorkTypes()
        {
            return _unitOfWork.WorkTypeRepository.GetAll().ToList();
        }


        public List<EmailKeyword> GetEmailKeywords()
        {
            return _unitOfWork.EmailKeywordRepository.GetAll().Where(x => x.KeywordTypeID == 1).ToList();
        }

        public IEnumerable<ProposalProjectType> GetProjectTypes()
        {
            return _unitOfWork.ProjectTypeRepository.GetAll();
        }
        public IEnumerable<ProposalWorkNominated> GetWorkNominated()
        {
            return _unitOfWork.WorkNominatedRepository.GetAll();
        }
        public IEnumerable<RegionalContact> GetRegionalContact()
        {
            return _unitOfWork.RegionalContactRepository.GetAll().Where(x => x.RoleId == "SO" ).ToList();
        }
        public IEnumerable<ProposalProgram> GetPrograms()
        {
            return _unitOfWork.ProposalProgramRepository.GetAll().Where(x => x.Active == true);
        }
        public IEnumerable<ProposalSubProgram> GetSubPrograms(int? programId = 0)
        {
            return _unitOfWork.ProposalSubProgramRepository.GetAll().Where(x => x.Active == true && x.ProposalProgramID == programId);
        }

        public IEnumerable<ProposalPMMProfile> GetPMMProfiles()
        {
            return _unitOfWork.PMMProfileRepository.GetAll();
        }

        public IEnumerable<ProposalSFC> GetProposalSFC()
        {
            return _unitOfWork.ProposalSFCRepository.GetAll();
        }

        public IEnumerable<ProposalProcurement> GetProposalProcurement()
        {
            return _unitOfWork.ProposalProcurementRepository.GetAll().Where(x => x.Active == true);
        }
        public IEnumerable<ProposalProcurement> GetAllProposalProcurement()
        {
            return _unitOfWork.ProposalProcurementRepository.GetAll();
        }
        public IEnumerable<RegionalContact> GetPMASO()
        {
            return _unitOfWork.RegionalContactRepository.GetAll();
        }


        public List<ProposalEndorsementType> GetEndorsementTypes()
        {
            return _unitOfWork.EndorsementTypeRepository.GetAll().ToList();
        }

        public ProposalRoles GetProposalRoles(int agencyID, int propertyID)
        {
            return _fmProposalsRepository.GetProposalRoles(agencyID, propertyID);
        }

        public ItemsCounts GetProposalCounts(int agencyID, int propertyID, string RoleID)
        {
            var userPropertyIds = _userService.GetByUserCode(_baseService.GetCurrentUser().UserCode, agencyID).UserPropertyIds;
            return _fmProposalsRepository.GetProposalCounts(agencyID, propertyID, RoleID, userPropertyIds);
        }

        public IEnumerable<ProposalSubProgram> GetSubProgramList(int programId)
        {
            var subPrograms = from pp in _unitOfWork.ProposalProgramRepository.GetAll()
                      join psp in _unitOfWork.ProposalSubProgramRepository.GetAll() on pp.ProposalProgramID equals psp.ProposalProgramID
                      where psp.ProposalProgramID == programId && psp.Active == true
                      orderby psp.SubProgramName
                      select psp;
            return subPrograms.ToList();
        }
    }
}
