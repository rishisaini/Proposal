using AssetCare.Common;
using AssetCare.Controllers;
using AssetCare.Helpers;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Service.Interfaces;
using AutoMapper;
using Elmah;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace AssetCare.Areas.CallOuts.ApiControllers
{
    [RoutePrefix("api/Proposal")]
    public class ProposalApiController : BaseApiController
    {
        private readonly IProposalService _fmProposalService;
        private readonly IUserService _userService;
        private readonly IEquipmentService _equipmentService;
        private readonly IMapper _mapper;
        private readonly IDocumentDownloadHelper _documentDownloadHelper;
        public ProposalApiController(IProposalService fmCallOutService,IUserService userService, IEquipmentService equipmentService, IMapper mapper, IBaseService baseService, IDocumentDownloadHelper documentDownloadHelper) : base(baseService)
        {
            _fmProposalService = fmCallOutService;
            _userService = userService;
            _equipmentService = equipmentService;
            _mapper = mapper;
            _documentDownloadHelper = documentDownloadHelper;
        }

        [HttpPost]
        [Route("filter")]
        public IHttpActionResult Filter(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            IEnumerable<ProposalViewModel> result = GetFilteredData(filters, false, out filteredResultsCount, out totalResultsCount);
            var resultData = new
            {
                draw = filters.draw,
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            };
            return Json(resultData);
        }

        private int GetAgencyId()
        {
            var agencyId = 0;
            if (HttpContext.Current.Session["AgencyDetails"] != null)
            {
                agencyId = ((KeyValuePair<int, string>)HttpContext.Current.Session["AgencyDetails"]).Key;
            }

            return agencyId;
        }

        [HttpPost]
        [Route("Upload")]
        //[AuthorizeAPIOverride(Roles = "ADMIN,CONTRACTOR,PROJECTDIR,PROJECTMNGR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,SUPERREP,CALLCENTERSTAFF,LOCALCONTRACTOR,PMASO")]
        public HttpResponseMessage Upload([ModelBinder(typeof(SupportDocumentViewModelBinder))] SupportDocumentViewModel model)
        {
            int sequenceId = 0;
            var httpRequest = HttpContext.Current.Request;
            int documentId = 0;
            int schoolId = 0;
            string filePath = string.Empty;
            if (httpRequest.Files.Count > 0)
            {
                var docfiles = new List<string>();
                var result = httpRequest.Files.ValidateFiles(PostedFileSettings.DefaultPostedFileSettings);
                if (result.Success)
                {
                    foreach (string file in httpRequest.Files)
                    {
                        var postedFile = httpRequest.Files[file];

                        if (model.PropertyID.HasValue)
                        {
                            Property schDetails = _fmProposalService.GetPropertyDetails(model.PropertyID.Value);
                            schoolId = schDetails == null ? 0 : schDetails.SchoolId.Value;
                        }

                        sequenceId = SaveFile(postedFile, model, schoolId, out filePath, out documentId);
                    }
                }
            }

            if (sequenceId > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK,
                    new { downloadLink = Url.Content("~/") + filePath.Remove(0, 1), filePath = filePath, sequenceId = sequenceId, documentId = documentId });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        private int SaveFile(HttpPostedFile postedFile, SupportDocumentViewModel model, int schoolId, out string filePath, out int documentId)
        {
            int sequenceId = 0;
            documentId = 0;
            var documentBaseURL = ConfigurationManager.AppSettings["SupportDocuments"];
            string folderURL = string.Format("{0}/{1}/{2}/{3}/{4}", documentBaseURL, model.AgencyID, schoolId, model.SupportDocumentClassID, model.SupportDocumentTypeID);
            //creaete folder if not exists
            Directory.CreateDirectory(System.Web.Hosting.HostingEnvironment.MapPath(folderURL));

            check:
            if (File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(string.Format("{0}/{1}", folderURL, model.ClientDocumentName))))
            {
                model.ClientDocumentName = "_" + model.ClientDocumentName;
                goto check;
            }
            filePath = string.Format("{0}/{1}", folderURL, model.ClientDocumentName);
            string fileName = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

            string loggedInUser = RequestContext.Principal.UserCode();

            SupportDocument suportDocument = new SupportDocument()
            {
                AgencyID = model.AgencyID,
                AttachmentText = model.ClientDocumentName,
                AttachmentLink = filePath,
                Deleted = false,
                SupportDocumentClassID = model.SupportDocumentClassID,
                SupportDocumentTypeID = model.SupportDocumentTypeID,
                DocumentSequenceID = model.SupportDocumentID,
                IssueDate = model.IssueDate,
                LastRecordedBy = loggedInUser,
                ClientDocumentName = model.ClientDocumentName,
                PropertyID = model.PropertyID,
                RecordedBy = loggedInUser,
                RecordedDate = DateTime.Now,
                MarkAsDeleted = false,
                RoleID = model.RoleID,
            };
            try
            {
                sequenceId = _fmProposalService.SaveDocument(suportDocument, model.MasterRecordID, out documentId);
                // Save file
                postedFile.SaveAs(fileName);

                if (model.SupportDocumentClassID == (int)SupportDocumentClassEnum.EQP ||
                model.SupportDocumentClassID == (int)SupportDocumentClassEnum.DEQP)
                {
                    _equipmentService.SaveUploadedDate(model.MasterRecordID);
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return sequenceId;
        }

        [HttpPost]
        [Route("download")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
        public HttpResponseMessage Download(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            List<ProposalForHPViewModel> result = GetFilteredProposalForEndorsement(filters, true, out filteredResultsCount, out totalResultsCount,"home").ToList();
            var excelData = result.Select(x => new
            {
                Id = x.Id,
                ProposalDate = x.ProposalDate,
                SchoolName = x.SchoolName,
                RequestedBy = x.RequestedBy,
                Description = x.Description,
                WorkNominatedBy = x.WorkNominatedBy,
                ProposalStatus = x.ProposalStatus,
                ApprovalStatus = x.ApprovalStatus,
                ApprovedBy = x.ApprovedBy,
                ApprovalDate = x.ApprovalDate,
                AwaitingActionBy = x.AwaitingActionBy,
                LastApproved= x.ApprovalDate,
                DaysSinceLogged = x.DaysSinceLogged
            }).ToList();
            return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "ProposalsList");
        }

        [HttpPost]
        [Route("downloadHP")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
        public HttpResponseMessage DownloadHP(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            List<ProposalForHPViewModel> result = GetFilteredProposalForHP(filters, true, out filteredResultsCount, out totalResultsCount).ToList();
            var excelData = result.Select(x => new
            {
                Id = x.Id,
                ProposalDate = x.ProposalDate,
                SchoolName = x.SchoolName,
                RequestedBy = x.RequestedBy,
                Description = x.Description,
                WorkCategory = x.Description,
                ApprovalStatus = x.ApprovalStatus,
                ApprovedBy = x.ApprovedBy,
                ApprovalDate = x.ApprovalDate,
                AwaitingActionBy = x.AwaitingActionBy,
                LastApproved = x.ApprovalDate,
                DaysSinceLogged = x.DaysSinceLogged
            }).ToList();
            return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "ProposalsList");
        }

        [HttpPost]
        [Route("downloadProposalEndorsementHP")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
        public HttpResponseMessage DownloadProposalEndorsementHP(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            List<ProposalForHPViewModel> result = GetFilteredProposalForEndorsement(filters, true, out filteredResultsCount, out totalResultsCount,"").ToList();
            var excelData = result.Select(x => new
            {
                Id = x.Id,
                ProposalDate = x.ProposalDate,
                SchoolName = x.SchoolName,
                RequestedBy = x.RequestedBy,
                Description = x.Description,
                WorkCategory = x.Description,
                ApprovalStatus = x.ApprovalStatus,
                ApprovedBy = x.ApprovedBy,
                ApprovalDate = x.ApprovalDate,
                AwaitingActionBy = x.AwaitingActionBy,
                LastApproved = x.ApprovalDate,
                DaysSinceLogged = x.DaysSinceLogged
            }).ToList();
            return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "ProposalsList");
        }

        public static double GetBusinessDays(DateTime startD, DateTime endD)
        {
            double calcBusinessDays =
                1 + ((endD - startD).TotalDays * 5 -
                (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if (endD.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (startD.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            return (int)calcBusinessDays;
        }

        private IEnumerable<ProposalViewModel> GetFilteredData(ProposalFilterPostModel filters, bool dataForExport, out int filteredResultsCount, out int totalResultsCount)
        {
            DataTableFilterModel<ProposalViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<ProposalViewModel>(filters);

            ProposalFilterModel proposalFilterModel = new ProposalFilterModel()
            {
                AgencyId = filters.AgencyId,
                PropertyId = filters.PropertyId,
                Workcategory = filters.Workcategory,
                GrantApplication = filters.GrantApplication
            };
            proposalFilterModel = _mapper.Map(filterModel, proposalFilterModel);
            if (dataForExport) proposalFilterModel.Take = -1; //if it is export, we need all the data
            List<ProposalViewModel> result = _fmProposalService.FilterProposalData(proposalFilterModel, dataForExport, out filteredResultsCount, out totalResultsCount).ToList();
            string filterSearch = filters.search.value;
           
            result = result.Select(x =>
            {
                if (x.JobId == null || x.JobId <= 0)
                {
                    x.CanDelete = true;
                }
                return x;
            }).ToList();
            var vDays = proposalFilterModel.ColumnSearches.Select(x => x.Key == "DaysSinceLogged").FirstOrDefault();
            foreach (var row in result)
            {
                //row.CreateProject = row.ProjectID ==null? row.AwaitingActionBy: "";
               // row.ApprovalStatus = row.AwaitingActionBy == "None" ? "Approved" : row.CalllogFitsID != null ? "Approved" : row.ApprovalStatus;
                row.AwaitingActionBy = row.AwaitingActionBy == "None" ? "" : row.CalllogFitsID != null ? "" : row.AwaitingActionBy;
                if(!vDays)
                    row.DaysSinceLogged = (int)GetBusinessDays(row.ProposalDate ?? DateTime.Now, DateTime.Now);
                row.WorkNominatedBy = row.WorkNominatedBy;
            }
            //if (filters.search.value != null)
            //{
            //    result = result.FindAll(f => f.ProposalID.ToString().Contains(filterSearch.ToLower()) || f.Id.ToString().Contains(filterSearch.ToLower()) ||
            //                                f.ProposalDate.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower()) ||
            //                                f.SchoolName.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                f.RequestedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                f.Description.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                f.WorkNominatedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                //f.ApprovalStatus.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                f.ApprovedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                (f.ApprovalDate.HasValue &&  f.ApprovalDate.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower())) ||
            //                                f.AwaitingActionBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                (f.LastApproved.HasValue && f.LastApproved.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower())) ||
            //                                f.DaysSinceLogged.ToString().ToLower().Contains(filterSearch.ToLower()) ||
            //                                f.ProposalStatus.ToString().ToLower().Contains(filterSearch.ToLower()
            //                                ));

                
            //}
            if (filters.columns[13].search.value!=null)//DaysSinceLogged
            {
                if(!vDays)
                    result = result.FindAll(x=>x.DaysSinceLogged.ToString().Contains(filters.columns[13].search.value));
            }
            if (filters.columns[8].search.value != null)//ApprovalStatus
            {
               // result = result.FindAll(x => x.ApprovalStatus.ToString().ToLower().Contains(filters.columns[8].search.value.ToLower()));
            }
            return result.ToList(); ;
        }

        private IEnumerable<ProposalForHPViewModel> GetFilteredProposalForHP(ProposalFilterPostModel filters, bool dataForExport, out int filteredResultsCount, out int totalResultsCount)
        {
            DataTableFilterModel<ProposalForHPViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<ProposalForHPViewModel>(filters);
            var userDetails = _userService.GetByUserCode(User.UserCode(), filters.AgencyId.HasValue ? filters.AgencyId.Value : 0);
            ProposalFilterModel proposalFilterModel = new ProposalFilterModel()
            {
                AgencyId = filters.AgencyId,
                SchoolId = filters.SchoolId,
                Workcategory = filters.Workcategory
            };
            proposalFilterModel = _mapper.Map(filterModel, proposalFilterModel);
            if (dataForExport) proposalFilterModel.Take = -1; //if it is export, we need all the data
            List<ProposalForHPViewModel> result = _fmProposalService.FilterProposalDataForHP(proposalFilterModel, dataForExport, out filteredResultsCount, out totalResultsCount).ToList();
            foreach (var row in result)
            {
                row.ApprovalStatus = row.AwaitingActionBy == "None" ? "Approved" : row.ApprovalStatus;
                row.AwaitingActionBy = row.AwaitingActionBy == "None" ? "" : row.AwaitingActionBy;
                row.DaysSinceLogged = (int)GetBusinessDays(row.ProposalDate ?? DateTime.Now, DateTime.Now);
                row.WorkNominatedBy = row.WorkNominatedBy;
            }
            string filterSearch = filters.search.value;
            if (filters.search.value != null)
            {
                result = result.FindAll(f => f.Id.ToString().Contains(filterSearch.ToLower()) || f.Id.ToString().Contains(filterSearch.ToLower()) ||
                                            f.ProposalDate.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower()) ||
                                            f.SchoolName.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.RequestedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.Description.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.WorkNominatedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.ApprovalStatus.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.ApprovedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            (f.ApprovalDate.HasValue && f.ApprovalDate.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower())) ||
                                            f.AwaitingActionBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            (f.LastApproved.HasValue && f.LastApproved.Value.ToString("dd-MM-yyyy").Contains(filterSearch.ToLower())) ||
                                            f.DaysSinceLogged.ToString().ToLower().Contains(filterSearch.ToLower()
                                            ));


            }
            if (filters.columns[12].search.value != null)//DaysSinceLogged
            {
                result = result.FindAll(x => x.DaysSinceLogged.ToString().ToLower().Contains(filters.columns[12].search.value.ToLower()));
            }
            if (filters.columns[7].search.value != null)//ApprovalStatus
            {
                result = result.FindAll(x => x.ApprovalStatus.ToString().ToLower().Contains(filters.columns[7].search.value.ToLower()));
            }

            return result;
        }

        [HttpPost]
        [Route("filteredProposalForHP")]
        public IHttpActionResult FilteredJobsForHP(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
           
            IEnumerable<ProposalForHPViewModel> result = GetFilteredProposalForHP(filters, false, out filteredResultsCount, out totalResultsCount);
            var resultData = new
            {
                draw = "",
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            };
            return Json(resultData);
        }

        [HttpPost]
        [Route("filteredProposalForEndorsement")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
        public IHttpActionResult FilteredJobsForEndorsement(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;

            IEnumerable<ProposalForHPViewModel> result = GetFilteredProposalForEndorsement(filters, false, out filteredResultsCount, out totalResultsCount,"");
            var resultData = new
            {
                draw = "",
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            };
            return Json(resultData);
        }

        [HttpPost]
        [Route("filteredProposalForEndorsementSHP")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,SCHOOL,SCHOOLMGR,SCHOOLDIR,EXECDIRECTOR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,PMASO")]
        public IHttpActionResult FilteredJobsForEndorsementSHP(ProposalFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;

            IEnumerable<ProposalForHPViewModel> result = GetFilteredProposalForEndorsement(filters, false, out filteredResultsCount, out totalResultsCount, "home");
            var resultData = new
            {
                draw = "",
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            };
            return Json(resultData);
        }

       

        private IEnumerable<ProposalForHPViewModel> GetFilteredProposalForEndorsement(ProposalFilterPostModel filters, bool dataForExport, out int filteredResultsCount, out int totalResultsCount,string location)
        {
            DataTableFilterModel<ProposalForHPViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<ProposalForHPViewModel>(filters);
            var userDetails = _userService.GetByUserCode(User.UserCode(), filters.AgencyId.HasValue ? filters.AgencyId.Value : 0);
            ProposalFilterModel proposalFilterModel = new ProposalFilterModel()
            {
                AgencyId = filters.AgencyId,
                SchoolId = filters.SchoolId,
                Workcategory = filters.Workcategory,
                SchoolName = filters.SchoolName,
               GrantApplication = filters.GrantApplication
            };
            proposalFilterModel = _mapper.Map(filterModel, proposalFilterModel);
            if (dataForExport) proposalFilterModel.Take = -1; //if it is export, we need all the data
            List<ProposalForHPViewModel> result = _fmProposalService.FilterProposalDataForEndorsement(proposalFilterModel, dataForExport, out filteredResultsCount, out totalResultsCount,location).ToList();
            foreach (var row in result)
            {
                row.ApprovalStatus = row.AwaitingActionBy == "None" ? "Approved" : row.ApprovalStatus;
                row.AwaitingActionBy = row.AwaitingActionBy == "None" ? "" : row.AwaitingActionBy;
                row.DaysSinceLogged = (int)GetBusinessDays(row.ProposalDate ?? DateTime.Now, DateTime.Now);
                row.WorkNominatedBy = row.WorkNominatedBy;

            }
            string filterSearch = filters.search.value;
            if (filters.search.value != null)
            {
                result = result.FindAll(f => f.ProposalID.ToString().Contains(filterSearch.ToLower()) ||
                                             f.ProposalDateList.ToString().Contains(filterSearch.ToLower()) ||
                                            f.SchoolName.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.RequestedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.Description.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.WorkNominatedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.ApprovalStatus.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.ApprovedBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.ApprovalDate.ToString().Contains(filterSearch.ToLower()) ||
                                            f.AwaitingActionBy.ToString().ToLower().Contains(filterSearch.ToLower()) ||
                                            f.LastApprovedList.ToString().Contains(filterSearch.ToLower()) ||
                                            f.DaysSinceLogged.ToString().ToLower().Contains(filterSearch.ToLower()
                                            ));


            }
            if (filters.columns[12].search.value != null)//DaysSinceLogged
            {
                result = result.FindAll(x => x.DaysSinceLogged.ToString().Contains(filters.columns[12].search.value));
            }
            if (filters.columns[7].search.value != null)//ApprovalStatus
            {
                result = result.FindAll(x => x.ApprovalStatus.ToString().Contains(filters.columns[7].search.value));
            }
            return result;
        }

        [HttpPost]
        [Route("DeleteProposal")]
        [AuthorizeAPIOverride(Roles = "Admin")]
        public IHttpActionResult DeleteProposal([FromUri] int? proposalId, int? agencyId)
        {
            var deleted = false;
            if (proposalId.HasValue)
            {
                _fmProposalService.DeleteProposal(proposalId, agencyId);
            }
            var resultData = new
            {
                OK = deleted
            };
            return Json(resultData);
        }

        [HttpGet]
        [Route("Downloadfile")]
        [AuthorizeAPIOverride(Roles = "ADMIN,CONTRACTOR,PROJECTDIR,PROJECTMNGR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,SUPERREP,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,SUPPORT")]
        public IHttpActionResult DownloadFile(string token, int agencyId = 0)
        {
            return _documentDownloadHelper.GetFileDownloadResult(token, agencyId, _baseService.GetCurrentUser().UserID).ToHttpActionResult(this);
        }
    }
}

