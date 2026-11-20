using AssetCare.Common;
using AssetCare.Controllers;
using AssetCare.Helpers;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Service;
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
    [RoutePrefix("api/CallOut")]
    public class CallOutApiController : BaseApiController
    {
        private readonly ICalloutService _fmCallOutService;
        private readonly IEquipmentService _equipmentService;
        private readonly ILocalContractorService _localContractorService;
        private readonly IPropertyService _propertyService;
        private readonly IDocumentDownloadHelper _documentDownloadHelper;
        private readonly IMapper _mapper;

        public CallOutApiController(ICalloutService fmCallOutService, IEquipmentService equipmentService, ILocalContractorService localContractorService, IPropertyService propertyService, IDocumentDownloadHelper documentDownloadHelper, IMapper mapper, IBaseService baseService) : base(baseService)
        {
            _fmCallOutService = fmCallOutService;
            _equipmentService = equipmentService;
            _localContractorService = localContractorService;
            _propertyService = propertyService;
            _documentDownloadHelper = documentDownloadHelper;
            _mapper = mapper;
        }

        [HttpPost]
        [Route("filter")]
        public IHttpActionResult Filter(CalloutFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            IEnumerable<CallOutViewModel> result = GetFilteredData(filters, false, out filteredResultsCount, out totalResultsCount);
            var resultData = new
            {
                draw = filters.draw,
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
            };
            return Json(resultData);
        }

        [HttpPost]
        [Route("filterForHP")]
        [AuthorizeAPIOverride(ReferredPages = "TaskList")]
        public IHttpActionResult FilterForHP(CalloutFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            IEnumerable<CallOutForHPViewModel> result = GetFilteredDataForHP(filters, false, out filteredResultsCount, out totalResultsCount);
            var resultData = new
            {
                draw = filters.draw,
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result
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
        [HttpPost]
        [Route("Upload")]
        [AuthorizeAPIOverride(Roles = "ADMIN,CONTRACTOR,PROJECTDIR,PROJECTMNGR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,SUPERREP,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,SUPPORT")]
        public HttpResponseMessage Upload([ModelBinder(typeof(SupportDocumentViewModelBinder))] SupportDocumentViewModel model)
        {
            return DocumentHelper.UploadFile(this, _propertyService, _fmCallOutService, _equipmentService, "CallOut", model);
        }


        [HttpPost]
        [Route("DeleteCallOut")]
        [AuthorizeAPIOverride(Roles = "Admin")]
        public IHttpActionResult DeleteCallOut([FromUri] int? callOutId, int? agencyId)
        {
            var deleted = false;
            if (callOutId.HasValue)
            {
                _fmCallOutService.DeleteCallOut(callOutId, agencyId);
            }
            var resultData = new
            {
                OK = deleted
            };
            return Json(resultData);
        }

        [HttpPost]
        [Route("Delete")]
        [AuthorizeAPIOverride(Roles = "ADMIN,PROJECTMNGR,PROJECTDIR,CONTRACTOR,CALLCENTERSTAFF,LOCALCONTRACTOR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM")]
        public IHttpActionResult DeleteDocument([FromUri] string token)
        {
            var deleted = false;

            if (!string.IsNullOrEmpty(token))
            {

                deleted = _documentDownloadHelper.DeleteDocument(
                    GetAgencyId(),
                    token,
                    _baseService.GetCurrentUser().UserID);
            }

            return Json(new { OK = deleted });
        }

        [HttpGet]
        [Route("GetSubContractorDetails")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,PROJECTMNGR,PROJECTDIR,SCHOOL,SCHOOLMGR,DETAMU,DETAMCU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM")]
        public IHttpActionResult GetSubContractorDetails(int id)
        {
            SubContractor subbie = _fmCallOutService.GetSubContractortails(id);
            return Json(subbie);
        }

        [HttpGet]
        [Route("GetLCWOCount")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,PROJECTMNGR,PROJECTDIR,SCHOOL,SCHOOLMGR,DETAMU,DETAMCU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,DOEUSER,SINSW")]
        public IHttpActionResult GetLCWOCount(int id)
        {
            LCWOViewModel lcWOcount = _fmCallOutService.GetLCWOCount(id);
            return Json(lcWOcount);
        }

        [HttpPost]
        [Route("GetTradesPeopleList")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,PROJECTMNGR,PROJECTDIR,SCHOOL,SCHOOLMGR,DETAMU,DETAMCU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM,DOEUSER,SINSW")]
        public IHttpActionResult GetTradesPeopleList(CalloutFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            TradieResponse result = GetTradesPeopleList(filters, out filteredResultsCount, out totalResultsCount);
            var resultData = new
            {
                draw = filters.draw,
                recordsTotal = totalResultsCount,
                recordsFiltered = filteredResultsCount,
                data = result.Data,
                hasValidTradie = result.HasValidTradie
            };

            return Json(resultData);
        }

        private TradieResponse GetTradesPeopleList(CalloutFilterPostModel filters, out int filteredResultsCount, out int totalResultsCount)
        {
            //DataTableFilterModel<CalloutFilterPostModel> filterModel = DataTableHelper.TransformDataTablePostModel<CalloutFilterPostModel>(filters);
            DataTableFilterModel<TradieViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<TradieViewModel>(filters);
            CallOutFilterModel callOutFilterModel = new CallOutFilterModel()
            {

                AgencyId = filters.AgencyId,
                PropertyId = filters.PropertyId,
                Workcategory = filters.Workcategory,
                SubContractorID = filters.SubContractorID,

            };
            callOutFilterModel = _mapper.Map(filterModel, callOutFilterModel);
            TradieResponse result = _fmCallOutService.GetTradesPeopleList(callOutFilterModel, out filteredResultsCount, out totalResultsCount);
            string filterSearch = filters.search.value;
            
            result.Data = result.Data.Select(x =>
            {
                return x;
            }).ToList();


            return result;
        }

        [HttpPost]
        [Route("SaveWorkOrder")]
        public IHttpActionResult SaveWorkOrder(List<CallOutAddEditViewModel> lstModel)
        {
            bool result = false;
            //if (CurrentUser.RoleID == "A" || CurrentUser.RoleID == "D" || CurrentUser.RoleID == "AO" || CurrentUser.RoleID == "C")
            //{
            foreach (var model in lstModel)
            {
                //if (!string.IsNullOrEmpty(model.NewContractors))
                //{
                //string[] newContractors = model.NewContractors.Split(',');
                //if (newContractors != null && newContractors.Length > 0)
                //{
                //    foreach (string row in newContractors)
                //    {
                //        string[] rowData = row.Split(':');
                //        if (rowData[0] != null && rowData[0].Length > 0)
                //        {
                //int contractorID = Convert.ToInt32(rowData[0]);
                //string comments = rowData[1] != null ? rowData[1] : string.Empty;
                //result = _fmCallOutService.ConvertCallOutToWorkOrder(model, model.ContractorID, "loggedInUser", model.WOComments);
                //model.WorkerId = workOrderId;
                //model.WorkOrderComments = model.WOComments;
                //calloutSharedFunctions.SendEmailToContractor(model, CurrentUser, contractorID);

                //}
                // }
                //}
                //}
                //}
            }
            // var result = _fmCallOutService.ConvertCallOutToWorkOrder(model, contractorID, loggedInUser, comments);
            //return Ok(result);
            return Ok(result);

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
        [Route("download")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,PROJECTMNGR,PROJECTDIR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM")]
        public HttpResponseMessage Download(CalloutFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            List<CallOutViewModel> result = GetFilteredData(filters, true, out filteredResultsCount, out totalResultsCount).ToList();
            if (User.IsInRole("school") || User.IsInRole("schoolmgr"))
            {
                var excelData = result.Select(x => new
                {
                    CallOutID = x.CallOutID,
                    CallOutDate = x.CallDate,
                    SchoolName = x.SchoolName,
                    RequestedBy = x.RequestedBy,
                    CalloutDescription = x.Description,
                    Category = x.Category,
                    Status = x.Status,
                    RequestedResponseTime = x.SchoolUrgency
                }).ToList();
                return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "CalloutData");
            }
            else if (User.IsInRoles("ADMIN,AMCUOFFICE,DETAMU,AMUOFFICER,DETAMCU,DIRAM,EDAM,GROUPLEADER,NETDETAMCU,NETDETAMU,NETOFFICER,PMASO,SRGRPLEADER"))
            {
                var excelData = result.Select(x => new
                {
                    CallOutID = x.CallOutID,
                    CallOutDate = x.CallDate,
                    SchoolName = x.SchoolName,
                    RequestedBy = x.RequestedBy,
                    CalloutDescription = x.Description,
                    Category = x.Category,
                    Status = x.Status,
                    Hazmat = x.IsHazmat,
                    WOID = string.Join(",", x.JobIds)
                }).ToList();
                return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "CalloutData");
            }
            else
            {
                var excelData = result.Select(x => new
                {
                    CallOutID = x.CallOutID,
                    CallOutDate = x.CallDate,
                    SchoolName = x.SchoolName,
                    RequestedBy = x.RequestedBy,
                    CalloutDescription = x.Description,
                    Category = x.Category,
                    Status = x.Status,
                    WOID = string.Join(",", x.JobIds)
                }).ToList();
                return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType, "CalloutData");
            }

             
           
        }

        [HttpPost]
        [Route("downloadForHP")]
        [AuthorizeAPIOverride(ReferredPages = "TaskList")]
        public HttpResponseMessage DownloadForHP(CalloutFilterPostModel filters)
        {
            int filteredResultsCount;
            int totalResultsCount;
            var mapSchoolUrgency = new Dictionary<string, string>
            {
                {"1", "Same Day (Urgent)"},
                {"2", "Same Day"},
                {"3", "Next Day"},
                {"4", "Non-Urgent (within 5 days)"}
            };
            List<CallOutForHPViewModel> result = GetFilteredDataForHP(filters, true, out filteredResultsCount, out totalResultsCount).ToList();
            var excelData = result.Select(x => new
            {
                CallOutID = x.CallOutID,
                CallOutDate = x.CallDate,
                SchoolName = x.SchoolName,
                RequestedBy = x.RequestedBy,
                CalloutDescription = x.Description,
                CalloutSummary = x.CallOutUrgencyDesc,
                Status = x.Status,
                RequestedResponseTime = (!string.IsNullOrEmpty(x.SchoolUrgency) && mapSchoolUrgency.ContainsKey(x.SchoolUrgency))
                ? mapSchoolUrgency[x.SchoolUrgency]
                : x.SchoolUrgency
            }).ToList();
            return (new ExcelHelper()).GenerateExcelDocument(excelData, filters.ExportType);
        }

        private IEnumerable<CallOutViewModel> GetFilteredData(CalloutFilterPostModel filters, bool dataForExport, out int filteredResultsCount, out int totalResultsCount)
        {
            DataTableFilterModel<CallOutViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<CallOutViewModel>(filters);

            CallOutFilterModel callOutFilterModel = new CallOutFilterModel()
            {
                AgencyId = filters.AgencyId,
                PropertyId = filters.PropertyId,
                //Workcategory = filters.Workcategory,
                ShowIsReadyForReimbursementOnly = filters.ShowIsReadyForReimbursementOnly
            };
            callOutFilterModel = _mapper.Map(filterModel, callOutFilterModel);
            if (dataForExport) callOutFilterModel.Take = -1; //if it is export, we need all the data
            List<CallOutViewModel> result = _fmCallOutService.FilterCallOutData(callOutFilterModel, dataForExport, out filteredResultsCount, out totalResultsCount).ToList();
            result = result.Select(x =>
            {
                if (x.JobId == null || x.JobId <= 0)
                {
                    x.CanDelete = true;
                }
                return x;
            }).ToList();

            return result;
        }

        private IEnumerable<CallOutForHPViewModel> GetFilteredDataForHP(CalloutFilterPostModel filters, bool dataForExport, out int filteredResultsCount, out int totalResultsCount)
        {
            DataTableFilterModel<CallOutForHPViewModel> filterModel = DataTableHelper.TransformDataTablePostModel<CallOutForHPViewModel>(filters);

            CallOutFilterModel callOutFilterModel = new CallOutFilterModel()
            {
                AgencyId = filters.AgencyId,
                SchoolId = filters.SchoolId,
                Workcategory = filters.Workcategory,
                PropertyId = filters.PropertyId
            };
            callOutFilterModel = _mapper.Map(filterModel, callOutFilterModel);
            if (dataForExport) callOutFilterModel.Take = -1; //if it is export, we need all the data
            List<CallOutForHPViewModel> result = _fmCallOutService.FilterCallOutDataForHP(callOutFilterModel, dataForExport, out filteredResultsCount, out totalResultsCount).ToList();

            return result;
        }

        [HttpGet]
        [Route("GetSubContractors")]
        [AuthorizeAPIOverride(Roles = "ADMIN,SUPPORT,CONTRACTOR,PROJECTDIR,PROJECTMNGR,SCHOOL,SCHOOLMGR,DETAMU,AMUOFFICER,DETAMCU,AMCUOFFICE,MFUMGR,MFUOFFICER,SUPERREP,CALLCENTERSTAFF,LOCALCONTRACTOR,GROUPLEADER,SRGRPLEADER,DIRAM,EDAM")]
        public IHttpActionResult GetSubContractors(int id, int agencyId, int propertyId)
        {
            List<SubContractor> subbies = _localContractorService.GetSubContractors(propertyId, agencyId, id);
            if (subbies != null)
            {
                subbies = subbies.OrderBy(x=>x.Name).OrderByDescending(x=>x.ATSIVerified).ToList(); // Show Aboriginal LC on top of the list
                var subbiesFiltered = subbies.Select(x => new { Text = x.Name, Value = x.ID.ToString() }).ToList();
                return Json(subbiesFiltered);
            }

            return null;
        }
    }
}