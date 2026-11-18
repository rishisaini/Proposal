using AssetCare.Model.Common;
using AssetCare.Model.Models;
using AssetCare.Model.ViewModels;
using AssetCare.Repository.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Linq.Dynamic;

namespace AssetCare.Repository
{
    //Test
    public partial class FMProposalRepository : GenericRepository<Proposal>, IFMProposalRepository
    {
        private readonly IMapper _mapper;
        public FMProposalRepository(AssetCareContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Saving New Proposal
        /// </summary>
        /// <param name="model"></param>
        /// <param name="loggedInUser"></param>
        /// <param name="callLogId"></param>
        /// <returns></returns>
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

        public bool Save(ProposalAddEditViewModel model, string loggedInUser, out int callLogId)
        {
            Proposal dataToSave = MapProposalModelToEntity(model);
            dataToSave.RecordedDate = DateTime.Now;
            dataToSave.LastRecordedBy = loggedInUser;
            dataToSave.RecordedBy = loggedInUser;
            dataToSave.WorkDescription = dataToSave.WorkDescription != null ? dataToSave.WorkDescription.Trim() : null;


            if (model.Submitted && (model.CurrentRoleID == "AO" || model.CurrentRoleID == "D"))
            {
                dataToSave.SubmittedBy = model.Approver;
                dataToSave.SubmittedtDate = DateTime.Now;
            }
            using (_context)
            {
                using (var dbContextTransaction = _context.Database.BeginTransaction())
                {
                    int? callLogSequenceId = _context.Proposal.Where(x => x.AgencyID == model.AgencyId).Max(x => (int?)x.AgencySequenceID);
                    dataToSave.AgencySequenceID = (callLogSequenceId == null ? 0 : callLogSequenceId.Value) + 1;

                    _context.Proposal.Add(dataToSave);
                    _context.SaveChanges();

                    callLogId = dataToSave.AgencySequenceID.Value;
                    model.AgencySequenceId = callLogId;
                    model.ProposalId = dataToSave.ProposalID;

                    //Saving Building data
                    if (model.JobBuildingIDs != null)
                    {
                        foreach (int buildingId in model.JobBuildingIDs)
                        {
                            ProposalBuilding buildings = new ProposalBuilding()
                            {
                                AgencyID = model.AgencyId,
                                PropertyID = model.PropertyId,
                                ProposalID = dataToSave.ProposalID,
                                BuildingID = buildingId,
                                LastUpdated = DateTime.Now
                            };
                            _context.ProposalBuildings.Add(buildings);
                            _context.SaveChanges();
                        }
                    }

                    //Saving Entitlements
                    if (model.Entitlements != null)
                    {
                        _context.ProposalEntitlement.RemoveRange(_context.ProposalEntitlement.Where(x => x.ProposalID == dataToSave.ProposalID));
                        _context.SaveChanges();

                        ProposalEntitlement entitlements = new ProposalEntitlement()
                        {
                            ProposalID = dataToSave.ProposalID,
                            REF = model.Entitlements.REF,
                            DA = model.Entitlements.DA,
                            Exempt = model.Entitlements.Exempt,
                            WithConsent = model.Entitlements.WithConsent,
                            WithoutConsent = model.Entitlements.WithoutConsent,
                            CDC = model.Entitlements.CDC,
                            Arboist = model.Entitlements.Arboist,
                            Bushfire = model.Entitlements.Bushfire,
                            Contamination = model.Entitlements.Contamination,
                            FloodProne = model.Entitlements.FloodProne,
                            Heritage = model.Entitlements.Heritage,
                            Ecology = model.Entitlements.Ecology,
                            Archaeology = model.Entitlements.Archaeology,
                            PRNone = model.Entitlements.PRNone,
                            EPNone = model.Entitlements.EPNone,
                            SSU = model.Entitlements.SSU,
                            ITD = model.Entitlements.ITD,
                            Demountable = model.Entitlements.Demountable,
                            AssetActivation = model.Entitlements.AssetActivation,
                            SCNone = model.Entitlements.SCNone,
                            ServiceIntereption = model.Entitlements.ServiceIntereption,
                            ExamsInOperation = model.Entitlements.ExamsInOperation,
                            VacationPeriod = model.Entitlements.VacationPeriod,
                            Relocation = model.Entitlements.Relocation,
                            OCNone = model.Entitlements.OCNone,
                            TSNone = model.Entitlements.TSNone,
                            Asbestos = model.Entitlements.Asbestos,
                            Lead = model.Entitlements.Lead,
                            Mould = model.Entitlements.Mould,
                            HMNone = model.Entitlements.HMNone,
                            Electrical = model.Entitlements.Electrical,
                            ElectricalYes = model.Entitlements.ElectricalYes,
                            ElectricalNo = model.Entitlements.ElectricalNo,
                            Hydraulics = model.Entitlements.Hydraulics,
                            Sewer = model.Entitlements.Sewer,
                            Mechanical = model.Entitlements.Mechanical,
                            Stormwater = model.Entitlements.Stormwater,
                            IT = model.Entitlements.IT,
                            Security = model.Entitlements.Security,
                            SNone = model.Entitlements.SNone,
                            LastRecordedBy = loggedInUser,
                            LastRecordedDate = DateTime.Now,
                            RecordedBy = loggedInUser,
                            RecordedDate = DateTime.Now,
                            LearningWellbeing = model.Entitlements.LearningWellbeing,
                            WHS = model.Entitlements.WHS,
                            StatPlanning = model.Entitlements.StatPlanning,
                            HeritageTech = model.Entitlements.TechHeritage,
                            Compliance = model.Entitlements.Compliance,
                            Sustainability = model.Entitlements.Sustainability,
                            Comms = model.Entitlements.Comms,
                            LeaseLicense = model.Entitlements.Leaseslicences,
                            CentralService = model.Entitlements.CentralServices
                        };
                        ProposalEntitlement entitlements1 = _context.ProposalEntitlement.SingleOrDefault(x => x.ProposalID == dataToSave.ProposalID);

                        if (entitlements1 != null)
                        {

                            _context.SaveChanges();
                        }



                        _context.ProposalEntitlement.Add(entitlements);
                        _context.SaveChanges();
                    }

                    if (model.ProposalWorkNominatedID == 1 || model.ProposalWorkNominatedID == 2)
                    {
                        if (model.SchoolEndorsements != null)
                        {
                            List<EndorsementViewModel> schoolEndorsement = model.SchoolEndorsements.Where(x => x.Approved == true).Distinct().ToList();
                            if (schoolEndorsement != null || schoolEndorsement.Count > 0)
                            {
                                foreach (var data in schoolEndorsement)
                                {
                                    ProposalEndorsement end = new ProposalEndorsement()
                                    {
                                        ProposalID = dataToSave.ProposalID,
                                        RecordedBy = dataToSave.RecordedBy,
                                        RecordedDate = dataToSave.RecordedDate,
                                        LastRecordedBy = dataToSave.RecordedBy,
                                        LastRecordedDate = dataToSave.LastRecordedDate,
                                        ProposalEndorsementTypeID = (int)data.EndorsementTypeID,
                                        Position = data.Position,
                                        Approved = (bool?)data.Approved,
                                        ApprovalDate = dataToSave.RecordedDate,
                                        ApprovedBy = dataToSave.RecordedBy
                                    };

                                    _context.ProposalEndorsement.Add(end);
                                    _context.SaveChanges();
                                };

                            }
                        }
                    }

                    //saving endorsements
                    if (model.Endorsements != null)
                    {
                        List<EndorsementViewModel> endorsement = model.Endorsements.Where(x => x.Approved == true).Distinct().ToList();

                        if (endorsement != null && endorsement.Count > 0)
                        {
                            foreach (var data in endorsement)
                            {
                                ProposalEndorsement end = new ProposalEndorsement()
                                {
                                    ProposalID = dataToSave.ProposalID,
                                    RecordedBy = dataToSave.RecordedBy,
                                    RecordedDate = dataToSave.RecordedDate,
                                    LastRecordedBy = dataToSave.RecordedBy,
                                    LastRecordedDate = dataToSave.LastRecordedDate,
                                    ProposalEndorsementTypeID = (int)data.EndorsementTypeID,
                                    Position = model.Position,
                                    Approved = (bool?)data.Approved,
                                    ApprovalDate = dataToSave.RecordedDate,
                                    ApprovedBy = model.Approver
                                };
                                _context.ProposalEndorsement.Add(end);
                                _context.SaveChanges();
                            };
                        }
                    }

                    int documentSequenceNumber = 0;
                    //update Support Documents with the calllog Id
                    foreach (SupportDocumentViewModel doc in model.SupportDocuments)
                    {
                        var supportDocument = _context.SupportDocument.Where(x => x.SupportDocumentID == doc.SupportDocumentID).FirstOrDefault();
                        documentSequenceNumber++;
                        supportDocument.MasterRecordID = model.ProposalId;
                        supportDocument.DocumentSequenceID = documentSequenceNumber;
                        _context.SaveChanges();

                    }
                    dbContextTransaction.Commit();
                }

            }

            return true;
        }

        /// <summary>
        /// Updating proposal data
        /// </summary>
        /// <param name="model"></param>
        /// <param name="loggedInUser"></param>
        /// <returns></returns>
        public bool Update(ProposalAddEditViewModel model, string loggedInUser)
        {
            HelperMethods helper = new HelperMethods();
            Proposal dataToSave = MapProposalModelToEntity(model);
            dataToSave.ProposalSubmission = model.ProposalSubmissionDisabled == true ? model.ProposalSubmissionDisabled : model.ProposalSubmission;
            //dataToSave.AgencySequenceID = model.CallOutId.Value;
            dataToSave.LastRecordedBy = loggedInUser;
            dataToSave.LastRecordedDate = DateTime.Now;

            if (model.Submitted && (model.CurrentRoleID == "AO" || model.CurrentRoleID == "D"))
            {
                dataToSave.SubmittedBy = model.Approver;
                dataToSave.SubmittedtDate = DateTime.Now;
            }
            bool workCategoryChanged = false;
            int existingWorkCategory = 0;
            bool updateBuildings = false;
            if (model.CurrentRoleID == "F" || model.CurrentRoleID == "SM" || model.CurrentRoleID == "SD" || model.CurrentRoleID == "ED")
            {
                updateBuildings = true;
            }
            using (_context)
            {
                using (var dbContextTransaction = _context.Database.BeginTransaction())
                {
                    Proposal result = _context.Proposal.SingleOrDefault(x => x.ProposalID == model.CallOutId && x.AgencyID == model.AgencyId);
                    var resSubmitted = result.Submitted;
                    var resSubmittedBy = result.SubmittedBy;
                    var resSubmittedDate = result.SubmittedtDate;
                    var resConceptEstimate = result.ConceptEstimate;
                    if (result != null)
                    {
                        //workCategoryChanged = result.WorkCategoryID != model.WorkCategoryId;
                        //existingWorkCategory = result.WorkCategoryID.Value;

                        dataToSave.ProposalID = result.ProposalID;
                        dataToSave.RecordedBy = result.RecordedBy;
                        dataToSave.RecordedDate = result.RecordedDate;
                        dataToSave.Emailsent = result.Emailsent;
                        if (result.Submitted)
                            dataToSave.Submitted = result.Submitted;
                        CopyValues(result, dataToSave);
                        //helper.updateChangedFields(model.UserCapabilities.DetailSection, model, result);
                        //helper.updateChangedFields(model.UserCapabilities.EntitlementSection, model, result);
                        //if (dataToSave.SchoolFunded == false)
                        //    result.FundsAvailability = dataToSave.FundsAvailability;
                        // if (result.ProposalSFCID ==null)
                        if (resSubmitted)
                        {
                            dataToSave.SubmittedBy = resSubmittedBy;
                            dataToSave.SubmittedtDate = resSubmittedDate;
                            dataToSave.ConceptEstimate = resConceptEstimate;
                        }
                        result.FundsAvailability = dataToSave.AMUFunded == true ? null : result.FundsAvailability;
                        result.ProposalSFCID = dataToSave.ProposalSFCID;
                        result.RoomNumber = dataToSave.RoomNumber;
                        ///result.PlannedMaintenance = dataToSave.PlannedMaintenance;
                        if (model.CallLogFITSID == null && model.DeliveryTypeLRLV && (model.Endorsements[2].Approved == true || model.Endorsements[3].Approved == true))
                        {
                            result.AwaitingActionBy = "None";
                            result.RoleID = "";
                        }



                        _context.SaveChanges();
                    }
                    if (resSubmitted != true)
                    {
                        if (!model.DisableField("entitlementsection", "REF") || updateBuildings)//TODO:
                        {
                            //delete all the buidings
                            //Save Buildings
                            _context.ProposalBuildings.RemoveRange(_context.ProposalBuildings.Where(x => x.ProposalID == result.ProposalID));
                            _context.SaveChanges();

                            //Existing building ids
                            List<int> existingBuildingIds = _context.ProposalBuildings.Where(x => x.ProposalID == result.ProposalID && x.Deleted != true).Select(y => y.BuildingID).ToList();
                            //if existing building id's exists, delete them first then add new building id's
                            if (model.JobBuildingIDs != null)
                            {
                                List<int> deletedBuildingIds = existingBuildingIds.Except(model.JobBuildingIDs).ToList();// list of building id's needs to be deleted.
                                List<int> newBuildingIds = model.JobBuildingIDs.Except(existingBuildingIds).ToList();// list of building id's needs to be added
                                foreach (int delBuilding in deletedBuildingIds)
                                {
                                    var deletedBuilding = _context.ProposalBuildings.Where(x => x.ProposalID == result.ProposalID &&
                                   x.AgencyID == model.AgencyId && x.PropertyID == model.PropertyId &&
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
                                    if (!(_context.ProposalBuildings.Where(x => x.ProposalID == result.ProposalID &&
                                     x.AgencyID == model.AgencyId && x.PropertyID == model.PropertyId &&
                                     x.BuildingID == buildingId && x.Deleted != true).Any()))
                                    {
                                        ProposalBuilding buildings = new ProposalBuilding()
                                        {
                                            AgencyID = model.AgencyId,
                                            PropertyID = model.PropertyId,
                                            ProposalID = result.ProposalID,
                                            BuildingID = buildingId,
                                            LastUpdated = DateTime.Now
                                        };
                                        _context.ProposalBuildings.Add(buildings);
                                        _context.SaveChanges();
                                    }
                                }
                            }
                        }
                    }


                    if (model.ProposalWorkNominatedID == 1 || model.ProposalWorkNominatedID == 2)
                    {
                        //School Endorsement
                        if (model.SchoolEndorsements != null)
                        {
                            List<EndorsementViewModel> schoolEndorsement = model.SchoolEndorsements.Where(x => x.Approved != null).Distinct().ToList();

                            if (schoolEndorsement != null && schoolEndorsement.Count > 0)
                            {
                                foreach (var data in schoolEndorsement)
                                {
                                    var existingData = _context.ProposalEndorsement.SingleOrDefault(b => b.ProposalID == model.ProposalId && b.ProposalEndorsementTypeID == data.EndorsementTypeID);
                                    if (existingData != null)
                                    {
                                        //existingData.RecordedDate = dataToSave.RecordedDate;
                                        existingData.LastRecordedDate = dataToSave.LastRecordedDate;
                                        existingData.Position = data.Position;
                                        existingData.Approved = (bool?)data.Approved;
                                        existingData.Comments = data.Comments;
                                        _context.SaveChanges();
                                    }
                                    else
                                    {
                                        ProposalEndorsement end = new ProposalEndorsement()
                                        {
                                            ProposalID = dataToSave.ProposalID,
                                            RecordedBy = dataToSave.RecordedBy,
                                            RecordedDate = dataToSave.RecordedDate,
                                            LastRecordedBy = dataToSave.RecordedBy,
                                            LastRecordedDate = dataToSave.LastRecordedDate,
                                            ProposalEndorsementTypeID = (int)data.EndorsementTypeID,
                                            Position = model.Position,
                                            Approved = (bool?)data.Approved,
                                            ApprovalDate = DateTime.Now,
                                            ApprovedBy = model.Approver,
                                            Comments = data.Comments
                                        };
                                        _context.ProposalEndorsement.Add(end);
                                        _context.SaveChanges();
                                    }
                                };

                            }
                        }
                    }

                    if (model.Endorsements != null)
                    {
                        //Checking Previous Endorsement and set EmailSent = True, when new Endorsement created...
                        var previousEndorsement = _context.ProposalEndorsement.Where(b => b.ProposalID == model.ProposalId && b.Emailsent != true).Distinct().ToList();
                        if (previousEndorsement != null && previousEndorsement.Count > 0)
                        {
                            foreach (var data in previousEndorsement)
                            {
                                var existingData = _context.ProposalEndorsement.SingleOrDefault(b => b.ProposalEndorsementID == data.ProposalEndorsementID);
                                if (existingData != null)
                                {
                                    existingData.Emailsent = true;
                                    _context.SaveChanges();
                                }
                            }
                        }

                        //Endorsement
                        List<EndorsementViewModel> endorsement = model.Endorsements.Where(x => x.Approved != null).Distinct().ToList();

                        if (endorsement != null && endorsement.Count > 0)
                        {
                            foreach (var data in endorsement)
                            {
                                var existingData = _context.ProposalEndorsement.SingleOrDefault(b => b.ProposalID == model.ProposalId && b.ProposalEndorsementTypeID == data.EndorsementTypeID);
                                if (existingData != null)
                                {
                                    //existingData.RecordedDate = dataToSave.RecordedDate;
                                    existingData.LastRecordedDate = dataToSave.LastRecordedDate;
                                    existingData.Position = data.Position;
                                    existingData.Approved = (bool?)data.Approved;
                                    existingData.Comments = data.Comments;
                                    //existingData.ApprovalDate = dataToSave.RecordedDate;
                                    _context.SaveChanges();
                                }
                                else
                                {
                                    ProposalEndorsement end = new ProposalEndorsement()
                                    {
                                        ProposalID = dataToSave.ProposalID,
                                        RecordedBy = dataToSave.RecordedBy,
                                        RecordedDate = dataToSave.RecordedDate,
                                        LastRecordedBy = dataToSave.RecordedBy,
                                        LastRecordedDate = dataToSave.LastRecordedDate,
                                        ProposalEndorsementTypeID = (int)data.EndorsementTypeID,
                                        Position = model.Position,
                                        Approved = (bool?)data.Approved,
                                        ApprovalDate = DateTime.Now,
                                        ApprovedBy = model.Approver,
                                        Comments = data.Comments
                                    };
                                    _context.ProposalEndorsement.Add(end);
                                    _context.SaveChanges();
                                    

                                }
                                var approvedProposal = data.Approved ?? false;

                                if (model.CallLogFITSID == null && model.DeliveryTypeLRLV && approvedProposal == true && (data.EndorsementTypeID == 7 || data.EndorsementTypeID == 6))//TODO: change this later
                                {
                                    //Initial user who has created proposal...
                                    var usercode = _context.Proposal.Where(x => x.ProposalID == dataToSave.ProposalID).FirstOrDefault().RecordedBy;
                                    User user = _context.Users.FirstOrDefault(p => p.UserCode == usercode);

                                    //Saving callout details...
                                    CallOutAddEditViewModel calloutModel = new CallOutAddEditViewModel();
                                    CallLogFITS CalloutdataToSave = _mapper.Map<CallLogFITS>(calloutModel); // MapCallOutModelToEntity(model);
                                    CalloutdataToSave.RecordedDate = DateTime.Now;
                                    CalloutdataToSave.LastRecordedBy = loggedInUser;
                                    CalloutdataToSave.RecordedBy = loggedInUser;
                                    CalloutdataToSave.CallLogDesc = result.WorkDescription.Trim();
                                    CalloutdataToSave.CallDate = dataToSave.ProposalDate;
                                    CalloutdataToSave.CallLogFinanceUrgencyID = 4;//set default value to (Non-Urgent (within 5 days))
                                    CalloutdataToSave.SchoolUrgencyID = 4;
                                    CalloutdataToSave.RoomNumber = result.RoomNumber;
                                    CalloutdataToSave.RoomLocation = result.RoomLocation;
                                    CalloutdataToSave.AgencyID = dataToSave.AgencyID;
                                    CalloutdataToSave.PropertyID = dataToSave.PropertyID;
                                    CalloutdataToSave.IsInsuranceEvent = false;//No by default
                                    CalloutdataToSave.RoleID = "SM";//default
                                    CalloutdataToSave.RequesterPosition = user.Role.RoleDescNew;
                                    CalloutdataToSave.RequestedBy = user.FirstName + " " + user.LastName;
                                    CalloutdataToSave.RequesterContact = user.Mobile ?? user.Phone;
                                    CalloutdataToSave.CallLogTypeID = dataToSave.PlannedMaintenance? 10: 4;//Default Proposal
                                    CalloutdataToSave.WorkCategoryID = 1;//default
                                    CalloutdataToSave.CallLogStatusID = 1;//Default to Registered
                                    CalloutdataToSave.WorkTypeId = 1;//Default to New(Addition)
                                    CalloutdataToSave.ElementId = 1;//Default to Roof and Roof structure
                                    CalloutdataToSave.HAZMAT = dataToSave.Hazmat == false ? "No" : "Yes";
                                   
                                        //int retryCount = 0;
                                        //while (retryCount < 5)
                                        //{
                                        //    int? callLogSequenceId = _context.CallLogFITS.Where(x => x.AgencyID == model.AgencyId).Max(x => (int?)x.AgencySequenceID);
                                        //CalloutdataToSave.AgencySequenceID = (callLogSequenceId == null ? 0 : callLogSequenceId.Value) + 1;
                                        //    try
                                        //    {
                                        //        _context.CallLogFITS.Add(CalloutdataToSave);
                                        //        _context.SaveChanges();
                                        //        break;
                                        //    }
                                        //    catch (DbUpdateException ex)
                                        //    {
                                        //        if (retryCount >= 4)
                                        //            throw ex;
                                        //        if (ex.InnerException.InnerException.ToString().Contains("'UC_Agency_AgencySequenceId_CallOut'"))
                                        //        {
                                        //            retryCount++;
                                        //            continue;
                                        //        }

                                        //    }
                                        //}
                                        //dataToSave.AgencySequenceID = GetNextSequenceId(model.AgencyID);

                                        CalloutdataToSave.AgencySequenceID = GetNextSequenceId(model.AgencyId);
                                        _context.CallLogFITS.Add(CalloutdataToSave);
                                        _context.SaveChanges();

                                        //callLogId = (int)dataToSave.AgencySequenceID;
                                        //model.AgencySequenceID = callLogId;
                                        //model.CallLogFITSID = dataToSave.CallLogFITSID;

                                    //callLogId = CalloutdataToSave.AgencySequenceID.Value;
                                    //calloutModel.AgencySequenceID = callLogId;
                                    dataToSave.CallLogFITSID = CalloutdataToSave.CallLogFITSID;
                                    result.CallLogFITSID = CalloutdataToSave.CallLogFITSID;
                                    _context.SaveChanges();

                                    //Save Buildings
                                    if (model.JobBuildingIDs != null)
                                    {
                                        List<int> existingBuildingIds = _context.CallLogBuildingFITSs.Where(x => x.CallLogFITSID == result.CallLogFITSID && x.Deleted != true).Select(y => y.BuildingID).ToList();
                                        List<int> deletedBuildingIds = existingBuildingIds.Except(model.JobBuildingIDs).ToList();
                                        List<int> newBuildingIds = model.JobBuildingIDs.Except(existingBuildingIds).ToList();
                                        foreach (int delBuilding in deletedBuildingIds)
                                        {
                                            var deletedBuilding = _context.CallLogBuildingFITSs.Where(x => x.CallLogFITSID == result.CallLogFITSID &&
                                           x.AgencyID == model.AgencyId && x.PropertyID == model.PropertyId &&
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
                                             x.AgencyID == model.AgencyId && x.PropertyID == model.PropertyId &&
                                             x.BuildingID == buildingId && x.Deleted != true).Any()))
                                            {
                                                CallLogBuildingFITS buildings = new CallLogBuildingFITS()
                                                {
                                                    AgencyID = model.AgencyId,
                                                    PropertyID = model.PropertyId,
                                                    CallLogFITSID = (int)result.CallLogFITSID,
                                                    BuildingID = buildingId,
                                                    LastUpdated = DateTime.Now
                                                };
                                                _context.CallLogBuildingFITSs.Add(buildings);
                                                _context.SaveChanges();
                                            }
                                        }
                                    }

                                }
                            };

                        }
                    }
                    //It will not update after submitted...
                    if (!model.DisableField("entitlementsection", "REF") && resSubmitted != true && (model.Endorsements == null && model.SchoolEndorsements==null))
                    {
                        if (model.Entitlements != null)
                        {
                            //_context.ProposalEntitlement.RemoveRange(_context.ProposalEntitlement.Where(x => x.ProposalID == result.ProposalID));
                            //_context.SaveChanges();
                            var existing = _context.ProposalEntitlement.FirstOrDefault(x => x.ProposalID == result.ProposalID);

                            if (existing != null)
                            {
                                // Update existing record
                               
                                //}
                                //ProposalEntitlement entitlements = new ProposalEntitlement()
                                //{
                                //existing.ProposalID = result.ProposalID;
                                existing.REF = model.Entitlements.REF;
                                existing.DA = model.Entitlements.DA;
                                existing.Exempt = model.Entitlements.Exempt;
                                existing.WithConsent = model.Entitlements.WithConsent;
                                existing.WithoutConsent = model.Entitlements.WithoutConsent;
                                existing.CDC = model.Entitlements.CDC;
                                existing.Arboist = model.Entitlements.Arboist;
                                existing.Bushfire = model.Entitlements.Bushfire;
                                existing.Contamination = model.Entitlements.Contamination;
                                existing.FloodProne = model.Entitlements.FloodProne;
                                existing.Heritage = model.Entitlements.Heritage;
                                existing.Ecology = model.Entitlements.Ecology;
                                existing.Archaeology = model.Entitlements.Archaeology;
                                existing.PRNone = model.Entitlements.PRNone;
                                existing.EPNone = model.Entitlements.EPNone;
                                existing.SSU = model.Entitlements.SSU;
                                existing.ITD = model.Entitlements.ITD;
                                existing.Demountable = model.Entitlements.Demountable;
                                existing.AssetActivation = model.Entitlements.AssetActivation;
                                existing.SCNone = model.Entitlements.SCNone;
                                existing.ServiceIntereption = model.Entitlements.ServiceIntereption;
                                existing.ExamsInOperation = model.Entitlements.ExamsInOperation;
                                existing.VacationPeriod = model.Entitlements.VacationPeriod;
                                existing.Relocation = model.Entitlements.Relocation;
                                existing.OCNone = model.Entitlements.OCNone;
                                existing.TSNone = model.Entitlements.TSNone;
                                existing.Asbestos = model.Entitlements.Asbestos;
                                existing.Lead = model.Entitlements.Lead;
                                existing.Mould = model.Entitlements.Mould;
                                existing.HMNone = model.Entitlements.HMNone;
                                existing.Electrical = model.Entitlements.Electrical;
                                existing.ElectricalYes = model.Entitlements.ElectricalYes;
                                existing.ElectricalNo = model.Entitlements.ElectricalNo;
                                existing.Hydraulics = model.Entitlements.Hydraulics;
                                existing.Sewer = model.Entitlements.Sewer;
                                existing.Mechanical = model.Entitlements.Mechanical;
                                existing.Stormwater = model.Entitlements.Stormwater;
                                existing.IT = model.Entitlements.IT;
                                existing.Security = model.Entitlements.Security;
                                existing.SNone = model.Entitlements.SNone;
                                existing.LastRecordedBy = loggedInUser;
                                existing.LastRecordedDate = DateTime.Now;
                                existing.RecordedBy = loggedInUser;
                                existing.RecordedDate = DateTime.Now;
                                existing.LearningWellbeing = model.Entitlements.LearningWellbeing;
                                existing.WHS = model.Entitlements.WHS;
                                existing.StatPlanning = model.Entitlements.StatPlanning;
                                existing.HeritageTech = model.Entitlements.TechHeritage;
                                existing.Compliance = model.Entitlements.Compliance;
                                existing.Sustainability = model.Entitlements.Sustainability;
                                existing.Comms = model.Entitlements.Comms;
                                existing.LeaseLicense = model.Entitlements.Leaseslicences;
                                existing.CentralService = model.Entitlements.CentralServices;
                            } 
                            //ProposalEntitlement entitlements1 = _context.ProposalEntitlement.SingleOrDefault(x => x.ProposalID == result.ProposalID);

                            //if (entitlements1 != null)
                            //{
                            //    _context.SaveChanges();
                            //}
                            //_context.ProposalEntitlement.Add(entitlements);
                            _context.SaveChanges();
                        }
                    }

                    //if workCategory is changed, update all the documents, document class and type
                    if (workCategoryChanged)
                    {
                        List<SupportDocument> docs = _context.SupportDocument
                                                     .Where(x => x.SupportDocumentClassID == ((existingWorkCategory == 13 ? (int)SupportDocumentClassEnum.WORKORDER : existingWorkCategory))
                                                             && x.MasterRecordID == dataToSave.ProposalID)
                                                     .ToList();
                        foreach (SupportDocument doc in docs)
                        {
                            var supportDocument = _context.SupportDocument.Where(x => x.SupportDocumentID == doc.SupportDocumentID).FirstOrDefault();
                            if (supportDocument != null)
                            {
                                int currentClassId = supportDocument.SupportDocumentClassID;
                                supportDocument.SupportDocumentClassID = (model.WorkCategoryId == 13 ? (int)SupportDocumentClassEnum.WORKORDER : model.WorkCategoryId.Value);



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

        public IEnumerable<ProposalForHPViewModel> FilterProposalDataForHP(string RoleID, ProposalFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, out int filteredResultsCount, out int totalResultsCount)
        {
            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
            {
                var jobs = _context.ProposalEndorsement;
                var callOuts = RoleID == "SO" ?
                            _context.Proposal

                            .Where(
                                    x => x.AgencyID == filterModel.AgencyId //&& x.RoleID == "SO" 
                                    &&
                                    (filterModel.SchoolId > 0 ? (x.PropertyID == filterModel.SchoolId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                                    (x.DeletedYN == null || x.DeletedYN == false) && x.Submitted == false && x.ProposalStatusID != 2 && x.ProposalStatusID != 15 && x.ProposalStatusID != 3) :
                            _context.Proposal

                            .Where(
                                    x => x.AgencyID == filterModel.AgencyId //&& x.RoleID != "SO"
                                                                            &&
                                    (filterModel.SchoolId > 0 ? (x.PropertyID == filterModel.SchoolId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                                    (x.DeletedYN == null || x.DeletedYN == false) && x.Submitted == false && x.ProposalStatusID != 2 && x.ProposalStatusID != 15 && x.ProposalStatusID != 3);
                // var jobs = _context.ServiceReports.Where(j => j.AgencyId == filterModel.AgencyId);
                IQueryable<ProposalForHPViewModel> searchResults = callOuts
                               .Select(
                                   coj => new ProposalForHPViewModel
                                   {
                                       Id = coj.ProposalID,
                                       AgencyId = coj.AgencyID,
                                       SchoolId = coj.PropertyID,
                                       SchoolName = coj.Property.PropertyShortDesc,
                                       ProposalDate = coj.ProposalDate,
                                       ProposalDateList = coj.ProposalDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", coj.ProposalDate).ToString().Length) + SqlFunctions.DateName("dd", coj.ProposalDate) + "-" +
                                                                                    SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", coj.ProposalDate).ToString().Length) + SqlFunctions.DatePart("month", coj.ProposalDate) + "-" +
                                                                                    SqlFunctions.DateName("yyyy", coj.ProposalDate)
                                                                                    : "",

                                       RequestedBy = coj.RecordedBy,
                                       WorkNominatedBy = coj.ProposalWorkNominated.WorkNominatedName == null ? string.Empty : coj.ProposalWorkNominated.WorkNominatedName,
                                       Description = coj.WorkDescription,
                                       //includeFullDesc ? coj.WorkDescription : (String.IsNullOrEmpty(coj.WorkDescription) ? String.Empty :
                                       // (coj.WorkDescription.Length > 50 ? coj.WorkDescription.Substring(0, 50) + "..." : coj.WorkDescription)),
                                       Status = coj.ProposalStatus.ProposalStatusDesc,
                                       AwaitingActionBy = coj.AwaitingActionBy == null ? string.Empty : coj.AwaitingActionBy,
                                       ApprovalStatus = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Approved == true ? "Endorsed" : "Not Endorsed",
                                       ApprovalDate = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                                       ApprovedBy = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy == null ? string.Empty : _context.ProposalEndorsement.Where(r => r.ProposalID == coj.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy,
                                       ProjectID = coj.PMMProjectID,//_context.Projects.Where(r => r.ProposalID == coj.ProposalID).OrderByDescending(x => x.ProjectID).FirstOrDefault().ProjectID,
                                       DaysSinceLogged = 1,
                                   });

                var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                     ? searchResults
                      : searchResults;

                filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");

                int i = 1;
                foreach (var columnSearch in filterModel.ColumnSearches)
                {
                    if (columnSearch.Key.ToLower() == "ProposalDate".ToLower())
                    {
                        filtered = filtered.Where(f => ((f.ProposalDate.Value.Day < 10 ? "0" + f.ProposalDate.Value.Day.ToString() : f.ProposalDate.Value.Day.ToString()) + "-" +
                                              (f.ProposalDate.Value.Month < 10 ? "0" + f.ProposalDate.Value.Month.ToString() : f.ProposalDate.Value.Month.ToString()) + "-" +
                                              f.ProposalDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() == "ApprovalDate".ToLower())
                    {
                        filtered = filtered.Where(f => ((f.ApprovalDate.Value.Day < 10 ? "0" + f.ApprovalDate.Value.Day.ToString() : f.ApprovalDate.Value.Day.ToString()) + "-" +
                                              (f.ApprovalDate.Value.Month < 10 ? "0" + f.ApprovalDate.Value.Month.ToString() : f.ApprovalDate.Value.Month.ToString()) + "-" +
                                              f.ApprovalDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "LastApproved".ToLower())
                    {
                        //filtered = filtered.Where(f => ((f.LastApproved.Value.Day < 10 ? "0" + f.LastApproved.Value.Day.ToString() : f.LastApproved.Value.Day.ToString()) + "-" +
                        //                      (f.LastApproved.Value.Month < 10 ? "0" + f.LastApproved.Value.Month.ToString() : f.LastApproved.Value.Month.ToString()) + "-" +
                        //                      f.LastApproved.Value.Year.ToString())
                        //                      .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ApprovalStatus".ToLower())
                    {
                        //filtered = filtered.Where(f => f.ApprovalStatus.ToString().Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ProposalId".ToLower())
                    {
                        filtered = filtered.Where(f => f.ProposalID.ToString().StartsWith(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "DaysSinceLogged".ToLower())
                    {
                        // filtered = filtered.Where(f => f.ProposalID.ToString().Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() != "DaysSinceLogged")
                    {
                        filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                    }

                    i++;
                }

                filteredResultsCount = filtered.Count();
                totalResultsCount = searchResults.Count();
                transaction.Commit();

                if (filterModel.Take == -1)
                    return filtered;
                return filtered.OrderByDescending(x => x.Id).Skip(filterModel.Skip).Take(filterModel.Take);
            }
        }

        public IEnumerable<ProposalForHPViewModel> FilterProposalDataForEndorsement(string RoleID, ProposalFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, out int filteredResultsCount, out int totalResultsCount, string location)
        {
            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
            {
                var jobs = _context.ProposalEndorsement;
            var roleStatus = "D,AO";
                var proposalStatusID = _context.ProposalStatus.Where(x => x.ProposalStatusDesc == "Archived").FirstOrDefault().ProposalStatusID;
                var callOuts = location == "home" ?
                            _context.Proposal
                            .Where(
                                    x => x.AgencyID == filterModel.AgencyId &&
                                   (filterModel.SchoolId > 0 ? (x.PropertyID == filterModel.SchoolId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                                    (x.DeletedYN == null || x.DeletedYN == false) //&& x.ProposalStatusID != 2  && x.CallLogFITSID == null 
                                    && (filterModel.GrantApplication == true ? x.ProposalStatusID == proposalStatusID : x.ProposalStatusID != proposalStatusID))
                             .GroupJoin(
                                  jobs,
                                  co => co.ProposalID,
                                  j => j.ProposalID,
                                  (x, y) => new { co = x, js = y }) :

            _context.Proposal
                        .Where(
                                x => x.AgencyID == filterModel.AgencyId
                                && ((RoleID == "D" || RoleID == "AO" || RoleID == "P") ? roleStatus.Contains(x.RoleID) : RoleID == "AM" ? ((x.AwaitingActionBy == "None" && x.ProposalSubmission == false || x.RoleID == "AM")) : x.RoleID == RoleID)
                                && (filterModel.SchoolId > 0 ? (x.PropertyID == filterModel.SchoolId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                                (x.DeletedYN == null || x.DeletedYN == false) && x.ProposalStatusID != 2 && x.ProposalStatusID != 15 && x.CallLogFITSID == null)
             // var jobs = _context.ServiceReports.Where(j => j.AgencyId == filterModel.AgencyId);
             .GroupJoin(
                              jobs,
                              co => co.ProposalID,
                              j => j.ProposalID,
                              (x, y) => new { co = x, js = y });
                //.SelectMany(
                //    xy => xy.js.DefaultIfEmpty(),
                //    (x, y) => new { co = x.co, j = y });
                IQueryable<ProposalForHPViewModel> model = callOuts
                               .Select(
                                   coj => new ProposalForHPViewModel
                                   {
                                       Id = coj.co.ProposalID,
                                       ProposalID = coj.co.ProposalID,
                                       AgencyId = coj.co.AgencyID,
                                       SchoolId = coj.co.PropertyID,
                                       SchoolName = coj.co.Property.PropertyShortDesc,
                                       ProposalDate = coj.co.ProposalDate,
                                       ProposalDateList = coj.co.ProposalDate.HasValue ? SqlFunctions.Replicate("0", 2 - SqlFunctions.DateName("dd", coj.co.ProposalDate).ToString().Length) + SqlFunctions.DateName("dd", coj.co.ProposalDate) + "-" +
                                                                                    SqlFunctions.Replicate("0", 2 - SqlFunctions.DatePart("month", coj.co.ProposalDate).ToString().Length) + SqlFunctions.DatePart("month", coj.co.ProposalDate) + "-" +
                                                                                    SqlFunctions.DateName("yyyy", coj.co.ProposalDate)
                                                                                    : "",
                                       RequestedBy = coj.co.RecordedBy,
                                       WorkNominatedBy = coj.co.ProposalWorkNominated.WorkNominatedName == null ? string.Empty : coj.co.ProposalWorkNominated.WorkNominatedName,
                                       Description = coj.co.WorkDescription,
                                   //includeFullDesc ? coj.co.WorkDescription : (String.IsNullOrEmpty(coj.co.WorkDescription) ? String.Empty :
                                   //(coj.co.WorkDescription.Length > 50 ? coj.co.WorkDescription.Substring(0, 50) + "..." : coj.co.WorkDescription)),
                                   Status = coj.co.ProposalStatus.ProposalStatusDesc,
                                   AwaitingActionBy = coj.co.AwaitingActionBy == null ? string.Empty : coj.co.AwaitingActionBy,
                                   ProposalStatus = _context.ProposalStatus.Where(x => x.ProposalStatusID == coj.co.ProposalStatusID).OrderByDescending(x => x.ProposalStatusID).FirstOrDefault().ProposalStatusDesc,
                                   ApprovalStatus = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Approved == true ? "Endorsed" : "Not Endorsed",
                                   ApprovalDate = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                                   ApprovedBy = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy == null ? string.Empty : _context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy,
                                   DaysSinceLogged = 1,
                                   ApprovalDateList = "",//_context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                                   LastApprovedList = "",//_context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).Skip(1).FirstOrDefault().ApprovalDate,
                                   LastApproved = _context.ProposalEndorsement.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).Skip(1).FirstOrDefault().ApprovalDate,
                                       ProjectID = coj.co.PMMProjectID, //_context.Projects.Where(r => r.ProposalID == coj.co.ProposalID).OrderByDescending(x => x.ProjectID).FirstOrDefault().ProjectID,
                               });



                var filtered = ((userPropertyIds != null) && userPropertyIds.Any()) ? model.Where(m => userPropertyIds.Contains(m.SchoolId.Value)) : model;
                var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<ProposalForHPViewModel>(filterModel.SearchBy.ToLower());
                filtered = string.IsNullOrEmpty(filterModel.SearchBy) ? model : model.Where(dynamicQuery);
                filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");

                int i = 1;
                foreach (var columnSearch in filterModel.ColumnSearches)
                {
                    if (columnSearch.Key.ToLower() == "ProposalDate".ToLower())
                    {
                        filtered = filtered.Where(f => ((f.ProposalDate.Value.Day < 10 ? "0" + f.ProposalDate.Value.Day.ToString() : f.ProposalDate.Value.Day.ToString()) + "-" +
                                              (f.ProposalDate.Value.Month < 10 ? "0" + f.ProposalDate.Value.Month.ToString() : f.ProposalDate.Value.Month.ToString()) + "-" +
                                              f.ProposalDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() == "ApprovalDate".ToLower())
                    {
                        filtered = filtered.Where(f => ((f.ApprovalDate.Value.Day < 10 ? "0" + f.ApprovalDate.Value.Day.ToString() : f.ApprovalDate.Value.Day.ToString()) + "-" +
                                              (f.ApprovalDate.Value.Month < 10 ? "0" + f.ApprovalDate.Value.Month.ToString() : f.ApprovalDate.Value.Month.ToString()) + "-" +
                                              f.ApprovalDate.Value.Year.ToString())
                                              .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "LastApproved".ToLower())
                    {
                        //filtered = filtered.Where(f => ((f.LastApproved.Value.Day < 10 ? "0" + f.LastApproved.Value.Day.ToString() : f.LastApproved.Value.Day.ToString()) + "-" +
                        //                      (f.LastApproved.Value.Month < 10 ? "0" + f.LastApproved.Value.Month.ToString() : f.LastApproved.Value.Month.ToString()) + "-" +
                        //                      f.LastApproved.Value.Year.ToString())
                        //                      .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ApprovalStatus".ToLower())
                    {
                        //filtered = filtered.Where(f => f.ApprovalStatus.ToString().Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ProposalId".ToLower())
                    {
                        filtered = filtered.Where(f => f.ProposalID.ToString().Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "DaysSinceLogged".ToLower())
                    {
                        // filtered = filtered.Where(f => f.ProposalID.ToString().Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() != "DaysSinceLogged")
                    {
                        filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                    }

                    i++;
                }

                filteredResultsCount = filtered.Count();
                totalResultsCount = filtered.Count();
                transaction.Commit();
                if (filterModel.Take == -1)
                    return filtered;
                return filtered.OrderByDescending(x => x.Id).Skip(filterModel.Skip).Take(filterModel.Take);
                // return filtered.OrderByDescending(x => x.Id).Skip(filterModel.Skip).Take(filterModel.Take);
            }

        }
        public IEnumerable<ProposalViewModel> FilterProposalData(ProposalFilterModel filterModel, bool includeFullDesc, List<int> userPropertyIds, out int filteredResultsCount, out int totalResultsCount)
        {
            var ignoreFields = new string[] { };
            var dynamicQuery = PredicateHelper.BuildDynamicWhereClause<ProposalViewModel>(filterModel.ColumnSearches, ignoreFields);

            if (filterModel.PropertyId.HasValue)
            {
                userPropertyIds.Clear();
                userPropertyIds.Add(filterModel.PropertyId.Value);
            }
            using (var dbContextTransaction = _context.Database.BeginTransaction(IsolationLevel.Snapshot))
            {
                var jobs = _context.ProposalEndorsement;
            var proposalStatusID = _context.ProposalStatus.Where(x => x.ProposalStatusDesc == "Archived").FirstOrDefault().ProposalStatusID;
                List<ProposalViewModel> searchResults = _context.Proposal.Where(x => x.AgencyID == filterModel.AgencyId &&
                 (filterModel.SchoolId > 0 ? (x.PropertyID == filterModel.SchoolId) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true)) &&
                            (x.DeletedYN == null || x.DeletedYN == false) &&
                                                                          (filterModel.GrantApplication == true ? (x.ProposalStatusID == proposalStatusID) : (x.ProposalStatusID != proposalStatusID))
                             )
                     .GroupJoin(
                                  jobs,
                                  co => co.ProposalID,
                                  j => j.ProposalID,
                                  (x, y) => new { co = x, js = y })
                         .Select(
                     d => new ProposalViewModel
                     {
                         Id = d.co.ProposalID,
                         AgencyId = d.co.AgencyID,
                         ProposalID = d.co.ProposalID,
                         ProposalDate = d.co.ProposalDate,
                         StatusID = d.co.ProposalStatusID.HasValue ? d.co.ProposalStatusID.Value : 0,
                         SchoolName = d.co.Property.PropertyDesc,
                         RequestedBy = d.co.RecordedBy,
                         Description = d.co.WorkDescription,
                     //includeFullDesc ? d.co.WorkDescription : (String.IsNullOrEmpty(d.co.WorkDescription) ? String.Empty :
                     //        (d.co.WorkDescription.Length > 50 ? d.co.WorkDescription.Substring(0, 50) + "..." : d.co.WorkDescription)),
                     Status = d.co.ProposalStatus.ProposalStatusDesc,
                         WorkNominatedBy = d.co.ProposalWorkNominated.WorkNominatedName == null ? string.Empty : d.co.ProposalWorkNominated.WorkNominatedName,
                         AwaitingActionBy = d.co.AwaitingActionBy == null ? string.Empty : d.co.AwaitingActionBy,
                         ProposalStatus = _context.ProposalStatus.Where(x => x.ProposalStatusID == d.co.ProposalStatusID).OrderByDescending(x => x.ProposalStatusID).FirstOrDefault().ProposalStatusDesc,
                         ApprovalStatus = d.co.AwaitingActionBy == "None" ? "Approved" : d.co.CallLogFITSID != null ? "Approved" : _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Approved == true ? "Endorsed" : "Not Endorsed",
                         ApprovalDate = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,// ==null?string.Empty: _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                     ApprovedBy = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy == null ? string.Empty : _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy,
                         LastApproved = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).Skip(1).FirstOrDefault().ApprovalDate,
                         ProjectID = d.co.PMMProjectID,//_context.Projects.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProjectID).FirstOrDefault().ProjectID,
                     PropertyId = d.co.PropertyID,
                         DaysSinceLogged = 1,//(int)GetBusinessDays(d.co.ProposalDate ?? DateTime.Now, DateTime.Now),
                         CalllogFitsID = d.co.CallLogFITSID,
                     }).ToList();

                foreach (var columnSearch in filterModel.ColumnSearches)
                {
                    if (columnSearch.Key.ToLower() == "DaysSinceLogged".ToLower() || !string.IsNullOrEmpty(filterModel.SearchBy))
                    {
                        foreach (var row in searchResults)
                        {
                            row.DaysSinceLogged = (int)GetBusinessDays(row.ProposalDate ?? DateTime.Now, DateTime.Now);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(filterModel.SearchBy))
                {
                    foreach (var row in searchResults)
                    {
                        row.DaysSinceLogged = (int)GetBusinessDays(row.ProposalDate ?? DateTime.Now, DateTime.Now);
                    }
                }



                //var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                //      ? searchResults.AsQueryable()
                //      : searchResults.AsQueryable();


                var filtered = string.IsNullOrEmpty(filterModel.SearchBy)
                ? searchResults.AsQueryable()
                 : searchResults.AsQueryable().Where(f => f.ProposalID.ToString().Contains(filterModel.SearchBy.ToLower()) || f.Id.ToString().Contains(filterModel.SearchBy.ToLower()) ||
                                             (f.ProposalDate.HasValue && f.ProposalDate.Value.ToString("dd-MM-yyyy").Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.SchoolName).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.RequestedBy).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.Description).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.WorkNominatedBy).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.ApprovedBy).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (f.ApprovalDate.HasValue && f.ApprovalDate.Value.ToString("dd-MM-yyyy").Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.AwaitingActionBy).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (f.LastApproved.HasValue && f.LastApproved.Value.ToString("dd-MM-yyyy").Contains(filterModel.SearchBy.ToLower())) ||
                                             (f.DaysSinceLogged > 0 && f.DaysSinceLogged.ToString().ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.ApprovalStatus).ToLower().Contains(filterModel.SearchBy.ToLower())) ||
                                             (Convert.ToString(f.ProposalStatus).ToLower().Contains(filterModel.SearchBy.ToLower()))
               );



                filtered = filtered.OrderBy($"{filterModel.SortBy} {filterModel.SortDir}");
                int i = 1;
                foreach (var columnSearch in filterModel.ColumnSearches)
                {
                    if (columnSearch.Key.ToLower() == "ProposalDate".ToLower())
                    {
                        filtered = filtered.Where(f => f.ProposalDate.HasValue && f.ProposalDate.Value.ToString("dd-MM-yyyy").Contains(columnSearch.Value.ToLower()));
                        //filtered = filtered.Where(f => ((f.ProposalDate.Value.Day < 10 ? "0" + f.ProposalDate.Value.Day.ToString() : f.ProposalDate.Value.Day.ToString()) + "-" +
                        //                      (f.ProposalDate.Value.Month < 10 ? "0" + f.ProposalDate.Value.Month.ToString() : f.ProposalDate.Value.Month.ToString()) + "-" +
                        //                      f.ProposalDate.Value.Year.ToString())
                        //                      .Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() == "ApprovalDate".ToLower())
                    {
                        filtered = filtered.Where(f => f.ApprovalDate.HasValue && f.ApprovalDate.Value.ToString("dd-MM-yyyy").Contains(columnSearch.Value.ToLower()));
                        //filtered = filtered.Where(f => ((f.ApprovalDate.Value.Day < 10 ? "0" + f.ApprovalDate.Value.Day.ToString() : f.ApprovalDate.Value.Day.ToString()) + "-" +
                        //                      (f.ApprovalDate.Value.Month < 10 ? "0" + f.ApprovalDate.Value.Month.ToString() : f.ApprovalDate.Value.Month.ToString()) + "-" +
                        //                      f.ApprovalDate.Value.Year.ToString())
                        //                      .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "LastApproved".ToLower())
                    {
                        filtered = filtered.Where(f => f.LastApproved.HasValue && f.LastApproved.Value.ToString("dd-MM-yyyy").Contains(columnSearch.Value.ToLower()));
                        //filtered = filtered.Where(f => ((f.LastApproved.Value.Day < 10 ? "0" + f.LastApproved.Value.Day.ToString() : f.LastApproved.Value.Day.ToString()) + "-" +
                        //                      (f.LastApproved.Value.Month < 10 ? "0" + f.LastApproved.Value.Month.ToString() : f.LastApproved.Value.Month.ToString()) + "-" +
                        //                      f.LastApproved.Value.Year.ToString())
                        //                      .Contains(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ApprovalStatus".ToLower())
                    {
                        filtered = filtered.Where(f => f.ApprovalStatus.ToString().ToLower().Contains(columnSearch.Value.ToLower()));
                    }
                    else if (columnSearch.Key.ToLower() == "ProposalId".ToLower())
                    {
                        filtered = filtered.Where(f => f.ProposalID.ToString().StartsWith(columnSearch.Value));
                    }
                    else if (columnSearch.Key.ToLower() == "ProposalStatus".ToLower())
                    {
                        filtered = filtered.Where(f => f.ProposalStatus.ToString().ToLower().Contains(columnSearch.Value.ToLower()));
                    }
                    else if (columnSearch.Key.ToLower() == "DaysSinceLogged".ToLower())
                    {
                         filtered = filtered.Where(f => f.DaysSinceLogged.ToString().Contains(columnSearch.Value));
                    }

                    else if (columnSearch.Key.ToLower() != "DaysSinceLogged")
                    {
                        filtered = filtered.Where($"{columnSearch.Key}.ToString().ToLower().Contains(@0)", columnSearch.Value);
                    }
                    i++;
                }

                filteredResultsCount = filtered.Count();
                totalResultsCount = searchResults.Count();
                dbContextTransaction.Commit();
                if (filterModel.Take == -1)
                    return filtered;
                // return filtered.ToList();
                return filtered.Skip(filterModel.Skip).Take(filterModel.Take).ToList();
            }
        }

        public int GetBusinessDays(DateTime startD, DateTime endD)
        {
            double calcBusinessDays =
                1 + ((endD - startD).TotalDays * 5 -
                (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if (endD.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (startD.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            return (int)calcBusinessDays;
        }

        public ProposalAddEditViewModel GetDetailsById(int? id, int agencyId)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction(IsolationLevel.Snapshot))
            {
                Projects projectsData = _context.Projects.Where(x => x.ProposalID == id.Value).FirstOrDefault();
                Proposal data = _context.Proposal.Where(x => x.ProposalID == id.Value).FirstOrDefault();
                var documentTypeID = _context.SupportDocumentType.Where(x => x.SupportDocumentTypeName == "POC").FirstOrDefault();

                var proposalData = new ProposalAddEditViewModel()
                {
                    AgencySequenceId = data.AgencySequenceID,
                    ProposalId = data.ProposalID,
                    AgencyId = data.AgencyID.Value,
                    PropertyId = data.PropertyID.Value,
                    ProposalDate = data.ProposalDate,
                    CallLogTime = data.RecordedDate,
                    CallOutId = id,
                    PrincipalEmail = data.PrincipalEmail,
                    PrincipalName = data.PrincipalName,
                    Phone = data.Property.Phone,
                    RoomLocation = data.RoomLocation,
                    Rooms = data.RoomNumber,
                    RoomSchool = data.RoomNumber,
                    SelectedCallStatusId = data.ProposalStatusID,
                    WorkDescription = data.WorkDescription,
                    ProposalSFCID = data.ProposalSFCID,
                    ProjectDate = data.ProjectDate,
                    ReportedDate = data.ReportedDate,
                    ConceptEstimate = data.ConceptEstimate == null ? data.ConceptEstimate : decimal.Round(data.ConceptEstimate.Value, 2, MidpointRounding.AwayFromZero),
                    ConceptEstimateDisabled = data.ConceptEstimate == null ? data.ConceptEstimate : decimal.Round(data.ConceptEstimate.Value, 2, MidpointRounding.AwayFromZero),
                    SubmittedtDate = data.SubmittedtDate,
                    ProjectTypeID = data.ProposalProjectTypeID,
                    WorkTypeId = data.WorkTypeId,
                    ProposalWorkNominatedID = data.ProposalWorkNominatedID,
                    ProposalWorkNominatedIDHidden = data.ProposalWorkNominatedID,
                    ProposalProgramID = data.ProposalProgramID,
                    ProposalProcurementID = data.ProposalProcurementID,
                    ProposalSubProgramID = data.ProposalSubProgramID,
                    PMMProfileID = data.ProposalPMMProfileID,
                    ProjectDescription = data.ProjectDescription,
                    ProjectDescriptionSchool = data.ProjectDescription,
                    SubmittedBy = data.SubmittedBy,
                    Submitted = data.Submitted,
                    Hazmat = data.Hazmat,
                    DeliveryTypeLRLV = data.DeliveryTypeLRLV,
                    DeliveryTypeLRLVHidden = data.DeliveryTypeLRLV,
                    DeliveryTypeSSD = data.DeliveryTypeSSD,
                    DeliveryTypeSSDHidden = data.DeliveryTypeSSD,
                    DeliveryTypeAMU = data.DeliveryTypeAMU,
                    DeliveryTypeAMUHidden = data.DeliveryTypeAMU,
                    SubmitDisabled = data.Submitted,
                    Functionality = data.Functionality,
                    UserEmail = data.UserEmail,
                    RoleID = data.RoleID,
                    UserID = data.UserID,
                    UserIDPMASOHidden = data.UserID,
                    Reviewed = data.Reviewed,
                    ReviewedDisbaled = data.Reviewed,
                    ReviewedBy = data.ReviewedBy,
                    ReviewDate = data.ReviewDate,
                    AwaitingActionBy = data.AwaitingActionBy,
                    JointFunding = data.JointFunding,
                    ProjectID = data.PMMProjectID,//projectsData?.ProjectID,
                    NomID = data.NomID,
                    ProposalSubmission = data.ProposalSubmission,
                    ProposalSubmissionDisabled = data.ProposalSubmission,
                    FundsAvailability = data.FundsAvailability == null ? data.FundsAvailability : decimal.Round(data.FundsAvailability.Value, 2, MidpointRounding.AwayFromZero),
                    FundsAvailabilityDisabled = data.FundsAvailability == null ? data.FundsAvailability : decimal.Round(data.FundsAvailability.Value, 2, MidpointRounding.AwayFromZero),
                    FundsAvailabilityOld = data.FundsAvailabilityOld == null ? data.FundsAvailabilityOld : decimal.Round(data.FundsAvailabilityOld.Value, 2, MidpointRounding.AwayFromZero),
                    SchoolFunded = data.SchoolFunded,
                    SchoolFundedHidden = data.SchoolFunded,
                    SchoolRecurrent = data.SchoolRecurrent,
                    ProgramName = data.ProposalProgramID == null ? null : _context.ProposalProgram.Where(r => r.ProposalProgramID == data.ProposalProgramID).OrderByDescending(x => x.ProposalProgramID).FirstOrDefault().ProgramName,
                    SchoolRecurrentHidden = data.SchoolRecurrent,
                    CallLogFITSID = data.CallLogFITSID,
                    NewCalloutID = _context.CallLogFITS.FirstOrDefault(x => x.CallLogFITSID == data.CallLogFITSID) == null ? 0 : _context.CallLogFITS.FirstOrDefault(x => x.CallLogFITSID == data.CallLogFITSID).AgencySequenceID,
                    AMUFunded = data.AMUFunded,
                    AMUFundedHidden = data.AMUFunded,
                    CommunityFunded = data.CommunityFunded,
                    CommunityFundedHidden = data.CommunityFunded,
                   //PlannedMaintenance = data.PlannedMaintenance,
                    //DeliveryTypePlannedMaintenanceHidden = data.PlannedMaintenance,
                    SchoolSelfDelivery = data.SchoolSelfDelivery,
                    SchoolSelfDeliveryHidden = data.SchoolSelfDelivery,
                    GrantApplication = data.GrantApplication,
                    GrantApplicationHidden = data.GrantApplication,
                    POCDocumentTypeID = documentTypeID == null ? 0 : documentTypeID.SupportDocumentTypeID
                };

                //Get school details
                var schoolDetails = _context.Properties.Where(x => x.PropertyId == proposalData.PropertyId).FirstOrDefault();
                if (schoolDetails != null)
                {
                    proposalData.SchoolName = schoolDetails.PropertyShortDesc;
                    proposalData.SchoolNameFull = schoolDetails.PropertyDesc;
                    proposalData.Area = schoolDetails.EducationArea != null ? schoolDetails.EducationArea.EducationAreaDesc : "";
                    proposalData.SchoolEmail = schoolDetails.Email;
                    proposalData.SchoolId = schoolDetails.SchoolId.HasValue ? schoolDetails.SchoolId.Value : 0;
                    proposalData.SchoolDetails = schoolDetails;
                    proposalData.ASOName = schoolDetails.ASOName;
                    proposalData.ASOContactNumber = schoolDetails.ASOContactNumber;
                    proposalData.ASOEmail = schoolDetails.ASOEmail;
                    proposalData.GLEmail = schoolDetails.GLEmail;
                    proposalData.SGLEmail = schoolDetails.SGLEmail;
                    proposalData.DAMEmail = schoolDetails.DAMEmail;
                    proposalData.EDAMEmail = schoolDetails.EDAMEmail;
                    proposalData.EDEmail = schoolDetails.EDEmail;
                    proposalData.DELEmail = schoolDetails.DELEmail;

                }

                //Get job id
                //if the Job is Cancelled, then dont bind
                ServiceReport sr = _context.ServiceReports.Where(r => r.CallLogFITSId == data.ProposalID).OrderByDescending(x => x.ServiceReportId).FirstOrDefault();
                proposalData.JobId = (sr != null && sr.ServiceReportStatusId != 9) ? sr.AgencySequenceId : null;
                proposalData.JobCancelled = sr != null ? sr.ServiceReportStatusId == 9 : false;
                SchoolLimit schoolLimit = _context.SchoolLimits.Where(x => x.PropertyID == proposalData.PropertyId && (x.EndDate == null || x.EndDate >= DateTime.Now)).FirstOrDefault();
                proposalData.SchoolLimit = schoolLimit != null ?
                                            decimal.Round(schoolLimit.SchoolLimitAmount.Value, 2, MidpointRounding.AwayFromZero) : 0;
                //Get Endorsements
                List<ProposalEndorsement> endorsements = _context.ProposalEndorsement
                    .Where(x => x.ProposalID == data.ProposalID)
                    .ToList();



                proposalData.Endorsements = endorsements.Where(x => x.ProposalEndorsementType.ViewType == "AMU").Select(x => new EndorsementViewModel()
                {
                    ProposalID = x.ProposalID,
                    EndorsementName = x.ProposalEndorsementType.EndorsementTypeName,
                    Approved = x.Approved,
                    ApprovedBy = x.ApprovedBy,
                    ApprovalDate = x.ApprovalDate,
                    Position = x.Position,
                    EndorsementTypeID = x.ProposalEndorsementTypeID,
                    Limit = x.ProposalEndorsementType.Limit,
                    EndorsementID = x.ProposalEndorsementID,
                    Comments = x.Comments
                }).ToList();

                proposalData.SchoolEndorsements = endorsements.Where(x => x.ProposalEndorsementType.ViewType == "School").Select(x => new EndorsementViewModel()
                {
                    ProposalID = x.ProposalID,
                    EndorsementName = x.ProposalEndorsementType.EndorsementTypeName,
                    Approved = x.Approved,
                    ApprovedBy = x.ApprovedBy,
                    ApprovalDate = x.ApprovalDate,
                    Position = x.Position,
                    EndorsementTypeID = x.ProposalEndorsementTypeID,
                    Limit = x.ProposalEndorsementType.Limit,
                    EndorsementID = x.ProposalEndorsementID,
                    Comments = x.Comments
                }).ToList();


                List<ProposalEndorsementType> endorsementTypes = _context.ProposalEndorsementType
                    .ToList();

                proposalData.EndorsementType = endorsementTypes.Select(x => new ProposalEndorsementType()
                {
                    EndorsementTypeName = x.EndorsementTypeName,
                    ProposalEndorsementTypeID = x.ProposalEndorsementTypeID,
                    Limit = x.Limit,
                    ViewType = x.ViewType
                }).ToList();

                //Get Entitlements
                ProposalEntitlement entitlements = _context.ProposalEntitlement
                    .Where(x => x.ProposalID == data.ProposalID).FirstOrDefault();

                if (entitlements != null)
                {
                    // callOutData.entitlements = entitlements;
                    proposalData.Entitlements = new ProposalEntitlementViewModel()
                    {
                        ValidateEntitlements = proposalData.Submitted,
                        ProposalID = entitlements.ProposalID,
                        REF = entitlements.REF,
                        DA = entitlements.DA,
                        Exempt = entitlements.Exempt,
                        WithConsent = entitlements.WithConsent,
                        WithoutConsent = entitlements.WithoutConsent,
                        CDC = entitlements.CDC,

                        Arboist = entitlements.Arboist,
                        Bushfire = entitlements.Bushfire,
                        Contamination = entitlements.Contamination,
                        FloodProne = entitlements.FloodProne,
                        Heritage = entitlements.Heritage,
                        Ecology = entitlements.Ecology,
                        Archaeology = entitlements.Archaeology,
                        PRNone = entitlements.PRNone,
                        EPNone = entitlements.EPNone,

                        SSU = entitlements.SSU,
                        ITD = entitlements.ITD,
                        Demountable = entitlements.Demountable,
                        AssetActivation = entitlements.AssetActivation,
                        SCNone = entitlements.SCNone,

                        ServiceIntereption = entitlements.ServiceIntereption,
                        ExamsInOperation = entitlements.ExamsInOperation,
                        VacationPeriod = entitlements.VacationPeriod,
                        Relocation = entitlements.Relocation,
                        OCNone = entitlements.OCNone,
                        TSNone = entitlements.TSNone,

                        Asbestos = entitlements.Asbestos,
                        Lead = entitlements.Lead,
                        Mould = entitlements.Mould,
                        HMNone = entitlements.HMNone,

                        Electrical = entitlements.Electrical,
                        ElectricalNo = entitlements.ElectricalNo,
                        ElectricalYes = entitlements.ElectricalYes,
                        Hydraulics = entitlements.Hydraulics,
                        Sewer = entitlements.Sewer,
                        Mechanical = entitlements.Mechanical,
                        Stormwater = entitlements.Stormwater,
                        IT = entitlements.IT,
                        Security = entitlements.Security,
                        SNone = entitlements.SNone,

                        LearningWellbeing = entitlements.LearningWellbeing,
                        WHS = entitlements.WHS,
                        StatPlanning = entitlements.StatPlanning,
                        TechHeritage = entitlements.HeritageTech,
                        Compliance = entitlements.Compliance,
                        Sustainability = entitlements.Sustainability,
                        Comms = entitlements.Comms,
                        Leaseslicences = entitlements.LeaseLicense,
                        CentralServices = entitlements.CentralService,
                    };
                }

                //Documents
                int supportDocumentClassId = 0;
                supportDocumentClassId = (int)SupportDocumentClassEnum.PROPOSAL;

                //Get documents
                List<SupportDocument> docs = _context.SupportDocument
                    .Where(x => x.SupportDocumentClassID == supportDocumentClassId
                            && x.MasterRecordID == data.ProposalID && (x.MarkAsDeleted == null || x.MarkAsDeleted == false))
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

                proposalData.SupportDocuments = docs.Select(x => new SupportDocumentViewModel()
                {
                    AgencyID = data.AgencyID.Value,
                    AttachmentLink = x.AttachmentLink,
                    AttachmentText = x.AttachmentText,
                    PropertyName = proposalData.SchoolName,
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


                proposalData.JobBuildingIDs = _context.ProposalBuildings
                                            .Where(x => x.ProposalID == data.ProposalID && x.Deleted != true)
                                            .Select(d => d.BuildingID).ToList();
                proposalData.CallLogTime = proposalData.ProposalDate;

                dbContextTransaction.Commit();
                return proposalData;
            }
        }

        private Proposal MapProposalModelToEntity(ProposalAddEditViewModel model)
        {
            Proposal dataToSave = new Proposal()
            {
                AgencyID = model.AgencyId,
                AgencySequenceID = model.AgencySequenceId,
                ProposalDate = model.ProposalDate,
                ProposalStatusID = model.SelectedCallStatusId,
                LastRecordedDate = DateTime.Now,
                RoomLocation = (model.RoomLocation != null && model.RoomLocation.Length > 50) ? model.RoomLocation.Substring(0, 50) : model.RoomLocation,
                RoomNumber = (model.Rooms != null && model.Rooms.Length > 100) ? model.Rooms.Substring(0, 100) : model.Rooms,
                PrincipalEmail = (model.PrincipalEmail != null && model.PrincipalEmail.Length > 50) ? model.PrincipalEmail.Substring(0, 50) : model.PrincipalEmail,
                PrincipalName = (model.PrincipalName != null && model.PrincipalName.Length > 50) ? model.PrincipalName.Substring(0, 50) : model.PrincipalName,
                PropertyID = model.PropertyId,//Proposal table SchoolId is the property Id.
                SchoolLimit = model.SchoolLimit,
                WorkDescription = model.WorkDescription,
                ProposalSFCID = model.ProposalSFCID == 0 ? null : model.ProposalSFCID,
                ConceptEstimate = model.ConceptEstimate == null ? model.ConceptEstimateDisabled : model.ConceptEstimate,
                ProposalProjectTypeID = model.ProjectTypeID,
                ProposalWorkNominatedID = model.ProposalWorkNominatedID,
                ProposalProgramID = model.ProposalProgramID == 0 ? null : model.ProposalProgramID,
                ProposalProcurementID = model.ProposalProcurementID,
                ProposalSubProgramID = model.ProposalSubProgramID == 0 ? null : model.ProposalSubProgramID,
                ProposalPMMProfileID = model.PMMProfileID,
                ProjectDescription = model.ProjectDescription,
                Functionality = model.Functionality,
                Submitted = model.Submitted,
                Hazmat = model.Hazmat,
                DeliveryTypeAMU = model.DeliveryTypeAMU,//!= model.DeliveryTypeAMUHidden? model.DeliveryTypeAMUHidden: model.DeliveryTypeAMU,
                DeliveryTypeSSD = model.DeliveryTypeSSD,// != model.DeliveryTypeSSDHidden ? model.DeliveryTypeSSDHidden : model.DeliveryTypeSSD,
                DeliveryTypeLRLV = model.DeliveryTypeLRLV,// != model.DeliveryTypeLRLVHidden ? model.DeliveryTypeLRLVHidden : model.DeliveryTypeLRLV,
                Reviewed = model.Reviewed,
                ReviewedBy = model.ReviewedBy,
                WorkTypeId = model.WorkTypeId,
                UserEmail = model.Submitted == true ? "1" : "0",
                UserID = model.UserID,
                RoleID = model.RoleID,
                ReviewDate = model.ReviewDate,
                AwaitingActionBy = model.AwaitingActionBy,
                JointFunding = model.JointFunding,
                ProposalSubmission = model.ProposalSubmission,
                FundsAvailability = model.FundsAvailability,
                FundsAvailabilityOld = model.FundsAvailabilityOld,
                SchoolFunded = model.SchoolFunded != model.SchoolFundedHidden ? model.SchoolFundedHidden : model.SchoolFunded,
                SchoolRecurrent = model.SchoolRecurrent,
                CommunityFunded = model.CommunityFunded != model.CommunityFundedHidden ? model.CommunityFundedHidden : model.CommunityFunded,
                AMUFunded = model.AMUFunded != model.AMUFundedHidden ? model.AMUFundedHidden : model.AMUFunded,
               PlannedMaintenance = model.PlannedMaintenance != model.PlannedMaintenanceHidden ? model.PlannedMaintenanceHidden : model.PlannedMaintenance,
                SchoolSelfDelivery = model.SchoolSelfDelivery != model.SchoolSelfDeliveryHidden ? model.SchoolSelfDeliveryHidden : model.SchoolSelfDelivery,
                GrantApplication = model.Submitted == true || model.SubmitDisabled == true ? model.GrantApplicationHidden : model.GrantApplication,

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

        public void DeleteProposal(int? proposalId, int? agencyId)
        {
            var proposalData = _context.Proposal.SingleOrDefault(x => x.ProposalID == proposalId && x.AgencyID == agencyId);
            proposalData.DeletedYN = true;
            _context.SaveChanges();

        }

        public IEnumerable<RegionalContact> GetPMASO()
        {
            var userData = _context.RegionalContacts.Where(x => x.RoleId == "SO").ToList();
            return userData;

        }

        public int SaveDocument(SupportDocument document, int callOutId, out int documentId)
        {
            documentId = 0;
            if (document.SupportDocumentClassID == (int)SupportDocumentClassEnum.PROPOSAL)
            {
                int? documentSequenceId = 0;
                document.MasterRecordID = callOutId;
                if (document.MasterRecordID == 0)
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

            return 0;
        }

        public List<ProposalViewModel> EmailNotifications()
        {
            var jobs = _context.ProposalEndorsement.Where(x => x.Emailsent == false);
            IQueryable<ProposalViewModel> searchResults = _context.Proposal.Where(x => x.Emailsent == false && x.Submitted == true)

                 .GroupJoin(
                              jobs,
                              co => co.ProposalID,
                              j => j.ProposalID,
                              (x, y) => new { co = x, js = y })
                     .Select(
                 d => new ProposalViewModel
                 {
                     Id = d.co.ProposalID,
                     AgencyId = d.co.AgencyID,
                     ProposalID = d.co.ProposalID,
                     ProposalDate = d.co.ProposalDate,
                     StatusID = d.co.ProposalStatusID.HasValue ? d.co.ProposalStatusID.Value : 0,
                     SchoolName = d.co.Property.PropertyDesc,
                     RequestedBy = d.co.RecordedBy,
                     Status = d.co.ProposalStatus.ProposalStatusDesc,
                     WorkNominatedBy = d.co.ProposalWorkNominated.WorkNominatedName,
                     AwaitingActionBy = d.co.AwaitingActionBy,
                     RoleId = d.co.RoleID,
                     EmailSent = d.co.Emailsent,
                     ProposalEndorsementId = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ProposalEndorsementID,
                     EndorsementEmailSent = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Emailsent == true ? true : false,
                     ApprovalStatus = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Approved == true ? "Endorsed" : "Not Endorsed",
                     ApprovalDate = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                     ApprovedBy = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy,
                     WorkCategory = d.co.WorkDescription,
                     PrincipalEmail = "sinswonline@det.nsw.edu.au",//data.PrincipalEmail,TODO
                     PrincipalName = d.co.PrincipalName,
                     Phone = d.co.Property.Phone,
                     RoomLocation = d.co.RoomLocation,
                     Rooms = d.co.RoomNumber,
                     PropertyId = d.co.PropertyID.Value,
                 });
            return searchResults.ToList();
        }
        public List<ProposalViewModel> EndorsementEmailNotifications()
        {
            var jobs = _context.ProposalEndorsement.Where(x => x.Emailsent == false);
            IQueryable<ProposalViewModel> searchResults = _context.Proposal.Where(x => x.Submitted == true)

                 .GroupJoin(
                              jobs,
                              co => co.ProposalID,
                              j => j.ProposalID,
                              (x, y) => new { co = x, js = y })
                     .Select(
                 d => new ProposalViewModel
                 {
                     Id = d.co.ProposalID,
                     AgencyId = d.co.AgencyID,
                     ProposalID = d.co.ProposalID,
                     ProposalDate = d.co.ProposalDate,
                     StatusID = d.co.ProposalStatusID.HasValue ? d.co.ProposalStatusID.Value : 0,
                     SchoolName = d.co.Property.PropertyDesc,
                     RequestedBy = d.co.RecordedBy,
                     Status = d.co.ProposalStatus.ProposalStatusDesc,
                     WorkNominatedBy = d.co.ProposalWorkNominated.WorkNominatedName,
                     AwaitingActionBy = d.co.AwaitingActionBy,
                     RoleId = d.co.RoleID,
                     EmailSent = d.co.Emailsent,
                     ProposalEndorsementId = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ProposalEndorsementID,
                     EndorsementEmailSent = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Emailsent == true ? true : false,
                     ApprovalStatus = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().Approved == true ? "Endorsed" : "Not Endorsed",
                     ApprovalDate = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovalDate,
                     ApprovedBy = _context.ProposalEndorsement.Where(r => r.ProposalID == d.co.ProposalID).OrderByDescending(x => x.ProposalEndorsementTypeID).FirstOrDefault().ApprovedBy,
                     WorkCategory = d.co.WorkDescription,
                     PrincipalEmail = "sinswonline@det.nsw.edu.au",//data.PrincipalEmail,TODO
                     PrincipalName = d.co.PrincipalName,
                     Phone = d.co.Property.Phone,
                     RoomLocation = d.co.RoomLocation,
                     Rooms = d.co.RoomNumber,
                     PropertyId = d.co.PropertyID.Value,
                 });
            return searchResults.ToList();
        }



        public bool UpdateEmailData(ProposalViewModel model, string source)
        {
            if (source.Equals("proposal"))
            {
                using (_context)
                {
                    Proposal result = _context.Proposal.SingleOrDefault(x => x.ProposalID == model.ProposalID && x.AgencyID == model.AgencyId);
                    if (result != null)
                    {
                        result.Emailsent = true;
                        _context.SaveChanges();
                    }

                }
            }
            else
            {
                using (_context)
                {
                    var existingData = _context.ProposalEndorsement.SingleOrDefault(b => b.ProposalEndorsementID == model.ProposalEndorsementId);
                    if (existingData != null)
                    {
                        existingData.Emailsent = true;
                        _context.SaveChanges();
                    }

                }
            }

            return true;
        }


        public ProposalRoles GetProposalRoles(int agencyID, int propertyID)
        {
            var userData = _context.ProposalRoles.Where(x => x.AgencyID == agencyID && x.PropertyID == propertyID).FirstOrDefault();
            return userData;
        }

        /// <summary>
        /// This method will give count from Proposal on Dashboard.
        /// InProgress, Open, Completed counts are getting generated from this method.
        /// </summary>
        /// <param name="agencyID"></param>
        /// <param name="propertyID"></param>
        /// <param name="RoleID"></param>
        /// <param name="userPropertyIds"></param>
        /// <returns></returns>
        public ItemsCounts GetProposalCounts(int agencyID, int propertyID, string RoleID, List<int> userPropertyIds)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var searchResults =
                RoleID == "SO" ?
                        _context.Proposal
                        .Where(
                                x => x.AgencyID == agencyID
                                && (propertyID > 0 ? (x.PropertyID == propertyID) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true))
                                //&& x.RoleID == "SO" 
                                // && x.ProposalStatusID != 2 && x.ProposalStatusID != 15 && x.ProposalStatusID != 3
                                && x.ProposalStatusID != 15
                                && (x.DeletedYN == null || x.DeletedYN == false)) :
                        _context.Proposal
                        .Where(
                                x => x.AgencyID == agencyID
                                && (propertyID > 0 ? (x.PropertyID == propertyID) : (userPropertyIds.Count > 0 ? userPropertyIds.Contains(x.PropertyID.Value) : true))
                                //&& x.RoleID != "SO"
                                // && x.ProposalStatusID != 2 && x.ProposalStatusID != 15 && x.ProposalStatusID != 3
                                && x.ProposalStatusID != 15
                               && (x.DeletedYN == null || x.DeletedYN == false));

                ItemsCounts itemsCounts = new ItemsCounts()
                {
                    OpenedCount = searchResults.Where(x => x.Submitted != true).Count(),
                    CompletedCount = searchResults.Where(x => x.AwaitingActionBy == "None" || x.ProposalStatusID == 3).Count(),
                };

                itemsCounts.InProgressCount = searchResults.Count() - itemsCounts.OpenedCount - itemsCounts.CompletedCount;
                itemsCounts.TotalCount = itemsCounts.OpenedCount + itemsCounts.InProgressCount + itemsCounts.CompletedCount;
                return itemsCounts;
            }
        }

    }

}
