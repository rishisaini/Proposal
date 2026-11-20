using AssetCare.Model.Common;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Repository.Interfaces;
using AutoMapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace AssetCare.Repository
{
    public partial class FMCallOutRepository : GenericRepository<CallLogFITS>, IFMCallOutRepository
    {
        private readonly IMapper _mapper;
        private static readonly ConcurrentDictionary<int, object> _agencyLocks = new ConcurrentDictionary<int, object>();
        public FMCallOutRepository(AssetCareContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public int GetNextSequenceId(int agencyId)
        {
            var connection = _context.Database.Connection; // EF6 way
            bool shouldClose = false;

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                shouldClose = true;
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
            DECLARE @NextId INT;
            UPDATE AgencySequenceIdCounters
            SET @NextId = NextSequenceVal = NextSequenceVal + 1
            WHERE AgencyId = @AgencyId and SequenceType='NextCalloutId';
            SELECT @NextId;";

                var param = cmd.CreateParameter();
                param.ParameterName = "@AgencyId";
                param.Value = agencyId;
                cmd.Parameters.Add(param);

                // attach current EF transaction
                if (_context.Database.CurrentTransaction != null)
                    cmd.Transaction = _context.Database.CurrentTransaction.UnderlyingTransaction;

                int nextId = (int)cmd.ExecuteScalar();

                if (shouldClose)
                    connection.Close();

                return nextId;
            }
        }

        public bool Save(CallOutAddEditViewModel model, string loggedInUser, out int callLogId)
        {
            CallLogFITS dataToSave = _mapper.Map<CallLogFITS>(model); // MapCallOutModelToEntity(model);
            dataToSave.RecordedDate = DateTime.Now;
            dataToSave.LastRecordedBy = loggedInUser;
            dataToSave.RecordedBy = loggedInUser;
            dataToSave.CallLogDesc = dataToSave.CallLogDesc.Trim();
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try { 
                    dataToSave.AgencySequenceID = GetNextSequenceId(model.AgencyID);
                    _context.CallLogFITS.Add(dataToSave);
                    _context.SaveChanges();

                callLogId = (int)dataToSave.AgencySequenceID;
                model.AgencySequenceID = callLogId;
                model.CallLogFITSID = dataToSave.CallLogFITSID;
                //Save Buildings
                foreach (int buildingId in model.JobBuildingIDs)
                {
                    CallLogBuildingFITS buildings = new CallLogBuildingFITS()
                    {
                        AgencyID = model.AgencyID,
                        PropertyID = model.PropertyID, //here it is property Id
                        CallLogFITSID = dataToSave.CallLogFITSID,
                        BuildingID = buildingId,
                        LastUpdated = DateTime.Now
                    };
                    _context.CallLogBuildingFITSs.Add(buildings);
                    //_context.SaveChanges();
                }

                int documentSequenceNumber = 0;
                //update Support Documents with the calllog Id
                foreach (SupportDocumentViewModel doc in model.SupportDocuments)
                {
                    var supportDocument = _context.SupportDocument.Where(x => x.SupportDocumentID == doc.SupportDocumentID).FirstOrDefault();
                    documentSequenceNumber++;
                    supportDocument.MasterRecordID = model.CallLogFITSID;
                    supportDocument.DocumentSequenceID = documentSequenceNumber;
                    //_context.SaveChanges();

                }
                    _context.SaveChanges();
                    dbContextTransaction.Commit();
            }
            catch
            {
                dbContextTransaction.Rollback();
                throw;
            }
        }

            return true;
        }

        public bool Update(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            HelperMethods helper = new HelperMethods();
            //CallLogFITS dataToSave = MapCallOutModelToEntity(model);

            bool workCategoryChanged = false;
            int existingWorkCategory = 0;
            using (_context)
            {
                using (var dbContextTransaction = _context.Database.BeginTransaction())
                {
                    CallLogFITS result = _context.CallLogFITS.SingleOrDefault(x => x.CallLogFITSID == model.CallOutId && x.AgencyID == model.AgencyID);
                    //var wasReadyForIRCMPayment = result.IsReadyForIRCMPayment;
                    if (result != null)
                    {
                        UpdateCapitalDefectUserCapabilities(result, currentUser, ref model);

                        helper.updateChangedFields(model.UserCapabilities.MainSection, model, result);
                        helper.updateChangedFields(model.UserCapabilities.CallCenterSection, model, result);
                        helper.updateChangedFields(model.UserCapabilities.IRCMSection, model, result);

                        result.HAZMAT = model.HAZMAT;
                        result.EmailAlertTypes = model.EmailAlertTypes;

                        result.LastRecordedBy = currentUser.UserCode;
                        result.LastRecordedDate = DateTime.Now;
                        workCategoryChanged = result.WorkCategoryID != model.WorkCategoryID;
                        existingWorkCategory = result.WorkCategoryID.Value;

                        if(model.CapitalDefect)
                        { 
                            result.WorkCategoryID = 2; // WorkCategoryID is 2
                        }
                        if (model.ProposalId > 0)
                        {
                            result.WorkCategoryID = 100;//Default to Proposal when LRLV exists. Coming from Proposal
                        }
                        
                        _context.SaveChanges();

                        //if ((!wasReadyForIRCMPayment.HasValue || 
                        //      wasReadyForIRCMPayment.HasValue && !wasReadyForIRCMPayment.Value) &&
                        //    result.IsReadyForIRCMPayment.HasValue &&
                        //    result.IsReadyForIRCMPayment.Value)
                        //{
                        //    UpdateClaimStatusForInsurance(_context, result, loggedInUser);
                        //}
                    }

                    List<int> existingBuildingIds = _context.CallLogBuildingFITSs.Where(x => x.CallLogFITSID == result.CallLogFITSID && x.Deleted != true).Select(y => y.BuildingID).ToList();
                    List<int> deletedBuildingIds = existingBuildingIds.Except(model.JobBuildingIDs).ToList();
                    List<int> newBuildingIds = model.JobBuildingIDs.Except(existingBuildingIds).ToList();
                    foreach (int delBuilding in deletedBuildingIds)
                    {
                        var deletedBuilding = _context.CallLogBuildingFITSs.Where(x => x.CallLogFITSID == result.CallLogFITSID &&
                       x.AgencyID == model.AgencyID && x.PropertyID == model.PropertyID &&
                       x.BuildingID == delBuilding && x.Deleted != true).FirstOrDefault();
                        if (deletedBuilding != null)
                        {
                            deletedBuilding.LastUpdated = DateTime.Now;
                            deletedBuilding.Deleted = true;
                            _context.SaveChanges();
                        }
                    }

                    foreach (int buildingId in newBuildingIds)
                    {
                        //if not available, add it.
                        if (!(_context.CallLogBuildingFITSs.Where(x => x.CallLogFITSID == result.CallLogFITSID &&
                         x.AgencyID == model.AgencyID && x.PropertyID == model.PropertyID &&
                         x.BuildingID == buildingId && x.Deleted != true).Any()))
                        {
                            CallLogBuildingFITS buildings = new CallLogBuildingFITS()
                            {
                                AgencyID = model.AgencyID,
                                PropertyID = model.PropertyID,
                                CallLogFITSID = result.CallLogFITSID,
                                BuildingID = buildingId,
                                LastUpdated = DateTime.Now
                            };
                            _context.CallLogBuildingFITSs.Add(buildings);
                            _context.SaveChanges();
                        }
                    }


                    //if workCategory is changed, update all the documents, document class and type
                    if (workCategoryChanged)
                    {
                        List<SupportDocument> docs = _context.SupportDocument
                                                     .Where(x => x.SupportDocumentClassID == ((existingWorkCategory == 13 ? (int)SupportDocumentClassEnum.WORKORDER : existingWorkCategory))
                                                             && x.MasterRecordID == result.CallLogFITSID)
                                                     .ToList();
                        foreach (SupportDocument doc in docs)
                        {
                            var supportDocument = _context.SupportDocument.Where(x => x.SupportDocumentID == doc.SupportDocumentID).FirstOrDefault();
                            if (supportDocument != null)
                            {
                                int currentClassId = supportDocument.SupportDocumentClassID;
                                supportDocument.SupportDocumentClassID = (model.WorkCategoryID == 13 ? (int)SupportDocumentClassEnum.WORKORDER : model.WorkCategoryID.Value);



                                // as the work category is changed we need to change the document type id as well
                                var currentDocType = _context.SupportDocumentType.Where(x => x.SupportDocumentTypeID == doc.SupportDocumentTypeID).FirstOrDefault();
                                if (currentDocType != null)
                                {
                                    var newDocType = _context.SupportDocumentType.Where(x => x.SupportDocumentTypeName == currentDocType.SupportDocumentTypeName &&
                                               x.SupportDocumentClassID == supportDocument.SupportDocumentClassID).FirstOrDefault();
                                    supportDocument.SupportDocumentTypeID = newDocType.SupportDocumentTypeID;
                                }


                                _context.SaveChanges();
                            }
                        }
                    }

                    dbContextTransaction.Commit();
                }
            }
            return true;
        }

        public TaskListViewModel GetCalloutCounts(int agencyID, int propertyID, List<int> userPropertyIds, string roleID, int DefaultContractorID)
        {
            int serviceReportACID = 0;
            string serviceReportACIDClaim = "0";
            switch (roleID)
            {
                case "LOCALCONTRACTOR":
                    serviceReportACID = 2;
                    serviceReportACIDClaim = "3,6,9";
                    break;
                case "CONTRACTOR":
                    serviceReportACID = 4;
                    serviceReportACIDClaim = "7";
                    break;
                case "DETAMU":
                case "AMUOFFICER":
                case "DETAMCU":
                case "GROUPLEADER":
                case "SRGRPLEADER":
                case "DIRAM":
                case "EDAM":
                    serviceReportACID = 5;
                    serviceReportACIDClaim = "8";
                    break;
                case "MFUMGR":
                case "MFUOFFICER":
                    serviceReportACID = 5;
                    serviceReportACIDClaim = "8";
                    break;

                default:
                    break;
            }

            List<int?> lstServiceReportACIDClaim = new List<int?>();
            lstServiceReportACIDClaim = serviceReportACIDClaim
                                .Split(',')
                                .Where(x => int.TryParse(x, out _))
                                .Select(int.Parse)
                                .ToList().ConvertAll<int?>(x => x);

            lstServiceReportACIDClaim = lstServiceReportACIDClaim.Where(i => i != 0).ToList();

            using (var dbContextTransaction = _context.Database.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var searchResults = _context.CallLogFITS.Where(x => x.AgencyID == agencyID &&
                                (x.DeletedYN == null || x.DeletedYN == false) &&
                                (propertyID > 0 ? (x.PropertyID == propertyID) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                                ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                               (x.IsInsuranceEvent == true || x.IsInsuranceEventByWO == true) : (x.CallLogStatusID != 2)) &&
                                ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                    (x.IsInsuranceEvent == true || x.IsInsuranceEventByWO == true) : true)
                                 );

                var searchReactiveWO = _context.ServiceReports.Where(x => x.AgencyId == agencyID &&
                            (propertyID > 0 ? (x.PropertyId == propertyID) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyId.Value) : true))
                            && (roleID.Equals("LOCALCONTRACTOR") ? x.SubContractorId == DefaultContractorID : true)
                            && ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                                   (x.CallLogFITS != null) && (x.CallLogFITS.IsInsuranceEvent == true || x.CallLogFITS.IsInsuranceEventByWO == true) : (serviceReportACID > 0 ? x.ServiceReportACID == serviceReportACID : true)) &&
                                    ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                        (x.CallLogFITS != null) && (x.CallLogFITS.IsInsuranceEvent == true || x.CallLogFITS.IsInsuranceEventByWO == true) : true)
                            && ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                                    (x.CallLogFITS != null) && (x.CallLogFITS.IsInsuranceEvent == true || x.CallLogFITS.IsInsuranceEventByWO == true) : (x.ServiceReportStatusId != 9))
                            );

                var searchReactiveWOClaim = _context.ServiceReports.Where(x => x.AgencyId == agencyID &&
                            (propertyID > 0 ? (x.PropertyId == propertyID) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyId.Value) : true))
                             && (roleID.Equals("LOCALCONTRACTOR") ? x.SubContractorId == DefaultContractorID : true)
                            && (lstServiceReportACIDClaim.Count > 0 ? lstServiceReportACIDClaim.Contains(x.ServiceReportACID.Value) : true)
                            && (x.ServiceReportStatusId != 9)
                            && ((roleID.Equals("MFUMGR") || roleID.Equals("MFUOFFICER")) ?
                                                    (x.IsInsuranceEvent == true) : true)

                            );

                TaskListViewModel filtered = new TaskListViewModel()
                {
                    CalloutTotalCount = searchResults.Count(),
                    CalloutInProgressCount = searchResults.Where(x => x.CallLogStatusID == 6 || x.CallLogStatusID == 3).Count(),
                    CalloutNewCount = searchResults.Where(x => x.CallLogStatusID == 1).Count(),
                    CalloutCompleteCount = searchResults.Where(x => x.CallLogStatusID == 4).Count(),
                    //RWOTotalCount = searchReactiveWO.Count(),
                    RWOEstimateReview = searchReactiveWO.Select(x => x.ServiceReportId).Distinct().Count(),
                    RWOClaimReview = searchReactiveWOClaim.Count(),
                    ICalloutTotalCount = searchResults.Count(),
                    IWOTotalCount = searchReactiveWO.Where(x => x.ServiceReportStatusId != 9 && x.ServiceReportStatusId != 3).Select(x => x.ServiceReportId).Distinct().Count(),
                    IRCalloutTotalCount = searchResults.Where(x => x.IsReadyForReimbursement == true).Count(),
                    IPWOTotalCount = searchReactiveWO.Where(x => x.ServiceReportStatusId == 11).Select(x => x.ServiceReportId).Distinct().Count()
                };
                filtered.CalloutInProgressCount = filtered.CalloutTotalCount - filtered.CalloutNewCount - filtered.CalloutCompleteCount;
                dbContextTransaction.Commit();

                return filtered;
            }
        }

        public IEnumerable<CallOutViewModel> FilterCallOutData(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, string userRole, out int filteredResultsCount, out int totalResultsCount)
        {
            var ignoreFields = new string[] { };
            var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<CallOutViewModel>(filterModel.ColumnSearches, ignoreFields);

            if (filterModel.PropertyId.HasValue)
            {
                userPropertyIds.Clear();
                userPropertyIds.Add(filterModel.PropertyId.Value);
            }

            IQueryable<CallOutViewModel> searchResults = _context.CallLogFITS.Where(x => x.AgencyID == filterModel.AgencyId
                         && (x.DeletedYN == null || x.DeletedYN == false)
                         && (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)
                         && (filterModel.Workcategory > 0 ? x.WorkCategoryID == filterModel.Workcategory || x.WorkCategoryID == 2 : x.WorkCategoryID > 0) // WorkCategory is Reactive or Capital Defects
                         && ((filterModel.ShowIsReadyForReimbursementOnly == true) ? x.IsReadyForReimbursement == true : true)
                         && ((userRole.Equals("MFUMGR") || userRole.Equals("MFUOFFICER")) ? (x.IsInsuranceEvent == true || x.IsInsuranceEventByWO == true) : true)
                         && (x.CapitalDefect == false) //Do not dispaly callouts with Capital Defect is checked in reactive callout list page
                         )
                     .Select(
                 d => new CallOutViewModel
                 {
                     Id = d.CallLogFITSID,
                     AgencyId = d.AgencyID,
                     CallOutID = d.AgencySequenceID.Value,
                     CallDate = d.CallDate,
                     CallDateList = d.CallDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", d.CallDate).ToString().Length) + SqlFunctions.DateName("dd", d.CallDate) + "-" +
                                                                                SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", d.CallDate).ToString().Length) + SqlFunctions.DatePart("month", d.CallDate) + "-" +
                                                                                SqlFunctions.DateName("yyyy", d.CallDate)
                                                                                : "",
                     StatusID = d.CallLogStatusID.HasValue ? d.CallLogStatusID.Value : 0,
                     SchoolName = d.Property.PropertyDesc,
                     AsbestosHRProperty = d.Property.AsbestosHR,
                     DefectMgmtProperty = d.Property.DefectMgmt,
                     RequestedBy = d.RequestedBy,
                     Description = d.CallLogDesc,
                     //includeFullDesc ? d.CallLogDesc : (String.IsNullOrEmpty(d.CallLogDesc) ? String.Empty :
                     //        (d.CallLogDesc.Length > 50 ? d.CallLogDesc.Substring(0, 50) + "..." : d.CallLogDesc)),
                     WorkCategory = d.WorkCategory.Description,
                     SubContractor = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).Select(x => x.SubContractor.Name).ToList(),
                     Status = d.CallLogStatus.CallLogStatusDesc,
                     //if the Job is Cancelled, Callout is open and user can add another Job
                     JobId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().AgencySequenceId,
                     ServiceReportId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault() != null ? _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().ServiceReportId : (int?)null,
                     JobIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.AgencySequenceId).ToList(),
                     ServiceReportIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.ServiceReportId).ToList(),
                     Category = _context.Proposal.Any(p => p.CallLogFITSID == d.CallLogFITSID)
                        ? "LRLV School Funded"
                        : d.IsSecurityIncident == true
                        ? "Security"
                        : _context.CallLogTypes
                              .Where(x => x.CallLogTypeID == d.CallLogTypeID)
                              .Select(x => x.CallLogTypeDesc)
                              .FirstOrDefault(),
                     CalloutSummary = d.Comments,
                     //includeFullDesc ? d.Comments : (String.IsNullOrEmpty(d.Comments) ? String.Empty :
                     //        (d.Comments.Length > 50 ? d.Comments.Substring(0, 50) + "..." : d.Comments)),
                     SchoolUrgency = _context.CallLogFinanceUrgency.Where(x => x.CallLogFinanceUrgencyID == d.SchoolUrgencyID).OrderByDescending(x => x.CallLogFinanceUrgencyID).FirstOrDefault().CallLogFinanceUrgencyDesc,
                     IsInsuranceEvent = (d.IsInsuranceEvent == true) ? "Yes" : "No",
                     IsReadyForReimbursement = (d.IsReadyForReimbursement == true) ? "Yes" : "No",
                     AssessorStatus = ((d.AssessorStatusID == 1) ? "None" :
                                        ((d.AssessorStatusID == 2) ? "Assessor Appointed" :
                                         ((d.AssessorStatusID == 3) ? "Waive Assessor" :
                                        "N/A"))),
                     IsHazmat = d.HAZMAT,//String.IsNullOrEmpty(d.EmailAlertTypes) ? "No" : "Yes"
                     ClaimNumber = d.ClaimNumber,
                     IncidentDate = d.IncidentDate,
                     IncidentDateList = d.IncidentDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", d.IncidentDate).ToString().Length) + SqlFunctions.DateName("dd", d.IncidentDate) + "-" +
                                                                                SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", d.IncidentDate).ToString().Length) + SqlFunctions.DatePart("month", d.IncidentDate) + "-" +
                                                                                SqlFunctions.DateName("yyyy", d.IncidentDate)
                                                                                : "",
                     SchoolIncidentCause = d.SchoolIncidentCause.SchoolIncidentCauseDesc
                 });


            var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.SchoolName.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.RequestedBy.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Status.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Description.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.SubContractor.Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDateList.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallOutID.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                (f.Category != null && f.Category.ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                f.JobId.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.IsInsuranceEvent.Contains(filterModel.SearchBy.ToLower()) ||
                f.IsReadyForReimbursement.Contains(filterModel.SearchBy.ToLower()) ||
                f.AssessorStatus.Contains(filterModel.SearchBy.ToLower()) ||
                f.IsHazmat.Contains(filterModel.SearchBy.ToLower()) ||
                f.ClaimNumber.Contains(filterModel.SearchBy.ToLower()) ||
                f.IncidentDateList.Contains(filterModel.SearchBy.ToLower()) ||
                f.SchoolIncidentCause.Contains(filterModel.SearchBy.ToLower())
                );

            filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in filterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "CallDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "IncidentDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.IncidentDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "JobId".ToLower())
                {
                    filtered = filtered.Where(f => f.JobId.ToString().Contains(columnSearch.Value));
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (filterModel.Take == -1)
                return filtered;
            return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
        }

        public IEnumerable<CallOutForHPViewModel> FilterCallOutDataForHP(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, string userRole, out int filteredResultsCount, out int totalResultsCount)
        {
            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
            {
                var callOuts = _context.CallLogFITS.Include(x => x.CallLogStatus).Include(x => x.CallLogType).Include(x => x.CallLogFinanceUrgency).Include(x => x.Property).Where(x =>
              x.AgencyID == filterModel.AgencyId);

                // "Where" clause gets applied only if required 
                callOuts = callOuts.Where(x => x.DeletedYN == null || x.DeletedYN == false);
                if (userRole.Equals("MFUMGR") || userRole.Equals("MFUOFFICER"))
                {
                    callOuts = callOuts.Where(x => x.IsReadyForReimbursement == true && x.IsReimbursementComplete != true);
                    callOuts = callOuts.Where(x => x.IsInsuranceEvent == true || x.IsInsuranceEventByWO == true);
                }
                else
                {
                    callOuts = callOuts.Where(x => x.CallLogStatusID == 1);
                }

                if (filterModel.Workcategory > 0)
                {
                    callOuts = callOuts.Where(x => x.WorkCategoryID == filterModel.Workcategory);
                }

                if (filterModel.PropertyId > 0)
                {
                    callOuts = callOuts.Where(x => x.PropertyID == filterModel.PropertyId);
                }
                else if (userPropertyIds.Count > 0)
                {
                    callOuts = callOuts.Where(x => userPropertyIds.Contains(x.PropertyID.Value));
                }


                var model = callOuts.Select(coj => new CallOutForHPViewModel
                {
                    Id = coj.CallLogFITSID,
                    AgencyId = coj.AgencyID,
                    SchoolId = coj.PropertyID,
                    SchoolName = coj.Property.PropertyShortDesc,
                    ASOName = coj.Property.ASOName,
                    ASOContactNumber = coj.Property.ASOContactNumber,
                    CallOutID = coj.AgencySequenceID.Value,
                    CallDate = coj.CallDate,
                    CallOutTypeID = coj.CallLogTypeID,
                    Description = coj.CallLogDesc,
                    CallOutUrgencyID = coj.CallLogFinanceUrgencyID,
                    CallOutUrgencyDesc = coj.CallLogFinanceUrgency.CallLogFinanceUrgencyDesc,
                    Category = _context.Proposal.Any(p => p.CallLogFITSID == coj.CallLogFITSID)
                        ? "LRLV School Funded"
                        : coj.IsSecurityIncident == true
                        ? "Security"
                        : coj.CallLogType.CallLogTypeDesc,
                    SchoolUrgency = coj.SchoolUrgencyID.ToString(),
                    RequestedBy = coj.RequestedBy,
                    Status = coj.CallLogStatus.CallLogStatusDesc,
                    IsInsuranceEvent = coj.IsInsuranceEvent == true ? "Yes" : "No",
                    IsReadyForReimbursement = coj.IsReadyForReimbursement == true ? "Yes" : "No",
                    AssessorStatus = ((coj.AssessorStatusID == 1) ? "None" :
                                            ((coj.AssessorStatusID == 2) ? "Assessor Appointed" :
                                             ((coj.AssessorStatusID == 3) ? "Waive Assessor" :
                                            "N/A"))),
                    ClaimNumber = coj.ClaimNumber,
                    IncidentDate = coj.IncidentDate,
                    IncidentDateList = coj.IncidentDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", coj.IncidentDate).ToString().Length) + SqlFunctions.DateName("dd", coj.IncidentDate) + "-" +
                                                                                SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", coj.IncidentDate).ToString().Length) + SqlFunctions.DatePart("month", coj.IncidentDate) + "-" +
                                                                                SqlFunctions.DateName("yyyy", coj.IncidentDate)
                                                                                : "",
                    SchoolIncidentCause = coj.SchoolIncidentCause.SchoolIncidentCauseDesc,
                    
                });

                if (!string.IsNullOrEmpty(filterModel.SearchBy))
                {
                    var searchLower = filterModel.SearchBy.ToLower();
                    model = model.Where(f =>
                        (f.SchoolName != null && f.SchoolName.ToLower().Contains(searchLower)) ||
                        (f.RequestedBy != null && f.RequestedBy.ToLower().Contains(searchLower)) ||
                        (f.Status != null && f.Status.ToLower().Contains(searchLower)) ||
                        (f.CallOutID.ToString().Contains(searchLower)) ||
                        (f.Category != null && f.Category.ToLower().Contains(searchLower)) ||
                        (f.Description != null && f.Description.ToLower().Contains(searchLower)) ||
                        (f.IsInsuranceEvent != null && f.IsInsuranceEvent.ToLower().Contains(searchLower)) ||
                        (f.IsReadyForReimbursement != null && f.IsReadyForReimbursement.ToLower().Contains(searchLower)) ||
                        (f.AssessorStatus != null && f.AssessorStatus.ToLower().Contains(searchLower)) ||
                        (f.ClaimNumber != null && f.ClaimNumber.ToLower().Contains(searchLower)) ||
                        (f.IncidentDateList != null && f.IncidentDateList.ToLower().Contains(searchLower)) ||
                        (f.SchoolIncidentCause != null && f.SchoolIncidentCause.ToLower().Contains(searchLower)) 
                    );
                }

                foreach (var columnSearch in filterModel.ColumnSearches)
                {
                    if (columnSearch.Key.Equals("CallDate", StringComparison.OrdinalIgnoreCase))
                    {
                        model = model.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                              (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                              f.CallDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.Equals("IncidentDate", StringComparison.OrdinalIgnoreCase))
                    {
                        model = model.Where(f => ((f.IncidentDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                              (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                              f.CallDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }
                    else
                    {
                        var columnSearchValue = columnSearch.Value.ToLower();
                        model = model.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearchValue);
                    }
                }

                model = string.IsNullOrEmpty(filterModel.SortBy)
                    ? model.OrderBy(x => x.Id)
                    : model.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");

                totalResultsCount = callOuts.Count();
                filteredResultsCount = model.Count();

                if (filterModel.Take > 0)
                {
                    model = model.Skip(filterModel.Skip).Take(filterModel.Take);
                }
                transaction.Commit();

                return model.ToList();
            }
        }

        public CallOutAddEditViewModel GetDetailsById(int? id, int agencyId, double estimationThreshold)
        {
            CallLogFITS data = _context.CallLogFITS.Where(x => x.CallLogFITSID == id.Value).FirstOrDefault();
            SubContractor subbie = new SubContractor();
            var callOutData = new CallOutAddEditViewModel()
            {
                AgencySequenceID = data.AgencySequenceID,
                CallLogFITSID = data.CallLogFITSID,
                AgencyID = data.AgencyID.Value,
                PropertyID = data.PropertyID.Value,
                AMUEUR = data.EUR100 ?? false,
                CallDate = data.CallDate,
                IncidentDate = data.IncidentDate,
                CallLogDesc = data.CallLogDesc,
                CallLogTime = data.RecordedDate,
                CallOutId = id,
                CallTakenBy = data.CallTakenBy,
                Complete = data.Complete ?? false,
                EmailDate = data.EmailContractorDate,
                ElementId = data.ElementId,
                EventNo = data.EventNo,
                SchoolIncidentCauseId = data.SchoolIncidentCauseId,
                LocalCotractorToRepairDamage = data.LocalCotractorToRepairDamage,
                LocalCotractorToSecurityIncident = data.IsSecInsuranceEvent,
                CallLogTypeID = data.CallLogTypeID,

                FMCRefNo = data.ContractorWONo,
                Comments = data.Comments,
                CallStatusComments = data.CallStatusComments,
                JobCreatedDate = data.JobCreatedDate,
                Phone = data.Property.Phone,
                PWANotInvolved = data.CommerceNotInvolved ?? false,
                RequestedBy = data.RequestedBy,
                RequesterPosition = data.RequesterPosition,
                RequesterContact = data.RequesterContact,
                RoomLocation = data.RoomLocation,
                RoomNumber = data.RoomNumber,
                SchEUR = data.SchEUR100 ?? false,
                //SchWorkCategory = data.SchoolWorkCategory.Description,
                CallLogFinanceUrgencyID = data.CallLogFinanceUrgencyID ?? 0,
                SelectedCallLogTypeId = data.CallLogTypeID,
                CallLogStatusID = data.CallLogStatusID,
                SelectedEquipmentTypeId = data.ComponentCASId,
                SelectedMaintenanceCallLogId = data.MaintenanceTaskCallLogID,
                //SelectedSchWorkCategoryId = data.SchWorkCategoryID.Value,

                AssemblyGroupId = data.AssemblyGroupId,
                ComponentCASId = data.ComponentCASId,

                TreeSafetyIssue = data.TreeSafetyIssue ?? false,
                WHSIssue = data.OHS ?? false,
                WorkCategoryID = data.WorkCategoryID.HasValue ? data.WorkCategoryID.Value : data.WorkCategoryID,
                LastEmailDate = data.LastEmailDate,
                WorkTypeId = data.WorkTypeId,
                SubContractor = subbie,
                //SubContractorId = subbie.ID,
                SchoolAddress = data.Property.PropertyShortDesc + ", " + data.Property.Street + ", " + data.Property.Suburb + ", " + data.Property.State + ", " + data.Property.Postcode,
                AsbestosHR = data.Property.AsbestosHR,
                DefectMgmt = data.Property.DefectMgmt,
                EmailAlertTypes = data.EmailAlertTypes,
                HAZMAT = data.HAZMAT,
                ProcessedBy = data.ProcessedBy,
                ProcessedDate = data.ProcessedDate,
                IsThirdpartyInvolved = data.IsThirdpartyInvolved,
                CauseOfTheIssue = data.CauseOfTheIssue,
                SchoolUrgencyID = data.SchoolUrgencyID,
                IsInsuranceEvent = data.IsInsuranceEvent,
                IsTotalEstimationOverLimit = IsTotalEstimationOverLimit(id, agencyId, estimationThreshold),
                IsReadyForReimbursement = data.IsReadyForReimbursement,
                IsReimbursementComplete = data.IsReimbursementComplete,
                RoleID = data.RoleID,
                IsInsuranceEventByWO = data.IsInsuranceEventByWO,
                SiteContact = data.SiteContact,
                SiteContactPerson = data.SiteContactPerson,
                ClaimNumber = data.ClaimNumber,
                CORPFileNo = data.CORPFileNo,
                AssessorStatusID = data.AssessorStatusID,
                CommentsByIRCM = data.CommentsByIRCM,
                ProcessedByIRCM = data.ProcessedByIRCM,
                ProcessedDateByIRCM = data.ProcessedDateByIRCM,
                TotalEstimationOverLimitNotificationDate = data.TotalEstimationOverLimitNotificationDate,
                CapitalDefect = data.CapitalDefect,
                ProposalId = _context.Proposal.FirstOrDefault(x => x.CallLogFITSID == id.Value) == null?0: _context.Proposal.FirstOrDefault(x => x.CallLogFITSID == id.Value).ProposalID,
                ConceptEstimate = _context.Proposal.FirstOrDefault(x => x.CallLogFITSID == id.Value) == null ? 0 : _context.Proposal.FirstOrDefault(x => x.CallLogFITSID == id.Value).ConceptEstimate,
                //Security Details
                IsSecurityIncident = data.IsSecurityIncident,
                IsSecInsuranceEvent = data.IsSecInsuranceEvent,
                SecEventNo = data.SecEventNo,
                SecCauseOfTheIssue = data.SecCauseOfTheIssue,
                SecIsThirdPartyInvolved = data.SecIsThirdPartyInvolved,
                SecLocalCotractorToRepairDamage = data.SecLocalCotractorToRepairDamage,
                SecSchoolIncidentCauseId = data.SecSchoolIncidentCauseId,
                SecIncidentDate = data.SecIncidentDate,
                SchoolFunded = data.CallLogTypeID == 4 ? true : false,
            };
            
            //Get school details
            var schoolDetails = _context.Properties.Where(x => x.PropertyId == callOutData.PropertyID).FirstOrDefault();
            if (schoolDetails != null)
            {
                callOutData.SchoolName = schoolDetails.PropertyShortDesc;
                callOutData.Area = schoolDetails.EducationArea != null ? schoolDetails.EducationArea.EducationAreaDesc : "";
                callOutData.SchoolEmail = schoolDetails.Email;
                callOutData.SchoolId = schoolDetails.SchoolId.HasValue ? schoolDetails.SchoolId.Value : 0;
                callOutData.SchoolDetails = schoolDetails;
                callOutData.ASOContactNumber = schoolDetails.ASOContactNumber;
                callOutData.ASOName = schoolDetails.ASOName;
                callOutData.GLContactNumber = schoolDetails.GLContactNumber;
                callOutData.GLName = schoolDetails.GLName;
            }

            //Get job id
            //if the Job is Cancelled, then dont bind
            ServiceReport sr = _context.ServiceReports.Where(r => r.CallLogFITSId == data.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault();
            callOutData.JobId = (sr != null && sr.ServiceReportStatusId != 9) ? sr.AgencySequenceId : null;
            callOutData.JobCancelled = sr != null ? sr.ServiceReportStatusId == 9 : false;
            SchoolLimit schoolLimit = _context.SchoolLimits.Where(x => x.PropertyID == callOutData.PropertyID && (x.EndDate == null || x.EndDate >= DateTime.Now)).FirstOrDefault();
            callOutData.SchoolLimit = schoolLimit != null ?
                                        decimal.Round(schoolLimit.SchoolLimitAmount.Value, 2, MidpointRounding.AwayFromZero) : 0;

            //Get Defect ID
            var defectCallout = _context.PSMDefectCallLog.Where(d => d.PropertyID == callOutData.PropertyID && d.CallLogFITSID == callOutData.CallLogFITSID).FirstOrDefault();
            if (defectCallout != null)
            {
                callOutData.DefectId = defectCallout.AgencySequenceID;
            }


            int supportDocumentClassId = 0;
            if (callOutData.WorkCategoryID.HasValue)
            {
                switch (callOutData.WorkCategoryID.Value)
                {
                    case 1:
                    case 2:
                    case 100:
                        supportDocumentClassId = (int)SupportDocumentClassEnum.CALLEUR;
                        break;

                    //case 2:
                    //    supportDocumentClassId = (int)SupportDocumentClassEnum.CALLMET;

                    //    break;

                    case 13:
                        supportDocumentClassId = (int)SupportDocumentClassEnum.CALLREA;
                        break;

                }

            }


            //Get documents
            List<SupportDocument> docs = _context.SupportDocument
                .Where(x => x.SupportDocumentClassID == supportDocumentClassId
                        && x.MasterRecordID == data.CallLogFITSID && (x.MarkAsDeleted == null || x.MarkAsDeleted == false))
                .ToList();

            if (docs.Any())
            {
                foreach (var d in docs)
                {
                    if (d.AttachmentLink.StartsWith("~/Uploads/7061001/1/"))
                    {
                        d.AttachmentLink = d.AttachmentLink.Replace("~/Uploads/7061001/1/", "~/Content/SupportDocuments/7061001/1/");
                    }
                }
            }

            callOutData.SupportDocuments = docs.Select(x => new SupportDocumentViewModel()
            {
                AgencyID = data.AgencyID.Value,
                AttachmentLink = x.AttachmentLink,
                AttachmentText = x.AttachmentText,
                PropertyName = callOutData.SchoolName,
                DocumentSequenceID = x.DocumentSequenceID,
                SupportDocumentTypeDesc = _context.SupportDocumentType
                                                                .Where(d => d.SupportDocumentTypeID == x.SupportDocumentTypeID)
                                                                .FirstOrDefault()
                                                                .SupportDocumentTypeDesc,
                RecordedDate = x.RecordedDate,
                SupportDocumentClassID = x.SupportDocumentClassID,
                MasterRecordID = x.MasterRecordID,
                SupportDocumentID = x.SupportDocumentID,
                SupportDocumentTypeID = x.SupportDocumentTypeID,
                ClientDocumentName = x.ClientDocumentName,
                RoleID = x.RoleID

            }).ToList();
            callOutData.JobBuildingIDs = _context.CallLogBuildingFITSs
                                        .Where(x => x.CallLogFITSID == data.CallLogFITSID && x.Deleted != true)
                                        .Select(d => d.BuildingID).ToList();
            callOutData.CallLogTime = callOutData.CallDate;
            callOutData.IncidentLogTime = callOutData.IncidentDate;
            callOutData.SecIncidentLogTime = callOutData.SecIncidentDate;


            callOutData.LocalContractorWorkOderDetails = _context.ServiceReports.Where(x => x.CallLogFITSId == data.CallLogFITSID).Select(y => new SubContractoWorkOrder()
            {
                Accepted = y.SubContractorAccepted,
                SubContractorId = y.SubContractorId,
                SubContractorName = y.SubContractor != null ? y.SubContractor.Name : "",
                SubContractorRespondedDate = y.SubContractorRespondedDate,
                WorkOrderId = y.AgencySequenceId.Value,
                ServiceReportId = y.ServiceReportId,
                SubContractorAssignedDate = y.SubContractorAssignedDate,
                WorkOrderStatus = y.ServiceReportStatus != null ? y.ServiceReportStatus.ServiceReportStatusDesc : "",
                WorkOrderCreatedDate = y.RecordedDate,
                WorkOrderCategory = y.WorkCategory != null ? y.WorkCategory.Description : "",
                WorkOrderCategoryId = y.WorkCategory != null ? y.WorkCategory.WorkCategoryId : 0
            }).ToList();

            //callOutData.LocalContractorWorkOderDetails = _context.ServiceReports.Where(x => x.CallLogFITSId == data.CallLogFITSID).Select(y => new SubContractoWorkOrder()
            //{
            //    Accepted = y.SubContractorAccepted,
            //    SubContractorId = y.SubContractorId,
            //    SubContractorName = y.SubContractor != null ? y.SubContractor.Name : "",
            //    SubContractorRespondedDate = y.SubContractorRespondedDate,
            //    WorkOrderId = y.AgencySequenceId.Value,
            //    ServiceReportId = y.ServiceReportId,
            //    SubContractorAssignedDate = y.SubContractorAssignedDate,
            //    WorkOrderStatus = y.ServiceReportStatus != null ? y.ServiceReportStatus.ServiceReportStatusDesc : "",
            //    WorkOrderCreatedDate = y.RecordedDate

            //}).ToList();

            callOutData.HasAtleastOneJob = _context.ServiceReports.Where(x => x.CallLogFITSId == data.CallLogFITSID && x.ServiceReportStatusId != 9).Any();
            var orderIds = _context.ServiceReports.Where(x => x.CallLogFITSId == data.CallLogFITSID).Select(s => s.ServiceReportId);
            //callOutData.HasAtleaseOneClaim = callOutData.HasAtleastOneJob.HasValue && 
            //                          callOutData.HasAtleastOneJob.Value &&
            //                          callOutData.IsInsuranceEvent.HasValue &&
            //                          callOutData.IsInsuranceEvent.Value &&
            //                          _context.Claims.Any(c => c.AgencyId == agencyId && orderIds.Contains(c.ReferringRecordId));
            //callOutData.IsReadyForIRCMPayment = callOutData.AllJobsPaid;
            //List<TradieListViewModel> tradies = GetTradies(callOutData.SubContractorId);
            //callOutData.Tradies = AutoMapper.Mapper.Map<List<TradieViewModel>>(tradies);
            return callOutData;
        }

        public bool SaveSchoolIncident(CallOutInsAddModel model)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                SchoolIncident inc = new SchoolIncident();
                inc.AgencyID = model.AgencyId;
                inc.PropertyID = model.PropertyId;

                inc.OHS = model.InsWHSIssue;
                inc.CallDate = model.InsCallDate;
                inc.CallTakenBy = model.InsCallTakenBy.SizeLimit(50);
                inc.LocationDesc = model.LocationDesc.SizeLimit(100);
                inc.SchoolIncidentDesc = model.SchoolIncidentDesc.SizeLimit(1000);
                inc.ContractorRepaired = model.ContractorRepaired;
                inc.SchoolAlarmNotificationID = model.SchoolAlarmNotificationId;
                inc.SchoolIncidentCauseID = model.SchoolIncidentCauseId;
                inc.IncidentFrom = model.IncidentFrom;
                inc.IncidentTo = model.IncidentTo;
                inc.RoomNumber = model.InsRooms;
                inc.ContactFirstName = model.ContactFirstName;
                inc.ContactLastName = model.ContactLastName;
                inc.ContactPhone = model.ContactPhone;
                inc.ContractorCalledOut = model.ContractorCalledOut;
                inc.MaintComplete = model.MaintComplete;
                inc.CallLogFinanceUrgencyID = model.InsCallLogFinanceUrgenyId;
                inc.DamageDetailDesc = model.DamageDetailDesc;
                inc.ConfirmedBySchool = model.ConfirmedBySchool;
                inc.RecordedBy = model.LoggedInUserId;
                inc.LoggerRole = model.LoggedInUserRoleId;
                inc.RecordedDate = DateTime.Now;
                inc.LastRecordedBy = model.LoggedInUserId;
                inc.LastRecordedDate = DateTime.Now;
                inc.SchoolLimitID = model.SchooLimitId;
                if (model.ConfirmedBySchool)
                {
                    inc.ConfirmedBySchoolDate = DateTime.Now;
                }


                if (model.ContractorCalledOut || model.ContractorEmailed)
                {
                    inc.ContractorEmailed = true;
                    inc.ContractorEmailedOn = DateTime.Now;
                }

                int? currentAgencySequenceID = _context.SchoolIncident.Where(x => x.AgencyID == model.AgencyId).Max(x => (int?)x.AgencySequenceID);
                inc.AgencySequenceID = (currentAgencySequenceID == null ? 0 : currentAgencySequenceID.Value) + 1;


                _context.SchoolIncident.Add(inc);
                _context.SaveChanges();
                model.SchoolIncidentAgencySequenceID = inc.AgencySequenceID.Value;

                model.CallLogFinanceUrgencyDesc = model.SelectedCallLogFinanceUrgenyId != null ? _context.CallLogFinanceUrgency.Where(x => x.CallLogFinanceUrgencyID == model.SelectedCallLogFinanceUrgenyId)
                    .FirstOrDefault().CallLogFinanceUrgencyDesc : string.Empty;

                model.SchoolIncidentID = inc.AgencySequenceID.Value;
                foreach (var buildingId in model.InsJobBuildingIDs)
                {
                    SchoolIncidentBuilding incBuilding = new SchoolIncidentBuilding()
                    {
                        AgencyID = model.AgencyId,
                        BuildingID = buildingId,
                        PropertyID = model.PropertyId,
                        SchoolIncidentID = inc.SchoolIncidentID
                    };
                    _context.SchoolIncidentBuilding.Add(incBuilding);
                    _context.SaveChanges();
                }

                if (model.SchoolIncidentLosses != null)
                {
                    int incLineNumber = 1;
                    foreach (var loss in model.SchoolIncidentLosses)
                    {
                        SchoolIncidentLoss lossInc = new SchoolIncidentLoss()
                        {
                            LossDesc = loss.LossDesc,
                            PropertyID = model.PropertyId,
                            Quantity = loss.Quantity,
                            SchoolIncidentID = inc.SchoolIncidentID,
                            EstRepairCost = loss.EstRepairCost,
                            SchoolIncidentLossStatusID = loss.SchoolIncidentLossStatusID,
                            RecordedDate = DateTime.Now,
                            RecordedBy = model.LoggedInUserId,
                            LineNumber = incLineNumber
                        };
                        _context.SchoolIncidentLoss.Add(lossInc);
                        _context.SaveChanges();
                        incLineNumber++;
                    }
                }


                dbContextTransaction.Commit();
            }


            return true;
        }

        private CallLogFITS MapCallOutModelToEntity(CallOutAddEditViewModel model)
        {
            CallLogFITS dataToSave = new CallLogFITS()
            {
                AgencyID = model.AgencyID,
                AgencySequenceID = model.AgencySequenceID,
                CallDate = model.CallDate,
                IncidentDate = model.IncidentDate,
                RequestedBy = (model.RequestedBy != null && model.RequestedBy.Length > 30) ? model.RequestedBy.Substring(0, 30) : model.RequestedBy,
                RequesterPosition = (model.RequesterPosition != null && model.RequesterPosition.Length > 30) ? model.RequesterPosition.Substring(0, 30) : model.RequesterPosition,
                RequesterContact = model.RequesterContact,

                ElementId = model.ElementId,
                AssemblyGroupId = model.AssemblyGroupId,
                ComponentCASId = model.ComponentCASId,

                CallLogDesc = model.CallLogDesc,
                CallLogFinanceUrgencyID = model.CallLogFinanceUrgencyID,
                CallLogStatusID = model.CallLogStatusID,
                CallTakenBy = (model.CallTakenBy != null && model.CallTakenBy.Length > 50) ? model.CallTakenBy.Substring(0, 50) : model.CallTakenBy,
                CallLogTypeID = model.CallLogTypeID,
                SchoolIncidentCauseId = model.SchoolIncidentCauseId,
                LocalCotractorToRepairDamage = model.LocalCotractorToRepairDamage,
                IsSecInsuranceEvent = model.LocalCotractorToSecurityIncident,
                SecLocalCotractorToRepairDamage = model.SecLocalCotractorToRepairDamage,
                EventNo = model.EventNo,
                CommerceNotInvolved = model.PWANotInvolved,
                Complete = model.Complete,
                Comments = model.Comments,
                EmailContractorDate = model.EmailDate,

                EUR100 = model.AMUEUR,
                LastRecordedDate = DateTime.Now,
                MaintenanceTaskCallLogID = model.SelectedMaintenanceCallLogId,
                OHS = model.WHSIssue,
                RoomLocation = (model.RoomLocation != null && model.RoomLocation.Length > 50) ? model.RoomLocation.Substring(0, 50) : model.RoomLocation,
                RoomNumber = (model.RoomNumber != null && model.RoomNumber.Length > 15) ? model.RoomNumber.Substring(0, 15) : model.RoomNumber,
                SchEUR100 = model.SchEUR,


                PropertyID = model.PropertyID,//Callout table SchoolId is the property Id.
                SchoolLimit = model.SchoolLimit,
                SchWorkCategoryID = model.SelectedSchWorkCategoryId,
                TreeSafetyIssue = model.TreeSafetyIssue,
                WorkCategoryID = model.WorkCategoryID,
                ContractorWONo = (model.FMCRefNo != null && model.FMCRefNo.Length > 15) ? model.FMCRefNo.Substring(0, 15) : model.FMCRefNo,
                LastEmailDate = model.LastEmailDate,
                EmailAlertTypes = model.EmailAlertTypes,
                HAZMAT = model.HAZMAT,
                // SubContractorId = model.SubContractorId,
                WorkTypeId = model.WorkTypeId,
                Version = model.WorkCategoryID == 13 ? 2 : 1,
                //IRCM inforamtion
                ClaimNumber = model.ClaimNumber.SizeLimit(64),
                CORPFileNo = model.CORPFileNo.SizeLimit(64),
                AssessorStatusID = model.AssessorStatusID,
                CommentsByIRCM = model.CommentsByIRCM.SizeLimit(1000),
                ProcessedByIRCM = model.ProcessedByIRCM,
                ProcessedDateByIRCM = model.ProcessedDateByIRCM
            };
            return dataToSave;

        }

        private void CopyValues<T>(T target, T source)
        {
            Type t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }

        public void DeleteDocument(int? supportDocumentID)
        {
            using (_context)
            {
                var supportDocument = _context.SupportDocument.Where(x => x.SupportDocumentID == supportDocumentID.Value).FirstOrDefault();
                supportDocument.MarkAsDeleted = true;
                _context.SaveChanges();

                //reset indexes
                //Re-arrange the order
                var docs = _context.SupportDocument.Where(x => x.AgencyID == supportDocument.AgencyID &&
                                x.SupportDocumentClassID == supportDocument.SupportDocumentClassID &&
                                x.MasterRecordID == supportDocument.MasterRecordID && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).OrderBy(x => x.RecordedDate).ToList();
                short workItemIndex = 1;
                foreach (var doc in docs)
                {
                    doc.DocumentSequenceID = workItemIndex;
                    _context.SaveChanges();
                    workItemIndex++;
                }
            }
        }

        public void DeleteCallOut(int? callOutId, int? agencyId)
        {
            var callLogFits = _context.CallLogFITS.SingleOrDefault(x => x.AgencySequenceID == callOutId && x.AgencyID == agencyId);
            callLogFits.DeletedYN = true;
            _context.SaveChanges();

        }

        public int SaveDocument(SupportDocument document, int callOutId, out int documentId)
        {
            documentId = 0;
            if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.CALLEUR ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.CALLMET ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.CALLREA ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.CPD ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.EQP ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.DEQP ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.SCH ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.ABS)
            {
                int? documentSequenceId = 0;
                document.MasterRecordID = callOutId;
                if (document.MasterRecordID == 0 || document.MasterRecordID < 0 )
                {
                    documentSequenceId = 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                }
                else
                {
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID && x.SupportDocumentClassID == document.SupportDocumentClassID &&
                                            x.MasterRecordID == document.MasterRecordID && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                }

                _context.SupportDocument.Add(document);
                _context.SaveChanges();

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            else if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.WORKORDER ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.WORKORDER_QUOTE ||
                document.SupportDocumentClassID == (int)SupportDocumentClassEnum.REA)
            {
                int? documentSequenceId = 0;
                var masterRecord = _context.ServiceReports.Where(x => x.AgencyId == document.AgencyID && x.AgencySequenceId == callOutId).FirstOrDefault();
                if (masterRecord != null)
                {
                    document.MasterRecordID = masterRecord.ServiceReportId;
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID && x.SupportDocumentClassID == document.SupportDocumentClassID &&
                                            x.MasterRecordID == document.MasterRecordID && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                    _context.SupportDocument.Add(document);
                    _context.SaveChanges();
                }

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            else if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.CV)
            {
                int? documentSequenceId = 0;
                var masterRecord = _context.ContractVariations.Where(x => x.AgencyID == document.AgencyID && x.AgencySequenceID == callOutId).FirstOrDefault();
                if (masterRecord != null)
                {
                    document.MasterRecordID = masterRecord.ContractVariationID;
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID && x.SupportDocumentClassID == document.SupportDocumentClassID &&
                                            x.MasterRecordID == document.MasterRecordID && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                    _context.SupportDocument.Add(document);
                    _context.SaveChanges();
                }

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            else if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.FMSchedule)
            {
                int? documentSequenceId = 0;
                document.MasterRecordID = callOutId;
                if (document.MasterRecordID != 0)
                {
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID &&
                                            x.MasterRecordID == document.MasterRecordID && x.SupportDocumentClassID == document.SupportDocumentClassID
                                            && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                    _context.SupportDocument.Add(document);
                    _context.SaveChanges();
                }

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            else if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.PRO_WORKORDER)
            {
                int? documentSequenceId = 0;
                document.MasterRecordID = callOutId;
                if (document.MasterRecordID != 0)
                {
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID &&
                                            x.MasterRecordID == document.MasterRecordID && x.SupportDocumentClassID == document.SupportDocumentClassID
                                            && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                    _context.SupportDocument.Add(document);
                    _context.SaveChanges();
                }

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            else if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.LCSD)
            {
                int? documentSequenceId = 0;
                document.MasterRecordID = callOutId;
                if (document.MasterRecordID != 0)
                {
                    documentSequenceId = _context.SupportDocument.Where(x => x.AgencyID == document.AgencyID &&
                                            x.MasterRecordID == document.MasterRecordID && x.SupportDocumentClassID == document.SupportDocumentClassID
                                            && (x.MarkAsDeleted == false || x.MarkAsDeleted == null)).Max(x => (int?)x.DocumentSequenceID);
                    documentSequenceId = (documentSequenceId == null ? 0 : documentSequenceId) + 1;
                    document.DocumentSequenceID = documentSequenceId.Value;
                    _context.SupportDocument.Add(document);
                    _context.SaveChanges();
                }

                documentId = document.SupportDocumentID;
                return documentSequenceId.Value;
            }
            return 0;
        }

        public void SavePSMDefectCallLog(PSMCalloutAddEditViewModel model, UserViewModel currentUser)
        {
            PSMDefectCallLog callLogToBeSaved = null;
            int? seqId = null;
            HelperMethods helper = new HelperMethods();
            if (!model.AgencySequenceId.HasValue || model.AgencySequenceId == 0)
            {
                callLogToBeSaved = _mapper.Map<PSMDefectCallLog>(model);
                seqId = _context.PSMDefectCallLog.Where(x => x.AgencyID == model.AgencyId).Max(x => (int?)x.AgencySequenceID);
                seqId = (seqId == null ? 0 : seqId.Value) + 1;
                callLogToBeSaved.AgencySequenceID = seqId;
                _context.PSMDefectCallLog.Add(callLogToBeSaved);
                _context.SaveChanges();
                model.PSMDefectCallLogId = callLogToBeSaved.PSMDefectCallLogId;
                model.AgencySequenceId = seqId;
            }
            else
            {
                //Existing defect update
                callLogToBeSaved = _context.PSMDefectCallLog.Where(x => x.PSMDefectCallLogId == model.PSMDefectCallLogId).FirstOrDefault();
                helper.updateChangedFields(model.UserCapabilities.MainSection, model, callLogToBeSaved);
                helper.updateChangedFields(model.UserCapabilities.PMSection, model, callLogToBeSaved);
                helper.updateChangedFields(model.UserCapabilities.ResponseTimeSection, model, callLogToBeSaved);
                callLogToBeSaved.LastRecordedBy = currentUser.UserCode;
                callLogToBeSaved.LastRecordedDate = DateTime.Now;
                _context.SaveChanges();
            }


            //4. Resave buildings if Job buildings is an editable field
            if (!HelperMethods.DisableField(model.UserCapabilities, "mainSection", "JobBuildingIDs"))
            {
                List<int> existingBuildingIds = _context.PSMDefectCallLogBuilding.Where(x => x.PSMDefectCallLogId == model.PSMDefectCallLogId && x.Deleted != true).Select(y => y.BuildingId).ToList();
                List<int> deletedBuildingIds = existingBuildingIds.Except(model.JobBuildingIDs).ToList();
                List<int> newBuildingIds = model.JobBuildingIDs.Except(existingBuildingIds).ToList();

                foreach (int delBuilding in deletedBuildingIds)
                {
                    var deletedBuilding = _context.PSMDefectCallLogBuilding.Where(x => x.PSMDefectCallLogId == model.PSMDefectCallLogId &&
                   x.AgencyId == model.AgencyId && x.PropertyId == model.PropertyId &&
                   x.BuildingId == delBuilding && x.Deleted != true).FirstOrDefault();
                    if (deletedBuilding != null)
                    {
                        deletedBuilding.LastUpdated = DateTime.Now;
                        deletedBuilding.Deleted = true;
                        _context.SaveChanges();
                    }
                }

                foreach (int buildingId in newBuildingIds)
                {
                    PSMDefectCallLogBuilding buildings = new PSMDefectCallLogBuilding()
                    {
                        AgencyId = model.AgencyId,
                        PropertyId = model.PropertyId,
                        PSMDefectCallLogId = model.PSMDefectCallLogId,
                        BuildingId = buildingId,
                        LastUpdated = DateTime.Now,

                    };

                    _context.PSMDefectCallLogBuilding.Add(buildings);
                    _context.SaveChanges();

                }
            }
        }

        public IEnumerable<CallOutViewModel> FilterDefectCallOutData(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, out int filteredResultsCount, out int totalResultsCount)
        {
            var ignoreFields = new string[] { };
            var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<CallOutViewModel>(filterModel.ColumnSearches, ignoreFields);

            if (filterModel.PropertyId.HasValue)
            {
                userPropertyIds.Clear();
                userPropertyIds.Add(filterModel.PropertyId.Value);
            }

            //Should be filtered by School work category as PM can change the work category to EUR
            IQueryable<CallOutViewModel> searchResults = _context.PSMDefectCallLog.Where(x => x.AgencyID == filterModel.AgencyId &&
                         (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true) &&
                         (filterModel.Workcategory > 0 ? x.SchWorkCategoryID == filterModel.Workcategory : x.WorkCategoryID > 0))
                     .Select(
                 d => new CallOutViewModel
                 {
                     Id = d.PSMDefectCallLogId,
                     AgencyId = d.AgencyID,
                     CallOutID = d.AgencySequenceID.Value,
                     CallDate = d.DefectFlaggedDate,
                     StatusID = d.CallLogStatusID.HasValue ? d.CallLogStatusID.Value : 0,
                     SchoolName = d.Property.PropertyDesc,
                     RequestedBy = d.RequestedBy,
                     Description = d.CallLogDesc,
                     //includeFullDesc ? d.CallLogDesc : (String.IsNullOrEmpty(d.CallLogDesc) ? String.Empty :
                     //        (d.CallLogDesc.Length > 50 ? d.CallLogDesc.Substring(0, 50) + "..." : d.CallLogDesc)),
                     WorkCategory = d.WorkCategory.Description,
                     Status = d.CallLogStatus.CallLogStatusDesc,

                     //if the Job is Cancelled, Callout is open and user can add another Job
                     JobId = _context.CallLogFITS.Where(r => r.CallLogFITSID == d.CallLogFITSID).FirstOrDefault().AgencySequenceID,
                     ServiceReportId = _context.CallLogFITS.Where(r => r.CallLogFITSID == d.CallLogFITSID).FirstOrDefault() != null ? _context.CallLogFITS.Where(r => r.CallLogFITSID == d.CallLogFITSID).FirstOrDefault().CallLogFITSID : (int?)null,
                 });


            var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.SchoolName.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.RequestedBy.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Status.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Description.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.WorkCategory.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Year.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Month.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Day.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallOutID.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.JobId.ToString().Contains(filterModel.SearchBy.ToLower()));

            filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in filterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "CallDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "JobId".ToLower())
                {
                    filtered = filtered.Where(f => f.JobId.ToString().Contains(columnSearch.Value));
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (filterModel.Take == -1)
                return filtered;
            return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
        }

        public IEnumerable<CallOutViewModel> FilterDefectCallOutDataHP(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, out int filteredResultsCount, out int totalResultsCount)
        {
            var ignoreFields = new string[] { };
            var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<CallOutViewModel>(filterModel.ColumnSearches, ignoreFields);

            if (filterModel.PropertyId.HasValue)
            {
                userPropertyIds.Clear();
                userPropertyIds.Add(filterModel.PropertyId.Value);
            }

            //Should be filtered by School work category as PM can change the work category to EUR
            IQueryable<CallOutViewModel> searchResults = _context.PSMDefectCallLog.Where(x => x.AgencyID == filterModel.AgencyId &&
                        (x.Complete == null || x.Complete == false) &&
                        (x.CallLogStatusID != 2 && x.CallLogStatusID != 4 && x.CallLogStatusID != 5) && //Cannot be any of the completes status in the home page
                         (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true) &&
                         (filterModel.Workcategory > 0 ? x.SchWorkCategoryID == filterModel.Workcategory : x.WorkCategoryID > 0))
                     .Select(
                 d => new CallOutViewModel
                 {
                     Id = d.PSMDefectCallLogId,
                     AgencyId = d.AgencyID,
                     CallOutID = d.AgencySequenceID.Value,
                     CallDate = d.DefectFlaggedDate,
                     StatusID = d.CallLogStatusID.HasValue ? d.CallLogStatusID.Value : 0,
                     SchoolName = d.Property.PropertyDesc,
                     RequestedBy = d.RequestedBy,
                     Description = d.CallLogDesc,
                     //includeFullDesc ? d.CallLogDesc : (String.IsNullOrEmpty(d.CallLogDesc) ? String.Empty :
                     //        (d.CallLogDesc.Length > 50 ? d.CallLogDesc.Substring(0, 50) + "..." : d.CallLogDesc)),
                     WorkCategory = d.WorkCategory.Description,
                     Status = d.CallLogStatus.CallLogStatusDesc,

                 });


            var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.SchoolName.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.RequestedBy.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Status.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Description.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.WorkCategory.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Year.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Month.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDate.Value.Day.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallOutID.ToString().Contains(filterModel.SearchBy.ToLower()));

            filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in filterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "CallDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (filterModel.Take == -1)
                return filtered;
            return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
        }

        public PSMCalloutAddEditViewModel GetPSMDefectCalloutById(int agencySequenceId, int agencyId)
        {
            PSMDefectCallLog callLog = _context.PSMDefectCallLog.Where(x => x.AgencySequenceID == agencySequenceId && x.AgencyID == agencyId).FirstOrDefault();
            if(callLog == null)
            { return null; }
            PSMCalloutAddEditViewModel model = _mapper.Map<PSMCalloutAddEditViewModel>(callLog);
            model.DefectCallTime = model.DefectCallDate;
            model.CallLogAgencySequenceId = callLog.CallLogFITSID > 0 ?
                                            _context.CallLogFITS.Where(x => x.CallLogFITSID == callLog.CallLogFITSID).FirstOrDefault().AgencySequenceID :
                                            null;
            model.JobBuildingIDs = _context.PSMDefectCallLogBuilding
                                      .Where(x => x.PSMDefectCallLogId == model.PSMDefectCallLogId && x.Deleted != true)
                                      .Select(d => d.BuildingId).ToList();


            return model;
        }

        public List<SupportDocumentViewModel> GetUploadedSupportDocuments(int supportDocumentClassId, int masterRecordId, int agencyId, string schoolName)
        {
            List<SupportDocument> docs = _context.SupportDocument
              .Where(x => x.SupportDocumentClassID == supportDocumentClassId
                      && x.MasterRecordID == masterRecordId && (x.MarkAsDeleted == null || x.MarkAsDeleted == false))
              .ToList();

            if (docs.Any())
            {
                foreach (var d in docs)
                {
                    if (d.AttachmentLink.StartsWith("~/Uploads/7061001/1/"))
                    {
                        d.AttachmentLink = d.AttachmentLink.Replace("~/Uploads/7061001/1/", "~/Content/SupportDocuments/7061001/1/");
                    }
                }
            }


            List<SupportDocumentViewModel> supportDocuments = docs.Select(x => new SupportDocumentViewModel()
            {
                AgencyID = agencyId,
                AttachmentLink = x.AttachmentLink,
                AttachmentText = x.AttachmentText,
                PropertyName = schoolName,
                DocumentSequenceID = x.DocumentSequenceID,
                SupportDocumentTypeDesc = _context.SupportDocumentType
                                                                .Where(d => d.SupportDocumentTypeID == x.SupportDocumentTypeID)
                                                                .FirstOrDefault()
                                                                .SupportDocumentTypeDesc,
                RecordedDate = x.RecordedDate,
                SupportDocumentClassID = x.SupportDocumentClassID,
                MasterRecordID = x.MasterRecordID,
                SupportDocumentID = x.SupportDocumentID,
                SupportDocumentTypeID = x.SupportDocumentTypeID

            }).ToList();

            return supportDocuments;
        }

        public void UpdateDefectEURCallLogIdAndDocuments(int agencyId, int? agencySequenceId, int callLogFITSId, string usercode)
        {
            int supportDocTypeId = 17;
            using (var cont = new AssetCareContext())
            {
                var defectCallLog = cont.PSMDefectCallLog.Where(x => x.AgencyID == agencyId && x.AgencySequenceID == agencySequenceId).FirstOrDefault();
                defectCallLog.CallLogFITSID = callLogFITSId;
                defectCallLog.CallDate = DateTime.Now;
                defectCallLog.LastRecordedDate = DateTime.Now;
                defectCallLog.LastRecordedBy = usercode;

                //copy documents
                List<SupportDocument> existingDocs = cont.SupportDocument.Where(x => x.MasterRecordID == defectCallLog.PSMDefectCallLogId && x.PropertyID == defectCallLog.PropertyID && x.SupportDocumentClassID == (int)SupportDocumentClassEnum.CPD && (x.Deleted == null || x.Deleted == false)).ToList();
                if (existingDocs != null && existingDocs.Count() > 0)
                {
                    foreach (var doc in existingDocs)
                    {

                        var docType = cont.SupportDocumentType.Where(x => x.SupportDocumentTypeID == doc.SupportDocumentTypeID).FirstOrDefault();
                        if (docType != null)
                        {
                            var eurDocType = cont.SupportDocumentType.Where(x => x.SupportDocumentTypeName == docType.SupportDocumentTypeName && x.SupportDocumentClassID == 1).FirstOrDefault();
                            if (eurDocType != null)
                            {
                                supportDocTypeId = eurDocType.SupportDocumentTypeID;
                            }
                        }

                        SupportDocument suportDocument = new SupportDocument()
                        {
                            AgencyID = agencyId,
                            AttachmentText = doc.AttachmentText,
                            AttachmentLink = doc.AttachmentLink,
                            Deleted = false,
                            SupportDocumentClassID = (int)SupportDocumentClassEnum.CALLEUR,
                            SupportDocumentTypeID = supportDocTypeId,
                            DocumentSequenceID = doc.DocumentSequenceID,
                            IssueDate = doc.IssueDate,
                            LastRecordedBy = doc.LastRecordedBy,
                            MasterRecordID = callLogFITSId,
                            PropertyID = doc.PropertyID,
                            RecordedBy = doc.RecordedBy,
                            RecordedDate = doc.RecordedDate,
                            MarkAsDeleted = false
                        };
                        cont.SupportDocument.Add(suportDocument);
                        cont.SaveChanges();
                    }
                }

                cont.SaveChanges();
            }
        }

        public void CopyPSMDefectDocumentsToEURCallLog(int psmDefectCalloutId, int callogFitsId)
        {

        }

        public List<SubContractor> GetSubContractorsByBusinessType(int id, int agencyId)
        {
            List<SubContractor> contractors = new List<SubContractor>();
            if (id == 0)
            {
                contractors = (from sb in _context.SubContractorBusinessType
                               join s in _context.SubContractor on sb.SubContractorId equals s.ID
                               join sa in _context.SubContractorAgency on s.ID equals sa.SubContractorId
                               where sa.AgencyId == agencyId && s.Active
                               select s).ToList();
                return contractors;
            }
            else
            {

                contractors = (from sb in _context.SubContractorBusinessType
                               join s in _context.SubContractor on sb.SubContractorId equals s.ID
                               join sa in _context.SubContractorAgency on s.ID equals sa.SubContractorId
                               where sb.BusinessTypeId == id && sa.AgencyId == agencyId && s.Active
                               select s).ToList();
                return contractors;
            }
        }
        public SubContractor GetSubContractorInfo(int userID)
        {
            var subContractor = (from t in _context.Tradie
                                 join s in _context.SubContractor on t.LocalContractorID equals s.ID
                                 where t.UserID == userID && s.Active == true
                                 select s).FirstOrDefault();
            return subContractor;
        }

        public TradieResponse GetTradies(CallOutFilterModel callOutFilterModel, out int filteredResultsCount, out int totalResultsCount)
        {
            List<TradieListViewModel> tradies = new List<TradieListViewModel>();
            if (callOutFilterModel.SubContractorID > 0)
            {
                tradies = (from t in _context.Tradie
                           join tu in _context.Users on t.UserID equals tu.UserID
                           where t.LocalContractorID == callOutFilterModel.SubContractorID
                           && tu.Active==true
                           select new TradieListViewModel
                           {
                               TradieID = t.ID,
                               FirstName = tu.FirstName,
                               LastName = tu.LastName,
                               Mobile = tu.Mobile == null ? string.Empty : tu.Mobile,
                               Email = tu.Email == null ? string.Empty : tu.Email,
                               Active = tu.Active.Value ? "True" : "False", 
                               LicenseNumber = t.LicenseNumber,

                               _LicenseStartDate = t.LicenseStartDate,
                               WWCC = AssetCareContext.GetTradieServiceTypes(t.ID),
                               _LicenseExpiryDate = t.LicenseExpiryDate,
                               _WWCCStartDate = t.WWCCStartDate,
                               _WWCCExpiryDate = t.WWCCExpiryDate,
                               LocalContractorID = t.LocalContractorID,
                               Verified = t.ATSIVerified == true ? "Yes" : "No",
                               WWCCCheck = t.WWCCCheck == true ? "Yes" : "No",
                               NTBELCheck = t.NTBELCheck == true ? "Yes" : "No",
                               PoliceCheck = t.PoliceCheck == true ? "Yes" : "No",
                               Compliant = t.Compliant.HasValue && t.Compliant.Value ? "Yes" : "No",
                           }).ToList();
            }
            //bool hasValidTradie = tradies.Any(t =>
            //                                  t.NTBELCheck == "Yes" &&
            //                                  t.PoliceCheck == "Yes" &&
            //                                  t.WWCCCheck == "Yes" ); //&& t.Compliant == "Yes"
            bool hasValidTradie = tradies.Any(t => t.Compliant == "Yes");

            var searchResults = tradies.AsQueryable();

            var filtered = string.IsNullOrEmpty(callOutFilterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.FirstName.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.LastName.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.Mobile.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.Email.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                 
                (f._LicenseStartDate.HasValue && f._LicenseStartDate.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower())) ||
                (f._LicenseExpiryDate.HasValue && f._LicenseExpiryDate.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower())) ||  //Assigned Date
                    // WWCCCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.WWCCCheck == "Yes") ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.WWCCCheck == "No") ||

                    // PoliceCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.PoliceCheck == "Yes") ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.PoliceCheck == "No") ||

                    // NTBELCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.NTBELCheck == "Yes") ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.NTBELCheck == "No") ||
                    // NTBELCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.Compliant == "Yes") ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.Compliant == "No") ||
                f.Verified.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) 
               
                );

            if (callOutFilterModel.SortDir == "descending")
            {
                filtered = filtered
               .OrderByDescending(f => f.NTBELCheck=="Yes")
               .ThenByDescending(f => f.WWCCCheck == "Yes")
               .ThenByDescending(f => f.PoliceCheck == "Yes")
               .ThenByDescending(f => callOutFilterModel.SortBy);
            }
            else
            {
                filtered = filtered
               .OrderByDescending(f => f.NTBELCheck == "Yes")
               .ThenByDescending(f => f.WWCCCheck == "Yes")
               .ThenByDescending(f => f.PoliceCheck == "Yes")
               .ThenBy(f => callOutFilterModel.SortBy);
            }

           // filtered = filtered.OrderBy($"{callOutFilterModel.SortBy} {callOutFilterModel.SortDir}");
            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            //if (callOutFilterModel.Take == -1)
            //    return filtered;
            //return filtered.Skip(callOutFilterModel.Skip).Take(callOutFilterModel.Take).ToList();

            if (callOutFilterModel.Take == -1)
            {
                return new TradieResponse
                {
                    Data = filtered,
                    RecordsTotal = totalResultsCount,
                    RecordsFiltered = filteredResultsCount,
                    HasValidTradie = hasValidTradie
                };
            }

            var result = filtered.Skip(callOutFilterModel.Skip)
                     .Take(callOutFilterModel.Take)
                     .ToList();

            return new TradieResponse
            {
                Data = result,
                RecordsTotal = totalResultsCount,
                RecordsFiltered = filteredResultsCount,
                HasValidTradie = hasValidTradie
            };
        }

        
        public IEnumerable<TradieViewModel> GetTradieDetailsHP(int woid, int agencyid)
        {
            List<TradieViewModel> tradies = new List<TradieViewModel>();
            var rawData = (from sr in _context.ServiceReports
                           join srs in _context.ServiceReportStatuses on sr.ServiceReportStatusId equals srs.ServiceReportStatusID
                           join cl in _context.CallLogFITS on sr.CallLogFITSId equals cl.CallLogFITSID
                           join st in _context.ServiceReportTradie on sr.ServiceReportId equals st.ServiceReportId into stGroup
                           from st in stGroup.DefaultIfEmpty()   // ← LEFT JOIN
                           join t in _context.Tradie on st.TradieId equals t.ID into tGroup
                           from t in tGroup.DefaultIfEmpty()     // ← LEFT JOIN
                           join u in _context.Users on t.UserID equals u.UserID into uGroup
                           from u in uGroup.DefaultIfEmpty()     // ← LEFT JOIN
                           join sc in _context.SubContractor on sr.SubContractorId equals sc.ID into scGroup
                           from sc in scGroup.DefaultIfEmpty()   // ← LEFT JOIN
                           where sr.AgencySequenceId == woid
                              && sr.AgencyId == agencyid
                              && sr.AgencyId == cl.AgencyID
                           select new
                           {
                               User = u,
                               ServiceReport = sr,
                               ServiceReportStatus = srs,
                               Callout = cl,
                               ST = st,
                               Subcontractor = sc
                           })
                            .ToList();

            tradies = rawData.Select(x => new TradieViewModel
            {
                WOID = x.ServiceReport?.AgencySequenceId?.ToString(),
                COID = x.Callout?.AgencySequenceID?.ToString(),
                CalllogFitsID = x.Callout?.CallLogFITSID.ToString(),
                ExpectedVisitDate = x.ServiceReport?.ExpectedVisitDate,
                WODescription = x.ServiceReport?.ServiceReportDesc,

                WODate = x.ServiceReport?.RecordedDate,
                WOStatus = x.ServiceReportStatus?.ServiceReportStatusDesc,
                LocalContractorName = x.Subcontractor?.ContactPersonName,
                LCContact = x.Subcontractor?.Mobile,
                LCEmail = x.Subcontractor?.Email,
                FirstVisitDate = x.ServiceReport?.FirstVisitDate,
                EstimatedCompletionDate = x.ServiceReport?.EstimatedCompletionDate,
                DateAllWorkCompleted = x.ServiceReport?.CompletedDate,
                LCComments = x.ServiceReport?.SubContractorComments,
                LCCommentDate = x.ServiceReport?.SubContractorCommentDate,
                FMCComments = x.ServiceReport?.Comments,
                FMCCommentDate = x.ServiceReport?.FMCCommentDate,
                ASOComments = x.ServiceReport?.AMUComments,
                ASOCommentDate = x.ServiceReport?.ASOCommentDate,
                AcceptanceDate = x.ServiceReport?.SubContractorAssignedDate
            })
 .ToList();

            return tradies;


        }

        public IEnumerable<TradieViewModel> GetSchoolHomeTradies(CallOutFilterModel callOutFilterModel, out int filteredResultsCount, out int totalResultsCount)
        {
            List<TradieViewModel> tradies = new List<TradieViewModel>();
            var rawData = (from st in _context.ServiceReportTradie
                           join t in _context.Tradie on st.TradieId equals t.ID
                           join u in _context.Users on t.UserID equals u.UserID
                           join sr in _context.ServiceReports on st.ServiceReportId equals sr.ServiceReportId
                           join cl in _context.CallLogFITS on sr.CallLogFITSId equals cl.CallLogFITSID
                           where st.PropertyId == callOutFilterModel.PropertyId && sr.AgencyId == callOutFilterModel.AgencyId && sr.AgencyId == cl.AgencyID
                            && sr.ServiceReportStatus.ServiceReportStatusDesc == "Local Contractor Accepted"
                           select new
                           {
                               Tradie = t,
                               User = u,
                               ServiceTypes = t.TradieServiceTypes.Select(ts => ts.BusinessType.Description),
                               ST = st,
                               DocumentUrl = t.Document.Url,
                               ServiceReport = sr,
                               Callout = cl
                           })
                  .ToList();

            tradies = rawData.Select(x => new TradieViewModel
            {
                ID = x.Tradie.ID,
                FirstName = x.User.FirstName,
                LastName = x.User.LastName,
                Mobile = x.User.Mobile,
                LicenseNumber = x.Tradie.LicenseNumber,
                LicenseExpiryDate = x.Tradie.LicenseExpiryDate,
                LicenseStartDate = x.Tradie.LicenseStartDate,
                PoliceCheckExpiryDate = x.Tradie.PoliceCheckExpiryDate,
                NTBELCheck = x.Tradie.NTBELCheck,
                WWCCCheck = x.Tradie.WWCCCheck,
                PoliceCheck = x.Tradie.PoliceCheck,
                Active = x.Tradie.Active ?? true,
                CheckIn = x.ST.CheckIn,
                CheckOut = x.ST.CheckOut,
                CreatedDate = x.ST.CreatedDate,
                AssignedBy = x.ST.CreatedBy,
                PictureUrl = x.DocumentUrl,
                TradieServiceTypeOptionNames = string.Join(", ", x.ServiceTypes),
                WOID = x.ServiceReport.AgencySequenceId?.ToString(),
                COID = x.Callout.AgencySequenceID?.ToString(),
                CalllogFitsID = x.Callout.CallLogFITSID.ToString(),
                ExpectedVisitDate = x.ServiceReport.ExpectedVisitDate,
                WODescription = x.ServiceReport.ServiceReportDesc
        })
            .ToList();


            var searchResults = tradies.AsQueryable();

            var filtered = string.IsNullOrEmpty(callOutFilterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.FirstName.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                  f.COID.ToString().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                  f.WOID.ToString().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.LastName.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.Mobile.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                f.LicenseNumber.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                (f.ExpectedVisitDate.HasValue && f.ExpectedVisitDate.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower())) ||
                //(f.LicenseStartDate.HasValue && f.LicenseStartDate.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower())) ||  //Assigned Date
                f.Active.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                    // WWCCCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.WWCCCheck == true) ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.WWCCCheck == null) ||

                    // PoliceCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.PoliceCheck == true) ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.PoliceCheck == null) ||

                    // NTBELCheck
                    (callOutFilterModel.SearchBy.ToLower() == "yes" && f.NTBELCheck == true) ||
                    (callOutFilterModel.SearchBy.ToLower() == "no" && f.NTBELCheck == null)
                //f.Verified.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                //f.WWCCCheck.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                //f.PoliceCheck.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) ||
                //f.NTBELCheck.ToString().ToLower().Contains(callOutFilterModel.SearchBy.ToLower()) //||
                //(f.CheckIn.HasValue && f.CheckIn.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower())) ||
                //(f.CheckOut.HasValue && f.CheckOut.Value.ToString("dd-MM-yyyy").Contains(callOutFilterModel.SearchBy.ToLower()))
                );


            filtered = filtered.OrderBy($"{callOutFilterModel.SortBy} {callOutFilterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in callOutFilterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "expectedvisitdate")
                {
                    filtered = filtered.Where(f =>
                                                f.ExpectedVisitDate.HasValue &&
                                                f.ExpectedVisitDate.Value
                                                    .ToString("dd-MM-yyyy HH:mm")
                                                    .Contains(columnSearch.Value)
                                            );
                }
                else if (columnSearch.Key.ToLower() == "COID".ToLower())
                {
                    filtered = filtered.Where(f => f.COID.ToString().Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "WOID".ToLower())
                {
                    filtered = filtered.Where(f => f.WOID.ToString().Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "NTBELCheck".ToLower())
                {
                    if (columnSearch.Value.ToLower() == "yes")
                    {
                        filtered = filtered.Where(f => f.NTBELCheck == true);
                    }
                    else if (columnSearch.Value.ToLower() == "no")
                    {
                        filtered = filtered.Where(f => f.NTBELCheck == null);
                    }
                }
                else if (columnSearch.Key.ToLower() == "WWCCCheck".ToLower())
                {
                    if (columnSearch.Value.ToLower() == "yes")
                    {
                        filtered = filtered.Where(f => f.WWCCCheck == true);
                    }
                    else if (columnSearch.Value.ToLower() == "no")
                    {
                        filtered = filtered.Where(f => f.WWCCCheck == null);
                    }
                }
                else if (columnSearch.Key.ToLower() == "PoliceCheck".ToLower())
                {
                    if (columnSearch.Value.ToLower() == "yes")
                    {
                        filtered = filtered.Where(f => f.PoliceCheck == true);
                    }
                    else if (columnSearch.Value.ToLower() == "no")
                    {
                        filtered = filtered.Where(f => f.PoliceCheck == null);
                    }
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            if (callOutFilterModel.SortBy == "ExpectedVisitDate")
            {
                var today = DateTime.Today;

                filtered = filtered
                   .OrderBy(f => f.ExpectedVisitDate.HasValue ? 0 : 1) // Nulls last
                   .ThenBy(f =>
                       f.ExpectedVisitDate.HasValue && f.ExpectedVisitDate.Value.Date == today ? 0 :
                       f.ExpectedVisitDate.HasValue && f.ExpectedVisitDate.Value.Date > today ? 1 :
                       f.ExpectedVisitDate.HasValue ? 2 : 3)
                   .ThenBy(f => f.ExpectedVisitDate); // Sort inside each group by date ascending
            }
            else
            {
                    filtered = filtered.OrderBy($"{callOutFilterModel.SortBy} {callOutFilterModel.SortDir}");
            }

            //filtered = filtered.OrderBy($"{callOutFilterModel.SortBy} {callOutFilterModel.SortDir}");
            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (callOutFilterModel.Take == -1)
                return filtered;
            return filtered.Skip(callOutFilterModel.Skip).Take(callOutFilterModel.Take).ToList();

        }

        public bool IsTotalEstimationOverLimit(int? id, int agencyId, double estimationThreshold)
        {
            var Estimations = from sr in _context.ServiceReports
                              join c in _context.CallLogBuildingFITSs on sr.CallLogFITSId equals c.CallLogFITSID
                              where c.CallLogFITSID == id && c.AgencyID == agencyId
                              select (sr.EstimatedAmount == null) ? 0 : sr.EstimatedAmount;
            var TotalEstimation = Estimations.Any() ? Estimations.Sum() : 0;
            return ((double)TotalEstimation) > (estimationThreshold > 0 ? estimationThreshold : 20 * 1000);
        }

        public IEnumerable<AssessorStatus> GetAssessorStatuses()
        {
            return _context.AssessorStatuses.ToList();
        }

        public bool UpdateTotalEstimationOverLimitNotificationDate(CallOutAddEditViewModel model)
        {
            CallLogFITS callout = _context.CallLogFITS.SingleOrDefault(x => x.CallLogFITSID == model.CallOutId && x.AgencyID == model.AgencyID);
            callout.TotalEstimationOverLimitNotificationDate = DateTime.Now;
            _context.SaveChanges();
            return true;
        }

        #region Defect callout

        public IEnumerable<CallOutViewModel> FilterCapitalDefectData(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, string userRole, out int filteredResultsCount, out int totalResultsCount)
        {
            var ignoreFields = new string[] { };
            var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<CallOutViewModel>(filterModel.ColumnSearches, ignoreFields);

            if (filterModel.PropertyId.HasValue)
            {
                userPropertyIds.Clear();
                userPropertyIds.Add(filterModel.PropertyId.Value);
            }

            IQueryable<CallOutViewModel> searchResults = _context.CallLogFITS.Where(x => x.AgencyID == filterModel.AgencyId
            && (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)
                       && (x.CapitalDefect == true) //Defect management list page display only records with Capital Defect is checked
                         )
                     .Select(
                 d => new CallOutViewModel
                 {
                     Id = d.CallLogFITSID,
                     AgencyId = d.AgencyID,
                     CallOutID = d.AgencySequenceID.Value,
                     CallDate = d.CallDate,
                     CallDateList = d.CallDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", d.CallDate).ToString().Length) + SqlFunctions.DateName("dd", d.CallDate) + "-" +
                                                                                SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", d.CallDate).ToString().Length) + SqlFunctions.DatePart("month", d.CallDate) + "-" +
                                                                                SqlFunctions.DateName("yyyy", d.CallDate)
                                                                                : "",
                     StatusID = d.CallLogStatusID.HasValue ? d.CallLogStatusID.Value : 0,
                     SchoolName = d.Property.PropertyDesc,
                     AsbestosHRProperty = d.Property.AsbestosHR,
                     DefectMgmtProperty = d.Property.DefectMgmt,
                     RequestedBy = d.RequestedBy,
                     Description = d.CallLogDesc,
                     //includeFullDesc ? d.CallLogDesc : (String.IsNullOrEmpty(d.CallLogDesc) ? String.Empty :
                     //        (d.CallLogDesc.Length > 50 ? d.CallLogDesc.Substring(0, 50) + "..." : d.CallLogDesc)),
                     WorkCategory = d.WorkCategory.Description,
                     SubContractor = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).Select(x => x.SubContractor.Name).ToList(),
                     Status = d.CallLogStatus.CallLogStatusDesc,
                     //if the Job is Cancelled, Callout is open and user can add another Job
                     JobId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().AgencySequenceId,
                     ServiceReportId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault() != null ? _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().ServiceReportId : (int?)null,
                     JobIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.AgencySequenceId).ToList(),
                     ServiceReportIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.ServiceReportId).ToList(),
                     Category = _context.CallLogTypes.Where(x => x.CallLogTypeID == d.CallLogTypeID).OrderByDescending(x => x.CallLogTypeID).FirstOrDefault().CallLogTypeDesc,
                     CalloutSummary = d.Comments,
                     //includeFullDesc ? d.Comments : (String.IsNullOrEmpty(d.Comments) ? String.Empty :
                     //        (d.Comments.Length > 50 ? d.Comments.Substring(0, 50) + "..." : d.Comments)),
                     SchoolUrgency = _context.CallLogFinanceUrgency.Where(x => x.CallLogFinanceUrgencyID == d.SchoolUrgencyID).OrderByDescending(x => x.CallLogFinanceUrgencyID).FirstOrDefault().CallLogFinanceUrgencyDesc,
                     IsInsuranceEvent = (d.IsInsuranceEvent == true) ? "Yes" : "No",
                     IsReadyForReimbursement = (d.IsReadyForReimbursement == true) ? "Yes" : "No",
                     AssessorStatus = ((d.AssessorStatusID == 1) ? "None" :
                                        ((d.AssessorStatusID == 2) ? "Assessor Appointed" :
                                         ((d.AssessorStatusID == 3) ? "Waive Assessor" :
                                        "N/A")))
                 });


            var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.SchoolName.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.RequestedBy.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Status.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Description.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.SubContractor.Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDateList.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallOutID.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                (f.Category != null && f.Category.ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                f.JobId.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.IsInsuranceEvent.Contains(filterModel.SearchBy.ToLower()) ||
                f.IsReadyForReimbursement.Contains(filterModel.SearchBy.ToLower()) ||
                f.AssessorStatus.Contains(filterModel.SearchBy.ToLower()) ||
                 f.SchoolUrgency.Contains(filterModel.SearchBy.ToLower())
                );

            filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in filterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "DefectDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "JobId".ToLower())
                {
                    filtered = filtered.Where(f => f.JobId.ToString().Contains(columnSearch.Value));
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (filterModel.Take == -1)
                return filtered;
            return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
        }
        
        public IEnumerable<CallOutViewModel> FilterCapitalDefectDataHP(CallOutFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, string userRole, out int filteredResultsCount, out int totalResultsCount)
        {
            IQueryable<CallOutViewModel> searchResults = _context.CallLogFITS.Where(x => x.AgencyID == filterModel.AgencyId
                               && (filterModel.PropertyId > 0 ? (x.PropertyID == filterModel.PropertyId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true))
                               && (x.CapitalDefect == true)
                               && (x.CallLogStatusID == 1 || x.CallLogStatusID == 10 || x.CallLogStatusID == 12)
                               )
                     .Select(
                 d => new CallOutViewModel
                 {
                     Id = d.CallLogFITSID,
                     AgencyId = d.AgencyID,
                     CallOutID = d.AgencySequenceID.Value,
                     CallDate = d.CallDate,
                     CallDateList = d.CallDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", d.CallDate).ToString().Length) + SqlFunctions.DateName("dd", d.CallDate) + "-" +
                                                                                SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", d.CallDate).ToString().Length) + SqlFunctions.DatePart("month", d.CallDate) + "-" +
                                                                                SqlFunctions.DateName("yyyy", d.CallDate)
                                                                                : "",
                     StatusID = d.CallLogStatusID.HasValue ? d.CallLogStatusID.Value : 0,
                     SchoolName = d.Property.PropertyDesc,
                     AsbestosHRProperty = d.Property.AsbestosHR,
                     DefectMgmtProperty = d.Property.DefectMgmt,
                     RequestedBy = d.RequestedBy,
                     Description = d.CallLogDesc,
                     //includeFullDesc ? d.CallLogDesc : (String.IsNullOrEmpty(d.CallLogDesc) ? String.Empty :
                     //        (d.CallLogDesc.Length > 50 ? d.CallLogDesc.Substring(0, 50) + "..." : d.CallLogDesc)),
                     WorkCategory = d.WorkCategory.Description,
                     SubContractor = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).Select(x => x.SubContractor.Name).ToList(),
                     Status = d.CallLogStatus.CallLogStatusDesc,
                     //if the Job is Cancelled, Callout is open and user can add another Job
                     JobId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID && r.ServiceReportStatusId != 9).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().AgencySequenceId,
                     ServiceReportId = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault() != null ? _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault().ServiceReportId : (int?)null,
                     JobIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.AgencySequenceId).ToList(),
                     ServiceReportIds = _context.ServiceReports.Where(r => r.CallLogFITSId == d.CallLogFITSID).OrderByDescending(x => x.ServiceReportId).Select(x => x.ServiceReportId).ToList(),
                     Category = _context.CallLogTypes.Where(x => x.CallLogTypeID == d.CallLogTypeID).OrderByDescending(x => x.CallLogTypeID).FirstOrDefault().CallLogTypeDesc,
                     CalloutSummary = d.Comments,
                     //includeFullDesc ? d.Comments : (String.IsNullOrEmpty(d.Comments) ? String.Empty :
                     //        (d.Comments.Length > 50 ? d.Comments.Substring(0, 50) + "..." : d.Comments)),
                     SchoolUrgency = _context.CallLogFinanceUrgency.Where(x => x.CallLogFinanceUrgencyID == d.SchoolUrgencyID).OrderByDescending(x => x.CallLogFinanceUrgencyID).FirstOrDefault().CallLogFinanceUrgencyDesc,
                     IsInsuranceEvent = (d.IsInsuranceEvent == true) ? "Yes" : "No",
                     IsReadyForReimbursement = (d.IsReadyForReimbursement == true) ? "Yes" : "No",
                     AssessorStatus = ((d.AssessorStatusID == 1) ? "None" :
                                        ((d.AssessorStatusID == 2) ? "Assessor Appointed" :
                                         ((d.AssessorStatusID == 3) ? "Waive Assessor" :
                                        "N/A")))
                 });


            var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                 ? searchResults
                  : searchResults.Where(f => f.SchoolName.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.RequestedBy.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Status.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.Description.ToLower().Contains(filterModel.SearchBy.ToLower()) ||
                f.SubContractor.Contains(filterModel.SearchBy.ToLower()) ||
                f.CallDateList.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.CallOutID.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                (f.Category != null && f.Category.ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                f.JobId.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                f.IsInsuranceEvent.Contains(filterModel.SearchBy.ToLower()) ||
                f.IsReadyForReimbursement.Contains(filterModel.SearchBy.ToLower()) ||
                f.AssessorStatus.Contains(filterModel.SearchBy.ToLower())
                );

            filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
            int i = 1;
            foreach (var columnSearch in filterModel.ColumnSearches)
            {
                if (columnSearch.Key.ToLower() == "DefectDate".ToLower())
                {
                    filtered = filtered.Where(f => ((f.CallDate.Value.Day < 10 ? "0" + f.CallDate.Value.Day.ToString() : f.CallDate.Value.Day.ToString()) + "-" +
                                          (f.CallDate.Value.Month < 10 ? "0" + f.CallDate.Value.Month.ToString() : f.CallDate.Value.Month.ToString()) + "-" +
                                          f.CallDate.Value.Year.ToString())
                                          .Contains(columnSearch.Value));
                }
                else if (columnSearch.Key.ToLower() == "JobId".ToLower())
                {
                    filtered = filtered.Where(f => f.JobId.ToString().Contains(columnSearch.Value));
                }
                else
                {
                    filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                }
                i++;
            }

            filteredResultsCount = filtered.Count();
            totalResultsCount = searchResults.Count();

            if (filterModel.Take == -1)
                return filtered;
            return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
        }
        
        public CapitalDefectCalloutViewModel GetPSMDefectCalloutByCallLogFITSId(int callLogFITSID, int agencyId)
        {
            PSMDefectCallLog callLog = _context.PSMDefectCallLog.Where(x => x.CallLogFITSID == callLogFITSID && x.AgencyID == agencyId).FirstOrDefault();
            if (callLog == null)
            {
                return null;
            }

            CapitalDefectCalloutViewModel model = new CapitalDefectCalloutViewModel
            {
                PSMDefectCallLogId = callLog.PSMDefectCallLogId,
                CallLogFITSID = callLog.CallLogFITSID,
                DefectEmailToSchoolAndPMPD = false,
                FirstEmailSentDate = callLog.FirstEmailSentDate,
                LastEmailSentDate = callLog.LastEmailSentDate,
                WorkCategoryID = callLog.WorkCategoryID,
                PSMWorkAssignmentStatusId = callLog.PSMWorkAssignmentStatusId,
                PMComments = callLog.PMComments,
                Complete = callLog.Complete.HasValue? callLog.Complete.Value: false,
                CompletedDate = callLog.CompletedDate,
                RecordedBy = callLog.RecordedBy,
                RecordedDate = callLog.RecordedDate,
                LastRecordedBy = callLog.LastRecordedBy,
                LastRecordedDate = callLog.LastRecordedDate,
                CallLogStatusId = callLog.CallLogStatusID,
                CallAssignedDate = callLog.CallAssignedDate,
                WorkTypeId = callLog.WorkTypeId,
                DefectFlaggedDate = callLog.DefectFlaggedDate,
                DefectReversalDate  = callLog.DefectReversalDate
            };

            return model;
        }

        public void SaveOrUpdatePSMDefectCallLog(CallOutAddEditViewModel model, UserViewModel currentUser)
        {
            PSMDefectCallLog callLogToBeSaved = _context.PSMDefectCallLog.Where(x => x.PSMDefectCallLogId == model.CapitalDefectCalloutViewModel.PSMDefectCallLogId).FirstOrDefault();
            HelperMethods helper = new HelperMethods();
            if (!model.CapitalDefectCalloutViewModel.CallLogFITSID.HasValue || model.CapitalDefectCalloutViewModel.CallLogFITSID == 0)
            {
                if (callLogToBeSaved == null)
                {
                    callLogToBeSaved = new PSMDefectCallLog
                    {
                        AgencyID = model.AgencyID,
                        AgencySequenceID = model.AgencySequenceID,
                        PropertyID = model.PropertyID,
                        WorkCategoryID = model.WorkCategoryID,
                        SchWorkCategoryID = model.WorkCategoryID,
                        ElementId = model.ElementId,
                        WorkTypeId = model.WorkTypeId,
                        RoomNumber = model.RoomNumber,
                        RoomLocation = model.RoomLocation,
                        CallLogFinanceUrgencyID = model.CallLogFinanceUrgencyID,
                        CallLogStatusID = model.CallLogStatusID,
                        CallLogFITSID = model.CallLogFITSID,
                        PSMWorkAssignmentStatusId = model.CapitalDefectCalloutViewModel.PSMWorkAssignmentStatusId,
                        RequestedBy = model.RequestedBy,
                        RequesterPosition = model.RequesterPosition,
                        CallLogDesc = model.CallLogDesc,
                        CallStatusComments = model.CallStatusComments,
                        PMComments = model.CapitalDefectCalloutViewModel.PMComments,
                        Complete = model.CapitalDefectCalloutViewModel.Complete,
                        FirstEmailSentDate = model.CapitalDefectCalloutViewModel.FirstEmailSentDate,
                        LastEmailSentDate = model.CapitalDefectCalloutViewModel.LastEmailSentDate,
                        CallDate = model.CallDate,
                        RecordedBy = model.CapitalDefectCalloutViewModel.RecordedBy,
                        RecordedDate = model.CapitalDefectCalloutViewModel.RecordedDate,
                        LastRecordedDate = model.CapitalDefectCalloutViewModel.LastRecordedDate,
                        CompletedDate = model.CapitalDefectCalloutViewModel.CompletedDate,
                        CallAssignedDate = model.CapitalDefectCalloutViewModel.CallAssignedDate,
                        DefectFlaggedDate = model.CapitalDefectCalloutViewModel.DefectFlaggedDate,
                        DefectReversalDate = model.CapitalDefectCalloutViewModel.DefectReversalDate,
                    };

                    _context.PSMDefectCallLog.Add(callLogToBeSaved);
                    _context.SaveChanges();
                    model.CapitalDefectCalloutViewModel.PSMDefectCallLogId = callLogToBeSaved.PSMDefectCallLogId;
                }
            }
            else
            {
                if (callLogToBeSaved != null)
                {
                    //Existing defect update
                    helper.updateChangedFields(model.UserCapabilities.CapitalDefectSection, model.CapitalDefectCalloutViewModel, callLogToBeSaved);
                    helper.updateChangedFields(model.UserCapabilities.MainSection, model, callLogToBeSaved);

                    callLogToBeSaved.LastRecordedBy = currentUser.UserCode;
                    callLogToBeSaved.LastRecordedDate = DateTime.Now;

                    _context.SaveChanges();
                }
            }

            //4. Resave buildings if Job buildings is an editable field
            if (!HelperMethods.DisableField(model.UserCapabilities, "mainSection", "JobBuildingIDs"))
            {
                List<int> existingBuildingIds = _context.PSMDefectCallLogBuilding.Where(x => x.PSMDefectCallLogId == model.CapitalDefectCalloutViewModel.PSMDefectCallLogId && x.Deleted != true).Select(y => y.BuildingId).ToList();
                List<int> deletedBuildingIds = existingBuildingIds.Except(model.JobBuildingIDs).ToList();
                List<int> newBuildingIds = model.JobBuildingIDs.Except(existingBuildingIds).ToList();

                foreach (int delBuilding in deletedBuildingIds)
                {
                    var deletedBuilding = _context.PSMDefectCallLogBuilding.Where(x => x.PSMDefectCallLogId == model.CapitalDefectCalloutViewModel.PSMDefectCallLogId &&
                   x.AgencyId == model.AgencyID && x.PropertyId == model.PropertyID &&
                   x.BuildingId == delBuilding && x.Deleted != true).FirstOrDefault();
                    if (deletedBuilding != null)
                    {
                        deletedBuilding.LastUpdated = DateTime.Now;
                        deletedBuilding.Deleted = true;
                        _context.SaveChanges();
                    }
                }

                foreach (int buildingId in newBuildingIds)
                {
                    PSMDefectCallLogBuilding buildings = new PSMDefectCallLogBuilding()
                    {
                        AgencyId = model.AgencyID,
                        PropertyId = model.PropertyID,
                        PSMDefectCallLogId = model.CapitalDefectCalloutViewModel.PSMDefectCallLogId,
                        BuildingId = buildingId,
                        LastUpdated = DateTime.Now,

                    };

                    _context.PSMDefectCallLogBuilding.Add(buildings);
                    _context.SaveChanges();

                }
            }
        }

        private void UpdateCapitalDefectUserCapabilities(CallLogFITS oldResult, UserViewModel currentUser, ref CallOutAddEditViewModel model)
        {
            HelperMethods helper = new HelperMethods();

            if (oldResult.CapitalDefect && !model.CapitalDefect && oldResult.CallLogTypeID == 3 && model.CallLogTypeID != 3)
            {
                if (currentUser.RoleID != "PM" && currentUser.RoleID != "PD"
                            || !model.CapitalDefectCalloutViewModel.PSMWorkAssignmentStatusId.HasValue
                            || model.CapitalDefectCalloutViewModel.PSMWorkAssignmentStatusId != 3)
                {
                    RemoveUserCapabilityForDefect(model, helper);
                }
            }

            if (!oldResult.CapitalDefect && model.CapitalDefect && oldResult.CallLogTypeID == 1 && model.CallLogTypeID == 3)
            {
                if (currentUser.RoleID != "PM" && currentUser.RoleID != "PD")
                {
                    var oldDefectData = _context.PSMDefectCallLog.Where(x => x.CallLogFITSID == oldResult.CallLogFITSID)?.FirstOrDefault();
                    if (oldDefectData != null && oldDefectData.PSMWorkAssignmentStatusId == 3 && (!model.CapitalDefectCalloutViewModel.PSMWorkAssignmentStatusId.HasValue
                            || model.CapitalDefectCalloutViewModel.PSMWorkAssignmentStatusId != 3))
                    {
                        RemoveUserCapabilityForDefect(model, helper);
                    }
                }
            }
        }

        private static void RemoveUserCapabilityForDefect(CallOutAddEditViewModel model, HelperMethods helper)
        {
            model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "CallLogTypeID");
            if (model.IsInsuranceEvent == true)
            {
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "EventNo");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "LocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "IsThirdpartyInvolved");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "CauseOfTheIssue");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SchoolIncidentCauseId");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "IsReadyForReimbursement");
            }
            if (model.IsSecInsuranceEvent == true)
            {
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecEventNo");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecLocalCotractorToRepairDamage");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecIsThirdpartyInvolved");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecCauseOfTheIssue");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecSchoolIncidentCauseId");
                model.UserCapabilities.MainSection = helper.RemoveUserCapability(model.UserCapabilities.MainSection, "SecIsReadyForReimbursement");
            }
        }

        #endregion
    }
}